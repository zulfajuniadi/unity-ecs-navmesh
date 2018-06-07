using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public struct NavCalculationData {
    public Entity entity;
    public float3 from;
    public float3 to;
    public int key;
}

public class NavMeshSystem : ComponentSystem {

    Text awaitingText;
    Text AwaitingText {
        get {
            if (awaitingText == null) {
                awaitingText = GameObject.Find ("AwaitingNavmeshText").GetComponent<Text> ();
            }
            return awaitingText;
        }
    }

    int maxFlushBatch = 20;
    int maxFlushSystem = 50;
    int id;
    int awaiting = 0;
    float nextUpdate;

    NavMeshPath path = new NavMeshPath ();

    int width = 100;
    int height = 100;
    int depth = 100;

    public int GetKey (float3 from, float3 to) {
        return Mathf.RoundToInt (from.x) + Mathf.RoundToInt (from.z) * width + Mathf.RoundToInt (to.x) * height * width + Mathf.RoundToInt (to.x) * height * width * depth;
    }

    public Queue<NavCalculationData> PendingCalculations = new Queue<NavCalculationData> ();

    EntityManager _manager;
    EntityManager manager {
        get {
            if (_manager == null) {
                _manager = World.Active.GetOrCreateManager<EntityManager> ();
            }
            return _manager;
        }
    }

    IEnumerator FlushQueue () {
        while (true) {
            if (Time.time > nextUpdate) {
                nextUpdate = Time.time + 0.5f;
                AwaitingText.text = string.Format ("Awaiting Path: {0} people", awaiting + PendingCalculations.Count);
            }
            int i = 0;
            int j = 0;
            while (PendingCalculations.Count > 0) {
                var data = PendingCalculations.Dequeue ();
                if (NeedsNavMeshCalculation (data.key, data.entity)) {
                    Waypoint waypoints;
                    var path = new NavMeshPath ();
                    NavMesh.CalculatePath (data.from, data.to, NavMesh.AllAreas, path);
                    var list = new NativeList<Vector3> (Allocator.Persistent);
                    foreach (var pos in path.corners) {
                        list.Add (pos);
                    }
                    waypoints = new Waypoint () {
                        Data = list
                    };
                    waypointCache.Waypoints.Add (data.key, waypoints);
                    setWaypoint (waypoints, data.entity);
                    i++;
                    if (i > maxFlushBatch) break;
                } else {
                    j++;
                    if (j > maxFlushSystem) break;
                }
            }
            yield return new WaitForEndOfFrame ();
        }
    }

    static void Enqueue (NavCalculationData data) {
        instance.PendingCalculations.Enqueue (data);
    }

    private bool NeedsNavMeshCalculation (int key, Entity entity, bool usePost = false) {
        Waypoint waypoints;
        if (waypointCache.Waypoints.TryGetValue (key, out waypoints)) {
            setWaypoint (waypoints, entity, usePost);
            return false;
        }
        return true;
    }

    private void setWaypoint (Waypoint waypoints, Entity entity, bool usePost = false) {
        var status = manager.GetComponentData<WaypointStatus> (entity);
        status.NextWaypointIndex = 0;
        status.TotalWaypoints = waypoints.Data.Length;
        status.WaitTime = Random.Range (15f, 60f);
        if (!usePost) {
            manager.AddComponent (entity, typeof (NeedsWaypointTag));
            manager.SetComponentData<WaypointStatus> (entity, status);
            manager.SetSharedComponentData<Waypoint> (entity, waypoints);
            if (manager.HasComponent<IsPathFindingTag> (entity)) {
                manager.RemoveComponent<IsPathFindingTag> (entity);
            }
        } else {
            PostUpdateCommands.AddComponent<NeedsWaypointTag> (entity, new NeedsWaypointTag ());
            PostUpdateCommands.SetComponent<WaypointStatus> (entity, status);
            PostUpdateCommands.SetSharedComponent<Waypoint> (entity, waypoints);
            PostUpdateCommands.RemoveComponent<IsPathFindingTag> (entity);
        }
    }

    static NavMeshSystem instance;

    protected override void OnCreateManager (int i) {
        instance = this;
        GameObject.FindObjectOfType<PopulationSpawner> ().StartCoroutine (FlushQueue ());
    }

    struct Data {
        public int Length;
        [ReadOnly] public EntityArray Entities;
        public ComponentDataArray<WaypointStatus> Status;
        public ComponentDataArray<NeedsPathTag> Tag;
        [ReadOnly] public ComponentDataArray<Position> Positions;
    }

    [Inject] private BuildingCacheSystem buildingCache;
    [Inject] private WaypointCacheSystem waypointCache;
    [Inject] private SpawnSystem spawn;
    [Inject] private Data data;

    protected override void OnUpdate () {
        awaiting = data.Length;
        if (spawn.pendingSpawn > 0) return;
        int i = 0;
        for (int index = 0; index < data.Length; ++index) {
            // try {
            Entity entity = data.Entities[index];
            var to = buildingCache.GetCommercialBuilding ();
            var key = GetKey (data.Positions[index].Value, to);
            PostUpdateCommands.AddComponent<IsPathFindingTag> (entity, new IsPathFindingTag ());
            PostUpdateCommands.RemoveComponent<NeedsPathTag> (entity);
            if (NeedsNavMeshCalculation (key, entity, true)) {
                i++;
                if (i > maxFlushSystem) break;
                Enqueue (new NavCalculationData { from = data.Positions[index].Value, entity = entity, to = to, key = key });
            }
            // } catch { break; }
        }
    }

    protected override void OnStopRunning () {
        awaiting = 0;
    }

}