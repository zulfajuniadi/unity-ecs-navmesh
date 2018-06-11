#region

using Behaviours;
using Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Systems
{
    public class SpawnSystem : ComponentSystem
    {
        public int pendingSpawn;
        private EntityManager _manager;

        private PopulationSpawner _spawner;
        private int _lastSpawned;
        private float _nextUpdate;

        private Vector3 one = Vector3.one;
        private EntityArchetype _agent;

        [Inject] private BuildingCacheSystem _buildings;
        [Inject] private InjectData data;
        [Inject] private WaypointProviderSystem waypointProvider;

        private int spawned;

        private Text spawnedText;

        private Text SpawnedText
        {
            get
            {
                if (spawnedText == null)
                {
                    spawnedText = GameObject.Find ("SpawnedText").GetComponent<Text> ();
                }

                return spawnedText;
            }
        }

        private PopulationSpawner Getspawner ()
        {
            if (_spawner == null)
            {
                _spawner = Object.FindObjectOfType<PopulationSpawner> ();
            }

            return _spawner;
        }

        private EntityManager Getmanager ()
        {
            if (_manager == null)
            {
                _manager = World.Active.GetOrCreateManager<EntityManager> ();
            }

            return _manager;
        }

        protected override void OnCreateManager (int capacity)
        {
            base.OnCreateManager (capacity);
            _agent = Getmanager ().CreateArchetype (
                typeof (NavAgent)
            );
        }

        protected override void OnUpdate ()
        {
            if (Time.time > _nextUpdate && _lastSpawned != spawned)
            {
                _nextUpdate = Time.time + 0.5f;
                _lastSpawned = spawned;
                SpawnedText.text = $"Spawned: {spawned} people";
            }

            if (Getspawner ().Renderers.Length == 0)
            {
                return;
            }

            if (_buildings.ResidentialBuildings.Length == 0)
            {
                return;
            }

            var spawnData = data.Spawn[0];
            pendingSpawn = spawnData.Quantity;
            spawnData.Quantity = 0;
            data.Spawn[0] = spawnData;
            for (var i = 0; i < pendingSpawn; i++)
            {
                spawned++;
                var pos = _buildings.GetResidentialBuilding ();
                var matrix = Matrix4x4.TRS (pos, Quaternion.identity, one);
                var entity = Getmanager ().CreateEntity (_agent);
                var agent = new NavAgent
                {
                    EntityIndex = entity.Index,
                    Matrix = matrix,
                    Position = pos,
                    Rotation = Quaternion.identity,
                    WaitTime = 5f
                };
                Getmanager ().SetComponentData (entity, agent);
                Getmanager ().AddSharedComponentData (entity, Getspawner ().Renderers[Random.Range (0, Getspawner ().Renderers.Length)].Value);
                waypointProvider.GetWaypoint (entity, agent);
            }
        }

        private struct InjectData
        {
            public int Length;
            public ComponentDataArray<PendingSpawn> Spawn;
        }

    }
}