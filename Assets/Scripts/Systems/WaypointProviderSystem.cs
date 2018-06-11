#region

using System.Collections.Concurrent;
using System.Collections.Generic;
using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Systems
{
    public class WaypointProviderSystem : ComponentSystem
    {
        public Dictionary<int, Vector3[]> CurrentWaypoints = new Dictionary<int, Vector3[]> ();
        public Dictionary<int, Vector3[]> Waypoints = new Dictionary<int, Vector3[]> ();
        private Dictionary<int, NavRequestData> navRequests = new Dictionary<int, NavRequestData> ();
        private ConcurrentQueue<NavRequestData> queuedFlush = new ConcurrentQueue<NavRequestData> ();

        private int lastCount;
        private float nextUpdate;
        private Text waypointCountText;
        private static WaypointProviderSystem instance;
        private Text awaitingText;
        private int depth = 1;
        private int height = 100;
        private readonly int width = 100;

        private Text WaypointCountText
        {
            get
            {
                if (waypointCountText == null)
                {
                    waypointCountText = GameObject.Find ("WaypointCountText").GetComponent<Text> ();
                }

                return waypointCountText;
            }
        }

        private Text AwaitingText
        {
            get
            {
                if (awaitingText == null)
                {
                    awaitingText = GameObject.Find ("AwaitingNavmeshText").GetComponent<Text> ();
                }

                return awaitingText;
            }
        }

        [Inject] private BuildingCacheSystem buildingCache;
        [Inject] NavMeshQuerySystem navQuerySystem;

        public static Dictionary<int, Vector3[]> EntityWaypoints
        {
            get => instance.CurrentWaypoints;
        }

        protected override void OnCreateManager (int capacity)
        {
            instance = this;
            navQuerySystem.RegisterPathResolvedCallback (OnPathResolved);
            navQuerySystem.RegisterPathFailedCallback (OnPathFailed);
        }

        protected override void OnUpdate ()
        {
            if (Time.time > nextUpdate && lastCount != Waypoints.Count)
            {
                nextUpdate = Time.time + 0.5f;
                lastCount = Waypoints.Count;
                WaypointCountText.text = string.Format ("Cached Paths: {0}", lastCount);
                AwaitingText.text = string.Format ("Awaiting Path: {0} people", navQuerySystem.QueueCount);
            }

            while (queuedFlush.TryDequeue (out NavRequestData entry))
            {
                EntityManager.SetComponentData (entry.entity, entry.agent);
            }
        }

        private int GetKey (float3 from, float3 to)
        {
            return Mathf.RoundToInt (from.x) + Mathf.RoundToInt (from.z) * width +
                Mathf.RoundToInt (to.x) * height * width + Mathf.RoundToInt (to.x) * height * width * depth;
        }

        public static void GetNextWaypoint (Entity entity, NavAgent agent)
        {
            instance.GetWaypoint (entity, agent);
        }

        public void GetWaypoint (Entity entity, NavAgent agent)
        {
            var to = buildingCache.GetCommercialBuilding ();
            var key = GetKey (agent.Position, to);
            var navData = new NavRequestData
            {
                entity = entity,
                agent = agent,
                key = key
            };
            if (Waypoints.TryGetValue (key, out Vector3[] corners))
            {
                SetWaypoints (navData, corners);
            }
            else
            {
                navRequests[entity.Index] = navData;
                navQuerySystem.RequestPath (entity.Index, agent.Position, to);
            }
        }

        private void SetWaypoints (NavRequestData data, Vector3[] corners)
        {
            data.agent.NextWaypointIndex = 0;
            data.agent.TotalWaypoints = corners.Length;
            CurrentWaypoints[data.entity.Index] = corners;
            queuedFlush.Enqueue (data);
        }

        void OnPathResolved (int id, Vector3[] corners)
        {
            if (navRequests.TryGetValue (id, out NavRequestData data))
            {
                Waypoints[data.key] = corners;
                SetWaypoints (data, corners);
            }
        }

        void OnPathFailed (int id, PathfindingFailedReason reason)
        {
            Debug.LogError ($"Path failed for: {id} with reason {reason}");
        }
    }

    public struct PendingFlush
    {
        public Entity Entity;
        public NavAgent Agent;
    }

    public struct NavCalculationData
    {
        public Entity entity;
        public NavAgent agent;
        public float3 from;
        public float3 to;
        public int key;

        public NavCalculationData (Entity entity, float3 from, float3 to, int key, NavAgent agent)
        {
            this.entity = entity;
            this.from = from;
            this.to = to;
            this.key = key;
            this.agent = agent;
        }
    }

    public struct NavRequestData
    {
        public Entity entity;
        public NavAgent agent;
        public int key;
    }
}