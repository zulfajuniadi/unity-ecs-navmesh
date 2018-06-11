#region

using Components;
using Unity.Burst;
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
        NativeQueue<NeedsUpdate> needsPath = new NativeQueue<NeedsUpdate> (Allocator.Persistent);
        NativeQueue<NeedsUpdate> needsWaypoint = new NativeQueue<NeedsUpdate> (Allocator.Persistent);

        public struct NeedsUpdate
        {
            public int index;
            public Entity entity;
            public NavAgent agent;
        }

        [BurstCompile]
        private struct DetectNextWaypointJob : IJobParallelFor
        {
            public InjectData data;
            public NativeQueue<NeedsUpdate>.Concurrent needsWaypoint;
            public NativeQueue<NeedsUpdate>.Concurrent needsPath;

            public void Execute (int index)
            {
                var status = data.Statuses[index];
                if (status.TotalWaypoints == 0 || status.RemainingDistance > 0)
                {
                    return;
                }

                var entity = data.Entities[index];
                if (status.NextWaypointIndex != status.TotalWaypoints)
                {
                    needsWaypoint.Enqueue (new NeedsUpdate
                    {
                        agent = data.Statuses[index],
                            entity = data.Entities[index],
                            index = index,
                    });
                }
                else if (status.WaitTime <= 0)
                {
                    needsPath.Enqueue (new NeedsUpdate
                    {
                        agent = data.Statuses[index],
                            entity = data.Entities[index],
                            index = index,
                    });
                }
            }
        }

        public struct SetNextWaypointJob : IJob
        {
            public InjectData data;
            public NativeQueue<NeedsUpdate> needsWaypoint;
            public void Execute ()
            {
                while (needsWaypoint.TryDequeue (out NeedsUpdate item))
                {
                    var status = data.Statuses[item.index];
                    var entity = data.Entities[item.index];
                    status.NextWaypoint = WaypointProviderSystem.EntityWaypoints[entity.Index][status.NextWaypointIndex];
                    data.Statuses[item.index] = status;
                }
            }
        }

        public struct SetNextPathJob : IJob
        {
            public InjectData data;
            public NativeQueue<NeedsUpdate> needsPath;
            public void Execute ()
            {
                while (needsPath.TryDequeue (out NeedsUpdate item))
                {
                    var status = data.Statuses[item.index];
                    var entity = data.Entities[item.index];
                    status.TotalWaypoints = 0;
                    status.WaitTime = 5f;
                    WaypointProviderSystem.GetNextWaypoint (entity, status);
                    data.Statuses[item.index] = status;
                }
            }
        }

        [BurstCompile]
        private struct MovementJob : IJobParallelFor
        {
            private readonly float dt;
            private readonly float speed;
            private readonly float3 up;
            private readonly float3 one;

            private InjectData data;

            public MovementJob (InjectData data, float dt, float speed)
            {
                this.dt = dt;
                this.speed = speed;
                this.data = data;
                up = Vector3.up;
                one = Vector3.one;
            }

            public void Execute (int index)
            {
                if (index >= data.Statuses.Length)
                {
                    return;
                }

                var status = data.Statuses[index];
                if (status.TotalWaypoints == 0)
                {
                    return;
                }

                if (status.RemainingDistance > 0)
                {
                    status.Position += math.forward (status.Rotation) * speed;
                    status.Matrix = Matrix4x4.TRS (status.Position, status.Rotation, one);
                    status.RemainingDistance -= speed;
                    data.Statuses[index] = status;
                }
                else if (status.NextWaypointIndex == status.TotalWaypoints)
                {
                    if (status.WaitTime >= 0)
                    {
                        status.WaitTime -= dt;
                        data.Statuses[index] = status;
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
                    data.Statuses[index] = status;
                }
            }
        }

        public struct InjectData
        {
            public int Length;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<NavAgent> Statuses;
        }

        [Inject] private InjectData data;

        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            var dt = Time.deltaTime;
            var speed = dt * 2;
            inputDeps = new DetectNextWaypointJob { data = data, needsPath = needsPath, needsWaypoint = needsWaypoint }.Schedule (data.Length, 64, inputDeps);
            inputDeps = new SetNextWaypointJob { data = data, needsWaypoint = needsWaypoint }.Schedule (inputDeps);
            inputDeps = new SetNextPathJob { data = data, needsPath = needsPath }.Schedule (inputDeps);
            inputDeps = new MovementJob (
                data,
                dt,
                speed
            ).Schedule (data.Length, 64, inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager ()
        {
            needsPath.Dispose ();
            needsWaypoint.Dispose ();
        }
    }
}