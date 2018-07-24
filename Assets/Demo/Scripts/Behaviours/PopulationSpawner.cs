using System.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Demo.Behaviours
{
    public class PopulationSpawner : MonoBehaviour
    {

        public MeshInstanceRendererComponent[] Renderers;

        public int InitialSpawn = 10000;
        public float AgentStoppingDistance = 0.1f;
        public float AgentAcceleration = 1;
        public float AgentMoveSpeed = 4;
        public float AgentRotationSpeed = 10;
        public float AgentAvoidanceDiameter = 0.7f;
        [HideInInspector]
        public int AgentAreaMask = ~(1 << 1);

        BuildNavMesh buildNavMesh;
        EntityManager entityManager;
        Entity thisEntity;

        private void Start ()
        {
            Renderers = FindObjectsOfType<MeshInstanceRendererComponent> ();
            entityManager = World.Active.GetOrCreateManager<EntityManager> ();
            thisEntity = GetComponent<GameObjectEntity> ().Entity;
            entityManager.AddComponent (thisEntity, typeof (PendingSpawn));
            entityManager.SetComponentData (thisEntity, new PendingSpawn ()
            {
                Quantity = InitialSpawn,
                    AgentStoppingDistance = AgentStoppingDistance,
                    AgentAcceleration = AgentAcceleration,
                    AgentMoveSpeed = AgentMoveSpeed,
                    AgentRotationSpeed = AgentRotationSpeed,
                    AgentAreaMask = AgentAreaMask,
                    AgentAvoidanceDiameter = AgentAvoidanceDiameter,
            });
            buildNavMesh = FindObjectOfType<BuildNavMesh> ();
        }

        public void SpawnPeople (int quantity)
        {
            var data = entityManager.GetComponentData<PendingSpawn> (thisEntity);
            data.Quantity += quantity;
            entityManager.SetComponentData (thisEntity, data);
        }
    }
}