using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public class PathPendingSystem : ComponentSystem {

    PopulationSpawner _spawner;
    PopulationSpawner spawner {
        get {
            if (_spawner == null) {
                _spawner = GameObject.FindObjectOfType<PopulationSpawner> ();
            }
            return _spawner;
        }
    }

    public struct Data {
        public int Length;
        public ComponentDataArray<Person> People;
        public ComponentDataArray<IsPendingWaypoint> IsPendingWaypoint;
    }

    [Inject] private Data data;
    NavMeshPath path = new NavMeshPath ();
    protected override void OnUpdate () {
        for (int index = 0; index < data.Length; ++index) {
            var count = spawner.GetQueueByEntityId (data.People[index].id).Count;
            if (count > 0) {
                PostUpdateCommands.AddComponent<IsMovingToWaypoint> (data.People[index].entity, new IsMovingToWaypoint ());
                PostUpdateCommands.AddComponent<CurrentWaypointData> (data.People[index].entity, new CurrentWaypointData () { IsValid = 0, RemainingWaypoints = count });
                PostUpdateCommands.RemoveComponent<IsPendingWaypoint> (data.People[index].entity);
            }
        }
    }
}