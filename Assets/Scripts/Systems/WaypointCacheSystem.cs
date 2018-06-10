#region

using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Systems
{
    public class WaypointCacheSystem : ComponentSystem
    {
        public Dictionary<int, Vector3[]> CurrentWaypoints = new Dictionary<int, Vector3[]> ();
        public Dictionary<int, Vector3[]> Waypoints = new Dictionary<int, Vector3[]> ();

        private int lastCount;
        private float nextUpdate;
        private Text waypointCountText;
        private static WaypointCacheSystem instance;

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

        public static Dictionary<int, Vector3[]> EntityWaypoints
        {
            get => instance.CurrentWaypoints;
        }

        protected override void OnCreateManager (int capacity)
        {
            instance = this;
        }

        protected override void OnUpdate ()
        {
            if (Time.time > nextUpdate && lastCount != Waypoints.Count)
            {
                nextUpdate = Time.time + 0.5f;
                lastCount = Waypoints.Count;
                WaypointCountText.text = string.Format ("Cached Paths: {0}", lastCount);
            }
        }
    }
}