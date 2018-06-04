using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Transforms2D;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PopulationSpawner : MonoBehaviour {

    public Queue<BuildingData> ResidentialBuildings = new Queue<BuildingData> ();
    public Queue<BuildingData> CommercialBuildings = new Queue<BuildingData> ();
    public MeshInstanceRendererComponent[] Renderers = new MeshInstanceRendererComponent[0];
    // public Dictionary<int, float> RemainingDistance = new Dictionary<int, float> ();
    // public Dictionary<int, float3> Target = new Dictionary<int, float3> ();
    public List<NativeQueue<Vector3>> WaypointQueues = new List<NativeQueue<Vector3>> ();
    public NativeHashMap<int, int> Waypoints;

    public Text SpawnedText;
    public Text AwaitingPathText;
    public Text CachedPathText;
    public int InitialSpawn = 100;
    public int CachedPath = 0;
    public int AwaitingPath = 0;

    int spawned = 0;
    float nextUpdate;
    BuildNavMesh buildNavMesh;
    EntityArchetype personArchetype;
    EntityManager entityManager;
    float3 infinity = new float3 (Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

    private void Start () {
        Waypoints = new NativeHashMap<int, int> (0, Allocator.Persistent);
        entityManager = World.Active.GetOrCreateManager<EntityManager> ();
        personArchetype = entityManager.CreateArchetype (
            typeof (Person),
            typeof (Position),
            typeof (Heading),
            typeof (TransformMatrix),
            typeof (MoveSpeed),
            typeof (MoveForward),
            typeof (IsPendingNavMeshQuery)
        );
        buildNavMesh = GameObject.FindObjectOfType<BuildNavMesh> ();
        StartCoroutine (DoInitialSpawn ());
    }

    IEnumerator DoInitialSpawn () {
        yield return new WaitUntil (() => buildNavMesh.IsBuilt);
        SpawnPeople (InitialSpawn);
    }

    public void SpawnPeople (int quantity) {
        for (int i = 0; i < quantity; i++) {
            SpawnPerson ();
        }
    }

    private void Update () {
        if (Time.time > nextUpdate) {
            nextUpdate = Time.time + 0.5f;
            CachedPathText.text = string.Format ("Cached Paths: {0}", CachedPath);
            AwaitingPathText.text = string.Format ("Awaiting Path: {0} people", AwaitingPath);
            SpawnedText.text = string.Format ("Spawned: {0} people", spawned);
        }
    }

    public NativeQueue<Vector3> GetQueueByEntityId (int id) {
        int queueIndex = 0;
        Waypoints.TryGetValue (id, out queueIndex);
        return WaypointQueues[queueIndex];
    }

    void SpawnPerson () {

        var entity = entityManager.CreateEntity (personArchetype);
        var direction = new float3 (Random.Range (-1f, 1f), 0, Random.Range (-1f, 1f));

        var waypointData = new NativeQueue<Vector3> (Allocator.Persistent);
        WaypointQueues.Add (waypointData);
        Waypoints.TryAdd (entity.Index, WaypointQueues.Count - 1);

        var home = ResidentialBuildings.Dequeue ();
        ResidentialBuildings.Enqueue (home);

        entityManager.SetComponentData (entity, new Person { id = entity.Index, entity = entity });
        entityManager.SetComponentData (entity, new Position { Value = home.Position });
        entityManager.SetComponentData (entity, new Heading { Value = direction });
        entityManager.SetComponentData (entity, new MoveSpeed { speed = 0 });

        entityManager.AddSharedComponentData (entity, Renderers[Random.Range (0, Renderers.Length)].Value);
        spawned++;
    }

    private void OnDestroy () {
        for (int i = WaypointQueues.Count - 1; i > -1; i--) {
            WaypointQueues[i].Dispose ();
            WaypointQueues.RemoveAtSwapBack (i);
        }
        Waypoints.Dispose ();
    }
}