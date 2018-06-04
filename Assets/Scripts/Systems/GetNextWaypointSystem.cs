using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;

public class GetNextWaypointSystem : ComponentSystem {

    PopulationSpawner _spawner;
    PopulationSpawner spawner {
        get {
            if (_spawner == null) {
                _spawner = GameObject.FindObjectOfType<PopulationSpawner> ();
            }
            return _spawner;
        }
    }
    float3 infinity = new float3 (Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

    public struct Data {
        public int Length;
        public ComponentDataArray<Person> People;
        public ComponentDataArray<Position> Positions;
        public ComponentDataArray<Heading> Headings;
        public ComponentDataArray<MoveSpeed> Speed;
        public ComponentDataArray<CurrentWaypointData> CurrentWaypoint;
        public ComponentDataArray<IsPendingNextWaypoint> IsPendingWaypoint;
    }

    [Inject] private Data data;

    protected override void OnUpdate () {
        for (int index = 0; index < data.Length; ++index) {
            var id = data.People[index].id;
            var currentWaypoint = data.CurrentWaypoint[index];
            var queue = spawner.GetQueueByEntityId (id);
            var nextTarget = queue.Dequeue ();
            var heading = nextTarget - (Vector3) data.Positions[index].Value;
            currentWaypoint.IsValid = 1;
            currentWaypoint.RemainingWaypoints = queue.Count;
            data.Headings[index] = new Heading (heading.normalized);
            currentWaypoint.RemainingDistance = heading.magnitude;
            data.CurrentWaypoint[index] = currentWaypoint;
            data.Speed[index] = new MoveSpeed () { speed = 4 };
            PostUpdateCommands.RemoveComponent<IsPendingNextWaypoint> (data.People[index].entity);
            PostUpdateCommands.AddComponent (data.People[index].entity, new IsMovingToWaypoint ());
        }
    }
}