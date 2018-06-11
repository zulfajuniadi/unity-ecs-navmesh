using System.Collections;
using Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Behaviours
{
    public class PopulationSpawner : MonoBehaviour
    {

        public MeshInstanceRendererComponent[] Renderers;

        public int InitialSpawn = 100;

        BuildNavMesh buildNavMesh;
        EntityManager entityManager;
        Entity thisEntity;

        private void Start ()
        {
            Renderers = FindObjectsOfType<MeshInstanceRendererComponent> ();
            entityManager = World.Active.GetOrCreateManager<EntityManager> ();
            thisEntity = GetComponent<GameObjectEntity> ().Entity;
            entityManager.AddComponent (thisEntity, typeof (PendingSpawn));
            entityManager.SetComponentData (thisEntity, new PendingSpawn () { Entity = thisEntity, Quantity = 0 });
            buildNavMesh = FindObjectOfType<BuildNavMesh> ();
            StartCoroutine (DoInitialSpawn ());
        }

        IEnumerator DoInitialSpawn ()
        {
            yield return new WaitUntil (() => buildNavMesh.IsBuilt);
            SpawnPeople (InitialSpawn);
        }

        public void SpawnPeople (int quantity)
        {
            var data = entityManager.GetComponentData<PendingSpawn> (thisEntity);
            data.Quantity += quantity;
            entityManager.SetComponentData (thisEntity, data);
        }
    }
}