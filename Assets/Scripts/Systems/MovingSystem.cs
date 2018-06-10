#region

using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#endregion

namespace Systems
{
    public class MovementSystem : JobComponentSystem
    {

        private struct DetectNextWaypointJob : IJobParallelFor
        {
            private InjectData _data;

            public DetectNextWaypointJob (InjectData data)
            {
                this._data = data;
            }

            public void Execute (int index)
            {
                var agent = _data.Statuses[index];
                if (agent.TotalWaypoints == 0)
                {
                    return;
                }

                if (agent.RemainingDistance > 0)
                {
                    return;
                }

                var entity = _data.Entities[index];
                if (agent.NextWaypointIndex != agent.TotalWaypoints)
                {
                    try
                    {
                        agent.NextWaypoint = WaypointCacheSystem.EntityWaypoints[entity.Index][agent.NextWaypointIndex];
                    }
                    catch { }
                }
                else if (agent.WaitTime <= 0)
                {
                    agent.TotalWaypoints = 0;
                    agent.WaitTime = 60f;
                    NavSystem.GetNextWaypoint (entity, agent);
                }

                _data.Statuses[index] = agent;
            }
        }

        [ComputeJobOptimization]
        private struct MovementJob : IJobParallelFor
        {
            private readonly float dt;
            private readonly float speed;
            private readonly float3 up;
            private readonly float3 one;

            private InjectData _data;

            public MovementJob (InjectData data, float dt, float speed)
            {
                this.dt = dt;
                this.speed = speed;
                _data = data;
                up = Vector3.up;
                one = Vector3.one;
            }

            public void Execute (int index)
            {
                if (index >= _data.Statuses.Length)
                {
                    return;
                }

                var status = _data.Statuses[index];
                if (status.TotalWaypoints == 0)
                {
                    return;
                }

                if (status.RemainingDistance > 0)
                {
                    status.RemainingDistance -= speed;
                    status.Position += math.forward (status.Rotation) * speed;
                    status.Matrix = Matrix4x4.TRS (status.Position, status.Rotation, one);
                }
                else if (status.NextWaypointIndex == status.TotalWaypoints)
                {
                    if (status.WaitTime >= 0)
                    {
                        status.WaitTime -= dt;
                    }
                }
                else
                {
                    var diff = (Vector3) status.NextWaypoint - (Vector3) status.Position;
                    var magnitude = diff.magnitude;
                    if (magnitude > 0.00001f)
                    {
                        status.RemainingDistance = magnitude;
                        status.Rotation = Quaternion.LookRotation (diff, up);
                    }

                    status.NextWaypointIndex++;
                }

                if (index >= _data.Statuses.Length)
                {
                    return;
                }

                _data.Statuses[index] = status;
            }
        }

        private struct InjectData
        {
            public int Length;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<NavAgent> Statuses;
        }

        [Inject] private InjectData data;

        [ComputeJobOptimization]
        protected override JobHandle OnUpdate (JobHandle deps)
        {
            var dt = Time.deltaTime;
            var speed = dt * 2;

            deps = new DetectNextWaypointJob (data).Schedule (data.Length, 64, deps);
            deps = new MovementJob (
                data,
                dt,
                speed
            ).Schedule (data.Length, 64, deps);

            return deps;
        }
    }
}