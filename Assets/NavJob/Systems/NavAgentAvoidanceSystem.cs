using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using NavJob.Components;

namespace NavJob.Systems
{
    [DisableAutoCreation]
    public class NavAgentAvoidanceSystem : JobComponentSystem
    {

        public NativeMultiHashMap<int, int> indexMap;
        public NativeMultiHashMap<int, float3> nextPositionMap;
        NavMeshQuery navMeshQuery;

        [BurstCompile]
        struct NavAgentAvoidanceJob : IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            [ReadOnly] public EntityArray entities;
            public ComponentDataArray<NavAgent> agents;
            public ComponentDataArray<NavAgentAvoidance> avoidances;
            [ReadOnly] public NativeMultiHashMap<int, int> indexMap;
            [ReadOnly] public NativeMultiHashMap<int, float3> nextPositionMap;
            [ReadOnly] public NavMeshQuery navMeshQuery;
            public float dt;
            public void ExecuteFirst(int index) { }

            public void ExecuteNext(int firstIndex, int index)
            {
                var agent = agents[index];
                var avoidance = avoidances[index];
                var move = Vector3.left;
                if (index % 2 == 1)
                {
                    move = Vector3.right;
                }
                float3 drift = agent.rotation * (Vector3.forward + move) * agent.currentMoveSpeed * dt;
                if (agent.nextWaypointIndex != agent.totalWaypoints)
                {
                    var offsetWaypoint = agent.currentWaypoint + drift;
                    var waypointInfo = navMeshQuery.MapLocation(offsetWaypoint, Vector3.one * 3f, 0, agent.areaMask);
                    if (navMeshQuery.IsValid(waypointInfo))
                    {
                        agent.currentWaypoint = waypointInfo.position;
                    }
                }
                agent.currentMoveSpeed = Mathf.Max(agent.currentMoveSpeed / 2f, 0.5f);
                var positionInfo = navMeshQuery.MapLocation(agent.position + drift, Vector3.one * 3f, 0, agent.areaMask);
                if (navMeshQuery.IsValid(positionInfo))
                {
                    agent.nextPosition = positionInfo.position;
                }
                else
                {
                    agent.nextPosition = agent.position;
                }
                agents[index] = agent;
            }
        }

        [BurstCompile]
        struct HashPositionsJob : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<NavAgent> agents;
            public ComponentDataArray<NavAgentAvoidance> avoidances;
            public NativeMultiHashMap<int, int>.Concurrent indexMap;
            public NativeMultiHashMap<int, float3>.Concurrent nextPositionMap;
            public int mapSize;

            public void Execute(int index)
            {
                var agent = agents[index];
                var avoidance = avoidances[index];
                var hash = Hash(agent.position, avoidance.radius);
                indexMap.Add(hash, index);
                nextPositionMap.Add(hash, agent.nextPosition);
                avoidance.partition = hash;
                avoidances[index] = avoidance;
            }
            public int Hash(float3 position, float radius)
            {
                int ix = Mathf.RoundToInt((position.x / radius) * radius);
                int iz = Mathf.RoundToInt((position.z / radius) * radius);
                return ix * mapSize + iz;
            }
        }

        struct InjectData
        {
            public readonly int Length;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<NavAgent> Agents;
            public ComponentDataArray<NavAgentAvoidance> Avoidances;
        }

        [Inject] InjectData agent;
        [Inject] NavMeshQuerySystem querySystem;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (agent.Length > 0)
            {
                indexMap.Clear();
                nextPositionMap.Clear();
                var hashPositionsJob = new HashPositionsJob
                {
                    mapSize = querySystem.MaxMapWidth,
                    agents = agent.Agents,
                    avoidances = agent.Avoidances,
                    indexMap = indexMap,
                    nextPositionMap = nextPositionMap
                };
                var dt = Time.deltaTime;
                var hashPositionsJobHandle = hashPositionsJob.Schedule(agent.Length, 64, inputDeps);
                var avoidanceJob = new NavAgentAvoidanceJob
                {
                    dt = dt,
                    indexMap = indexMap,
                    nextPositionMap = nextPositionMap,
                    agents = agent.Agents,
                    avoidances = agent.Avoidances,
                    entities = agent.Entities,
                    navMeshQuery = navMeshQuery
                };
                var avoidanceJobHandle = avoidanceJob.Schedule(indexMap, 64, hashPositionsJobHandle);
                return avoidanceJobHandle;
            }
            return inputDeps;
        }

        protected override void OnCreateManager(int capacity)
        {
            navMeshQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 128);
            indexMap = new NativeMultiHashMap<int, int>(100 * 1024, Allocator.Persistent);
            nextPositionMap = new NativeMultiHashMap<int, float3>(100 * 1024, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {

            if (indexMap.IsCreated) indexMap.Dispose();
            if (nextPositionMap.IsCreated) nextPositionMap.Dispose();
            navMeshQuery.Dispose();
        }
    }

}
