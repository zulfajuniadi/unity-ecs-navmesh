using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public class UpdateAwaitingPathCountSystem : ComponentSystem {

    PopulationSpawner _spawner;
    PopulationSpawner spawner {
        get {
            if (_spawner == null) {
                _spawner = GameObject.FindObjectOfType<PopulationSpawner> ();
            }
            return _spawner;
        }
    }

    struct Data {
        public int Length;
        public ComponentDataArray<IsPendingNavMeshQuery> States;
    }

    [Inject] private Data data;
    protected override void OnUpdate () {
        spawner.AwaitingPath = data.Length;
    }

    protected override void OnStopRunning () {
        spawner.AwaitingPath = 0;
        base.OnStopRunning ();
    }
}