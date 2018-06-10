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
        [Inject] private NavSystem navSystem;

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

        private PopulationSpawner spawner
        {
            get
            {
                if (_spawner == null)
                {
                    _spawner = Object.FindObjectOfType<PopulationSpawner> ();
                }

                return _spawner;
            }
        }

        private EntityManager manager
        {
            get
            {
                if (_manager == null)
                {
                    _manager = World.Active.GetOrCreateManager<EntityManager> ();
                }

                return _manager;
            }
        }

        protected override void OnCreateManager (int i)
        {
            base.OnCreateManager (i);
            _agent = manager.CreateArchetype (
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

            if (spawner.Renderers.Length == 0)
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
                var entity = manager.CreateEntity (this._agent);
                var agent = new NavAgent
                {
                    EntityIndex = entity.Index,
                    Matrix = matrix,
                    Position = pos,
                    Rotation = Quaternion.identity,
                    WaitTime = 60f
                };
                manager.SetComponentData (entity, agent);
                manager.AddSharedComponentData (entity, spawner.Renderers[Random.Range (0, spawner.Renderers.Length)].Value);
                navSystem.GetWaypoint (entity, agent);
            }
        }

        private struct InjectData
        {
            public int Length;
            public ComponentDataArray<PendingSpawn> Spawn;
        }

    }
}