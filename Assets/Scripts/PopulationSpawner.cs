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

    public MeshInstanceRendererComponent[] Renderers;

    public int InitialSpawn = 100;

    BuildNavMesh buildNavMesh;
    EntityManager entityManager;
    Entity thisEntity;

    private void Start () {
        Renderers = GameObject.FindObjectsOfType<MeshInstanceRendererComponent> ();
        entityManager = World.Active.GetOrCreateManager<EntityManager> ();
        thisEntity = GetComponent<GameObjectEntity> ().Entity;
        entityManager.AddComponent (thisEntity, typeof (PendingSpawn));
        entityManager.SetComponentData (thisEntity, new PendingSpawn () { Entity = thisEntity, Quantity = 0 });
        buildNavMesh = GameObject.FindObjectOfType<BuildNavMesh> ();
        StartCoroutine (DoInitialSpawn ());
    }

    IEnumerator DoInitialSpawn () {
        yield return new WaitUntil (() => buildNavMesh.IsBuilt);
        SpawnPeople (InitialSpawn);
    }

    public void SpawnPeople (int quantity) {
        var data = entityManager.GetComponentData<PendingSpawn> (thisEntity);
        data.Quantity += quantity;
        entityManager.SetComponentData (thisEntity, data);
    }
}