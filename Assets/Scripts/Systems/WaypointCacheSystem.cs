using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class WaypointCacheSystem : ComponentSystem {

    public Dictionary<int, Waypoint> Waypoints = new Dictionary<int, Waypoint> ();

    Text waypointCountText;
    Text WaypointCountText {
        get {
            if (waypointCountText == null) {
                waypointCountText = GameObject.Find ("WaypointCountText").GetComponent<Text> ();
            }
            return waypointCountText;
        }
    }

    protected override void OnDestroyManager () {
        foreach (var entry in Waypoints) {
            entry.Value.Data.Dispose ();
        }
    }

    struct InjectData {
        ComponentDataArray<PendingSpawn> dummy;
    }

    int lastCount;
    float nextUpdate;

    protected override void OnUpdate () {
        if (Time.time > nextUpdate && lastCount != Waypoints.Count) {
            nextUpdate = Time.time + 0.5f;
            lastCount = Waypoints.Count;
            WaypointCountText.text = string.Format ("Cached Paths: {0}", lastCount);
        }
    }
}