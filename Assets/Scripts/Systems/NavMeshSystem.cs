using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshSystem : ComponentSystem {

    PopulationSpawner _spawner;
    PopulationSpawner spawner {
        get {
            if (_spawner == null) {
                _spawner = GameObject.FindObjectOfType<PopulationSpawner> ();
            }
            return _spawner;
        }
    }

    EntityManager _manager;
    EntityManager manager {
        get {
            if (_manager == null) {
                _manager = World.Active.GetOrCreateManager<EntityManager> ();
            }
            return _manager;
        }
    }

    struct Data {
        public int Length;
        public ComponentDataArray<Person> People;
        public ComponentDataArray<Position> Positions;
        public ComponentDataArray<IsPendingNavMeshQuery> IsPending;
        // [ReadOnly] public SharedComponentDataArray<WayPointData> Waypoints;
    }

    Dictionary<float4, Vector3[]> pathCaches = new Dictionary<float4, Vector3[]> ();
    float batch = 10;
    int id;
    float4 key;
    NavMeshPath path = new NavMeshPath ();
    Vector3[] foundPath;
    BuildingData buildingData;

    float4 GetKey (float3 from, float3 to) {
        return new float4 (
            Mathf.Round (from.x),
            Mathf.Round (from.z),
            Mathf.Round (to.x),
            Mathf.Round (to.z)
        );
    }

    [Inject] private Data data;

    protected override void OnUpdate () {
        spawner.CachedPath = pathCaches.Count;
        int i = 0;
        for (int index = 0; index < data.Length; ++index) {
            buildingData = spawner.CommercialBuildings.Dequeue ();
            spawner.CommercialBuildings.Enqueue (buildingData);
            id = data.People[index].id;
            key = GetKey (data.Positions[index].Value, buildingData.Position);
            if (pathCaches.TryGetValue (key, out foundPath)) {
                var queue = spawner.GetQueueByEntityId (id);
                foreach (var corner in foundPath) {
                    queue.Enqueue (corner);
                }
                PostUpdateCommands.AddComponent<IsMovingToWaypoint> (data.People[index].entity, new IsMovingToWaypoint ());
                PostUpdateCommands.AddComponent<CurrentWaypointData> (data.People[index].entity, new CurrentWaypointData () { IsValid = 0, RemainingWaypoints = queue.Count });
                PostUpdateCommands.RemoveComponent<IsPendingNavMeshQuery> (data.People[index].entity);
            } else {
                if (NavMesh.CalculatePath (data.Positions[index].Value, buildingData.Position, NavMesh.AllAreas, path)) {
                    var queue = spawner.GetQueueByEntityId (id);
                    foreach (var corner in path.corners) {
                        queue.Enqueue (corner);
                    }
                    pathCaches[key] = path.corners;
                }
                PostUpdateCommands.AddComponent (data.People[index].entity, new IsPendingWaypoint ());
                PostUpdateCommands.RemoveComponent<IsPendingNavMeshQuery> (data.People[index].entity);
                i++;
                if (i > batch) {
                    return;
                }
            }
        }
    }
}