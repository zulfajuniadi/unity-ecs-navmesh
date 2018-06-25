using System.Collections.Concurrent;
using System.Collections.Generic;
using NavJob.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace NavJob.Systems
{

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
            [ReadOnly] public NativeMultiHashMap<int, int> indexMap;
            [ReadOnly] public NativeMultiHashMap<int, float3> nextPositionMap;
            [ReadOnly] public NavMeshQuery navMeshQuery;
            public float dt;
            public void ExecuteFirst (int index) { }

            public void ExecuteNext (int firstIndex, int index)
            {
                if (agents[index].avoidanceDiameter == 0) return;
                var move = Vector3.left;
                if (index % 2 == 1)
                {
                    move = Vector3.right;
                }
                var data = agents[index];
                float3 drift = data.rotation * (Vector3.forward + move) * data.currentMoveSpeed * dt;
                if (data.nextWaypointIndex != data.totalWaypoints)
                {
                    var offsetWaypoint = data.currentWaypoint + drift;
                    var waypointInfo = navMeshQuery.MapLocation (offsetWaypoint, Vector3.one * 3f, 0, data.areaMask);
                    if (navMeshQuery.IsValid (waypointInfo))
                    {
                        data.currentWaypoint = waypointInfo.position;
                    }
                }
                data.currentMoveSpeed = Mathf.Max (data.currentMoveSpeed / 2f, 0.5f);
                var positionInfo = navMeshQuery.MapLocation (data.position + drift, Vector3.one * 3f, 0, data.areaMask);
                if (navMeshQuery.IsValid (positionInfo))
                {
                    data.nextPosition = positionInfo.position;
                }
                else
                {
                    data.nextPosition = data.position;
                }
                agents[index] = data;
            }
        }

        [BurstCompile]
        struct HashPositionsJob : IJobParallelFor
        {
            public ComponentDataArray<NavAgent> agents;
            public NativeMultiHashMap<int, int>.Concurrent indexMap;
            public NativeMultiHashMap<int, float3>.Concurrent nextPositionMap;
            public int mapSize;

            public void Execute (int index)
            {
                var agent = agents[index];
                if (agent.avoidanceDiameter == 0) return;
                var hash = Hash (agent.position, agent.avoidanceDiameter);
                indexMap.Add (hash, index);
                nextPositionMap.Add (hash, agent.nextPosition);
                agent.partition = hash;
                agents[index] = agent;
            }
            public int Hash (float3 position, float radius)
            {
                int ix = Mathf.RoundToInt ((position.x / radius) * radius);
                int iz = Mathf.RoundToInt ((position.z / radius) * radius);
                return ix * mapSize + iz;
            }
        }

        struct InjectData
        {
            public int Length;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<NavAgent> Agents;
        }

        [Inject] InjectData data;
        [Inject] NavMeshQuerySystem querySystem;
        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            if (data.Length > 0)
            {
                indexMap.Clear ();
                nextPositionMap.Clear ();
                var hashPositionsJob = new HashPositionsJob
                {
                    mapSize = querySystem.MaxMapWidth,
                    agents = data.Agents,
                    indexMap = indexMap,
                    nextPositionMap = nextPositionMap
                };
                var dt = Time.deltaTime;
                var hashPositionsJobHandle = hashPositionsJob.Schedule (data.Length, 64, inputDeps);
                var avoidanceJob = new NavAgentAvoidanceJob
                {
                    dt = dt,
                    indexMap = indexMap,
                    nextPositionMap = nextPositionMap,
                    agents = data.Agents,
                    entities = data.Entities,
                    navMeshQuery = navMeshQuery
                };
                var avoidanceJobHandle = avoidanceJob.Schedule (indexMap, 64, hashPositionsJobHandle);
                return avoidanceJobHandle;
            }
            return inputDeps;
        }

        protected override void OnCreateManager (int capacity)
        {
            navMeshQuery = new NavMeshQuery (NavMeshWorld.GetDefaultWorld (), Allocator.Persistent, 128);
            indexMap = new NativeMultiHashMap<int, int> (100 * 1024, Allocator.Persistent);
            nextPositionMap = new NativeMultiHashMap<int, float3> (100 * 1024, Allocator.Persistent);
        }

        protected override void OnDestroyManager ()
        {

            if (indexMap.IsCreated) indexMap.Dispose ();
            if (nextPositionMap.IsCreated) nextPositionMap.Dispose ();
            navMeshQuery.Dispose ();
        }
    }

}