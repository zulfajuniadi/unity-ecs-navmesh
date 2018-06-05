using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshSystem : ComponentSystem {

    int maxBatch = 15;
    int id;
    float4 key;
    NavMeshPath path = new NavMeshPath ();

    float4 GetKey (float3 from, float3 to) {
        return new float4 (
            Mathf.Round (from.x),
            Mathf.Round (from.z),
            Mathf.Round (to.x),
            Mathf.Round (to.z)
        );
    }
    struct Data {
        public int Length;
        public EntityArray Entities;
        public ComponentDataArray<WaypointStatus> Status;
        public ComponentDataArray<MoveSpeed> MoveSpeeds;
        [ReadOnly] public ComponentDataArray<Position> Positions;
    }

    [Inject, ReadOnly] private BuildingCacheSystem buildingCache;
    [Inject, ReadOnly] private WaypointCacheSystem waypointCache;
    [Inject, ReadOnly] private SpawnSystem spawn;
    [Inject] private Data data;

    bool allocated = false;
    NativeList<int> indexes;
    NativeList<float4> keys;
    NativeList<Entity> queued;
    NativeList<Vector3> from;
    NativeList<Vector3> to;
    List<Waypoint> results = new List<Waypoint> ();

    [ComputeJobOptimization]
    protected override void OnUpdate () {
        if (spawn.pendingSpawn > 0) return;
        int i = 0;
        int batchSize = data.Length;
        batchSize = math.min (maxBatch, data.Length);
        allocated = false;
        for (int index = 0; index < data.Length; ++index) {
            var status = data.Status[index];
            if (status.StateFlag != 0) continue;
            Vector3 buildingData = buildingCache.GetCommercialBuilding ();
            Entity entity = data.Entities[index];
            id = entity.Index;
            key = GetKey (data.Positions[index].Value, buildingData);
            Waypoint waypoints;
            if (waypointCache.Waypoints.TryGetValue (key, out waypoints)) {
                status.StateFlag = 1;
                status.NextWaypointIndex = 0;
                status.TotalWaypoints = waypoints.Data.Length;
                data.Status[index] = status;
                PostUpdateCommands.SetSharedComponent<Waypoint> (entity, waypoints);
            } else {
                if (!allocated) {
                    results.Clear ();
                    allocated = true;
                    indexes = new NativeList<int> (Allocator.Temp);
                    keys = new NativeList<float4> (Allocator.Temp);
                    queued = new NativeList<Entity> (Allocator.Temp);
                    from = new NativeList<Vector3> (Allocator.Temp);
                    to = new NativeList<Vector3> (Allocator.Temp);
                }
                indexes.Add (index);
                keys.Add (key);
                queued.Add (data.Entities[index]);
                from.Add (data.Positions[index].Value);
                to.Add (buildingData);
                i++;
                if (i == batchSize) break;
            }
        }

        if (allocated) {
            for (int k = 0; k < queued.Length; k++) {
                Waypoint waypoints;
                if (waypointCache.Waypoints.TryGetValue (keys[k], out waypoints)) {
                    var st = data.Status[indexes[k]];
                    st.StateFlag = 1;
                    st.NextWaypointIndex = 0;
                    st.TotalWaypoints = waypoints.Data.Length;
                    PostUpdateCommands.SetSharedComponent<Waypoint> (queued[k], waypoints);
                } else {
                    var path = new NavMeshPath ();
                    NavMesh.CalculatePath (from[k], to[k], NavMesh.AllAreas, path);
                    var list = new NativeList<Vector3> (Allocator.Persistent);
                    foreach (var pos in path.corners) {
                        list.Add (pos);
                    }
                    waypoints = new Waypoint () {
                        Data = list
                    };
                    waypointCache.Waypoints.Add (keys[k], waypoints);
                    results.Add (waypoints);
                }
            }
            for (int j = 0; j < queued.Length; j++) {
                var st = data.Status[indexes[j]];
                st.StateFlag = 1;
                st.NextWaypointIndex = 0;
                st.TotalWaypoints = results[j].Data.Length;
                data.Status[indexes[j]] = st;
                PostUpdateCommands.SetSharedComponent<Waypoint> (queued[j], results[j]);
            }
            indexes.Dispose ();
            keys.Dispose ();
            queued.Dispose ();
            from.Dispose ();
            to.Dispose ();
        }
    }
}