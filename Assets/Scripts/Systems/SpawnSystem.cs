using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class SpawnSystem : ComponentSystem {

    Waypoint dummy;

    Text spawnedText;
    Text SpawnedText {
        get {
            if (spawnedText == null) {
                spawnedText = GameObject.Find ("SpawnedText").GetComponent<Text> ();
            }
            return spawnedText;
        }
    }

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

    public class SpawnBarrier : BarrierSystem { }

    int spawned = 0;
    int lastSpawned = 0;
    public int pendingSpawn = 0;
    float nextUpdate = 0;
    EntityArchetype pendingArchetype;

    protected override void OnCreateManager (int i) {
        base.OnCreateManager (i);
        dummy = new Waypoint () { Data = new NativeList<Vector3> (Allocator.Persistent) };
        pendingArchetype = manager.CreateArchetype (
            typeof (Position),
            typeof (Rotation),
            typeof (WaypointStatus),
            typeof (NeedsPathTag)
        );
    }

    struct InjectData {
        public int Length;
        public ComponentDataArray<PendingSpawn> Spawn;
    }

    [Inject] InjectData data;
    [Inject] BuildingCacheSystem buidlings;
    [Inject] SpawnBarrier barrier;

    Vector3 one = Vector3.one;

    protected override void OnUpdate () {
        if (Time.time > nextUpdate && lastSpawned != spawned) {
            nextUpdate = Time.time + 0.5f;
            lastSpawned = spawned;
            SpawnedText.text = string.Format ("Spawned: {0} people", spawned);
        }
        var spawnData = data.Spawn[0];
        pendingSpawn = spawnData.Quantity;
        if (spawner.Renderers.Length == 0) return;
        if (spawnData.Quantity == 0) return;
        if (buidlings.ResidentialBuildings.Count == 0) return;
        var quantity = spawnData.Quantity > 50 ? 50 : spawnData.Quantity;
        spawnData.Quantity -= quantity;
        data.Spawn[0] = spawnData;
        var command = barrier.CreateCommandBuffer ();
        for (int i = 0; i < quantity; i++) {
            spawned++;
            var pos = buidlings.GetResidentialBuilding ();
            var matrix = Matrix4x4.TRS (pos, Quaternion.identity, one);
            command.CreateEntity (pendingArchetype);
            command.SetComponent (new Position { Value = pos });
            command.SetComponent (new Rotation { Value = Quaternion.identity });
            command.SetComponent (new WaypointStatus { Matrix = matrix });
            command.AddSharedComponent (dummy);
            command.AddSharedComponent (spawner.Renderers[Random.Range (0, spawner.Renderers.Length)].Value);
        }
    }

    protected override void OnDestroyManager () {
        dummy.Data.Dispose ();
    }
}