#region

using System.Collections.Generic;
using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

#endregion

namespace Systems
{

    public struct NavCalculationData
    {
        public Entity Entity;
        public NavAgent Agent;
        public float3 From;
        public float3 To;
        public int Key;

        public NavCalculationData (Entity entity, float3 from, float3 to, int key, NavAgent agent)
        {
            Entity = entity;
            From = from;
            To = to;
            Key = key;
            Agent = agent;
        }
    }

    [RequireComponent (typeof (PendingSpawn))]
    public class NavSystem : ComponentSystem
    {
        private static NavSystem _instance;
        private Text _awaitingText;
        private int _depth = 1;
        private int _height = 100;
        private int maxFlushBatch = 15;
        private int maxFlushSystem = 50;
        private float _nextUpdate;
        private NavMeshPath[] _paths;
        private NativeQueue<PendingFlush> _queuedFlush = new NativeQueue<PendingFlush> (Allocator.Persistent);
        private NativeQueue<PendingFlush>.Concurrent _queuedFlushConc;
        private NativeQueue<NavCalculationData> _queuedSearch = new NativeQueue<NavCalculationData> (Allocator.Persistent);
        private NativeQueue<NavCalculationData>.Concurrent _queuedSearchConc;

        [Inject] private WaypointCacheSystem waypointCache;
        [Inject] private BuildingCacheSystem buildingCache;

        private readonly int width = 100;

        private Text AwaitingText
        {
            get
            {
                if (_awaitingText == null)
                {
                    _awaitingText = GameObject.Find ("AwaitingNavmeshText").GetComponent<Text> ();
                }

                return _awaitingText;
            }
        }

        private int GetKey (float3 from, float3 to)
        {
            return Mathf.RoundToInt (from.x) + Mathf.RoundToInt (from.z) * width +
                Mathf.RoundToInt (to.x) * _height * width + Mathf.RoundToInt (to.x) * _height * width * _depth;
        }

        private bool NeedsNavMeshCalculation (int key, Entity entity, NavAgent agent)
        {
            Vector3[] waypoints;
            if (waypointCache.Waypoints.TryGetValue (key, out waypoints))
            {
                SetWaypoint (waypoints, entity, agent);
                return false;
            }

            return true;
        }

        private void SetWaypoint (Vector3[] waypoints, Entity entity, NavAgent agent)
        {
            agent.NextWaypointIndex = 0;
            agent.TotalWaypoints = waypoints.Length;
            waypointCache.CurrentWaypoints[entity.Index] = waypoints;
            _queuedFlushConc.Enqueue (new PendingFlush { Entity = entity, Agent = agent });
        }

        public static void GetNextWaypoint (Entity entity, NavAgent agent)
        {
            _instance.GetWaypoint (entity, agent);
        }

        protected override void OnUpdate ()
        {
            if (_paths == null)
            {
                return;
            }

            if (Time.time > _nextUpdate)
            {
                _nextUpdate = Time.time + 0.5f;
                try
                {
                    AwaitingText.text = string.Format ("Awaiting Path: {0} people", _queuedSearch.Count);
                }
                catch { }
            }

            int batch = 0;
            PendingFlush entry;
            while (_queuedFlush.TryDequeue (out entry))
            {
                try
                {
                    EntityManager.SetComponentData (entry.Entity, entry.Agent);
                }
                catch
                {
                    var entity = EntityManager.GetEntityByIndex (entry.Agent.EntityIndex);
                    if (entity.Index != 0)
                    {
                        EntityManager.SetComponentData (entity, entry.Agent);
                    }
                }
            }

            NavCalculationData data;
            while (_queuedSearch.TryDequeue (out data))
            {
                if (NavMesh.CalculatePath (data.From, data.To, NavMesh.AllAreas, _paths[batch]))
                {
                    var path = new Vector3[_paths[batch].corners.Length];
                    _paths[batch].corners.CopyTo (path, 0);
                    waypointCache.Waypoints[data.Key] = path;
                    SetWaypoint (path, data.Entity, data.Agent);
                }
                else
                {
                    GetWaypoint (data.Entity, data.Agent);
                }

                batch++;
                if (batch > maxFlushBatch)
                {
                    return;
                }
            }
        }

        public void GetWaypoint (Entity entity, NavAgent agent)
        {
            var to = buildingCache.GetCommercialBuilding ();
            var key = GetKey (agent.Position, to);
            if (NeedsNavMeshCalculation (key, entity, agent))
            {
                var data = new NavCalculationData
                {
                    Entity = entity,
                    From = agent.Position,
                    To = to,
                    Key = key,
                    Agent = agent
                };
                _queuedSearchConc.Enqueue (data);
            }
        }

        protected override void OnCreateManager (int i)
        {
            _instance = this;
            _queuedFlushConc = _queuedFlush;
            _queuedSearchConc = _queuedSearch;

            _paths = new NavMeshPath[maxFlushSystem];
            for (var j = 0; j < maxFlushBatch + 2; j++)
            {
                _paths[j] = new NavMeshPath ();
            }
        }

        protected override void OnDestroyManager ()
        {
            _queuedFlush.Dispose ();
            _queuedSearch.Dispose ();
        }

        protected override void OnStopRunning () { }

        private struct PendingFlush
        {
            public Entity Entity;
            public NavAgent Agent;
        }
    }
}