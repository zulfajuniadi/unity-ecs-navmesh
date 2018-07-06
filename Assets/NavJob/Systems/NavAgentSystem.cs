#region

using System.Collections.Concurrent;
using System.Collections.Generic;
using NavJob.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

#endregion

namespace NavJob.Systems
{

    class SetDestinationBarrier : BarrierSystem { }
    class PathSuccessBarrier : BarrierSystem { }
    class PathErrorBarrier : BarrierSystem { }

    [DisableAutoCreation]
    public class NavAgentSystem : JobComponentSystem
    {

        private struct AgentData
        {
            public int index;
            public Entity entity;
            public NavAgent agent;
        }

        private NativeQueue<AgentData> needsWaypoint;
        private ConcurrentDictionary<int, Vector3[]> waypoints = new ConcurrentDictionary<int, Vector3[]> ();
        private NativeHashMap<int, AgentData> pathFindingData;

        [BurstCompile]
        private struct DetectNextWaypointJob : IJobParallelFor
        {
            public int navMeshQuerySystemVersion;
            public InjectData data;
            public NativeQueue<AgentData>.Concurrent needsWaypoint;

            public void Execute (int index)
            {
                var agent = data.Agents[index];
                if (agent.remainingDistance - agent.stoppingDistance > 0 || agent.status != AgentStatus.Moving)
                {
                    return;
                }
                var entity = data.Entities[index];
                if (agent.nextWaypointIndex != agent.totalWaypoints)
                {
                    needsWaypoint.Enqueue (new AgentData { agent = data.Agents[index], entity = entity, index = index });
                }
                else if (navMeshQuerySystemVersion != agent.queryVersion || agent.nextWaypointIndex == agent.totalWaypoints)
                {
                    agent.totalWaypoints = 0;
                    agent.currentWaypoint = 0;
                    agent.status = AgentStatus.Idle;
                    data.Agents[index] = agent;
                }
            }
        }

        private struct SetNextWaypointJob : IJob
        {
            public InjectData data;
            public NativeQueue<AgentData> needsWaypoint;
            public void Execute ()
            {
                while (needsWaypoint.TryDequeue (out AgentData item))
                {
                    var entity = data.Entities[item.index];
                    if (NavAgentSystem.instance.waypoints.TryGetValue (entity.Index, out Vector3[] currentWaypoints))
                    {
                        var agent = data.Agents[item.index];
                        agent.currentWaypoint = currentWaypoints[agent.nextWaypointIndex];
                        agent.remainingDistance = Vector3.Distance (agent.position, agent.currentWaypoint);
                        agent.nextWaypointIndex++;
                        data.Agents[item.index] = agent;
                    }
                }
            }
        }

        [BurstCompile]
        private struct MovementJob : IJobParallelFor
        {
            private readonly float dt;
            private readonly float3 up;
            private readonly float3 one;

            private InjectData data;

            public MovementJob (InjectData data, float dt)
            {
                this.dt = dt;
                this.data = data;
                up = Vector3.up;
                one = Vector3.one;
            }

            public void Execute (int index)
            {
                if (index >= data.Agents.Length)
                {
                    return;
                }

                var agent = data.Agents[index];
                if (agent.status != AgentStatus.Moving)
                {
                    return;
                }

                if (agent.remainingDistance > 0)
                {
                    agent.currentMoveSpeed = Mathf.Lerp (agent.currentMoveSpeed, agent.moveSpeed, dt * agent.acceleration);
                    // todo: deceleration
                    if (agent.nextPosition.x != Mathf.Infinity)
                    {
                        agent.position = agent.nextPosition;
                    }
                    var heading = (Vector3) (agent.currentWaypoint - agent.position);
                    agent.remainingDistance = heading.magnitude;
                    if (agent.remainingDistance > 0.001f)
                    {
                        var targetRotation = Quaternion.LookRotation (heading, up).eulerAngles;
                        targetRotation.x = targetRotation.z = 0;
                        if (agent.remainingDistance < 1)
                        {
                            agent.rotation = Quaternion.Euler (targetRotation);
                        }
                        else
                        {
                            agent.rotation = Quaternion.Slerp (agent.rotation, Quaternion.Euler (targetRotation), dt * agent.rotationSpeed);
                        }
                    }
                    var forward = math.forward (agent.rotation) * agent.currentMoveSpeed * dt;
                    agent.nextPosition = agent.position + forward;
                    data.Agents[index] = agent;
                }
                else if (agent.nextWaypointIndex == agent.totalWaypoints)
                {
                    agent.nextPosition = new float3 { x = Mathf.Infinity, y = Mathf.Infinity, z = Mathf.Infinity };
                    agent.status = AgentStatus.Idle;
                    data.Agents[index] = agent;
                }
            }
        }

        private struct InjectData
        {
            public readonly int Length;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<NavAgent> Agents;
        }

        [Inject] private InjectData data;
        [Inject] private NavMeshQuerySystem querySystem;
        [Inject] SetDestinationBarrier setDestinationBarrier;
        [Inject] PathSuccessBarrier pathSuccessBarrier;
        [Inject] PathErrorBarrier pathErrorBarrier;

        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            var dt = Time.deltaTime;
            inputDeps = new DetectNextWaypointJob { data = data, needsWaypoint = needsWaypoint, navMeshQuerySystemVersion = querySystem.Version }.Schedule (data.Length, 64, inputDeps);
            inputDeps = new SetNextWaypointJob { data = data, needsWaypoint = needsWaypoint }.Schedule (inputDeps);
            inputDeps = new MovementJob (data, dt).Schedule (data.Length, 64, inputDeps);
            return inputDeps;
        }

        /// <summary>
        /// Used to set an agent destination and start the pathfinding process
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agent"></param>
        /// <param name="destination"></param>
        public void SetDestination (Entity entity, NavAgent agent, Vector3 destination, int areas = -1)
        {
            if (pathFindingData.TryAdd (entity.Index, new AgentData { index = entity.Index, entity = entity, agent = agent }))
            {
                var command = setDestinationBarrier.CreateCommandBuffer ();
                agent.status = AgentStatus.PathQueued;
                agent.destination = destination;
                agent.queryVersion = querySystem.Version;
                command.SetComponent<NavAgent> (entity, agent);
                querySystem.RequestPath (entity.Index, agent.position, agent.destination, areas);
            }
        }

        /// <summary>
        /// Static counterpart of SetDestination
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agent"></param>
        /// <param name="destination"></param>
        public static void SetDestinationStatic (Entity entity, NavAgent agent, Vector3 destination, int areas = -1)
        {
            instance.SetDestination (entity, agent, destination, areas);
        }

        protected static NavAgentSystem instance;

        protected override void OnCreateManager (int capacity)
        {
            instance = this;
            querySystem.RegisterPathResolvedCallback (OnPathSuccess);
            querySystem.RegisterPathFailedCallback (OnPathError);
            needsWaypoint = new NativeQueue<AgentData> (Allocator.Persistent);
            pathFindingData = new NativeHashMap<int, AgentData> (0, Allocator.Persistent);
        }

        protected override void OnDestroyManager ()
        {
            needsWaypoint.Dispose ();
            pathFindingData.Dispose ();
        }

        private void SetWaypoint (Entity entity, NavAgent agent, Vector3[] newWaypoints)
        {
            waypoints[entity.Index] = newWaypoints;
            var command = pathSuccessBarrier.CreateCommandBuffer ();
            agent.status = AgentStatus.Moving;
            agent.nextWaypointIndex = 1;
            agent.totalWaypoints = newWaypoints.Length;
            agent.currentWaypoint = newWaypoints[0];
            agent.remainingDistance = Vector3.Distance (agent.position, agent.currentWaypoint);
            command.SetComponent<NavAgent> (entity, agent);
        }

        private void OnPathSuccess (int index, Vector3[] waypoints)
        {
            if (pathFindingData.TryGetValue (index, out AgentData entry))
            {
                SetWaypoint (entry.entity, entry.agent, waypoints);
                pathFindingData.Remove (index);
            }
        }

        private void OnPathError (int index, PathfindingFailedReason reason)
        {
            if (pathFindingData.TryGetValue (index, out AgentData entry))
            {
                entry.agent.status = AgentStatus.Idle;
                var command = pathErrorBarrier.CreateCommandBuffer ();
                command.SetComponent<NavAgent> (entry.entity, entry.agent);
                pathFindingData.Remove (index);
            }
        }
    }
}