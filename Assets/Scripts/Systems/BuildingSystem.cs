#region

using Behaviours;
using Components;
using Modifiers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

#endregion

namespace Systems
{
    public class BuildingCacheSystem : ComponentSystem
    {
        private PopulationSpawner _spawner;
        public NativeList<Vector3> CommercialBuildings = new NativeList<Vector3> (Allocator.Persistent);
        public NativeList<Vector3> ResidentialBuildings = new NativeList<Vector3> (Allocator.Persistent);
        private int _nextCommercial = -1;
        private int _nextResidential = -1;
        [Inject] private InjectBuildings data;

        private struct InjectBuildings
        {
            public int Length;
            [ReadOnly] public ComponentDataArray<BuildingData> Buildings;
        }

        private PopulationSpawner Spawner
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

        public Vector3 GetResidentialBuilding ()
        {
            _nextResidential++;
            if (_nextResidential >= ResidentialBuildings.Length)
            {
                _nextResidential = 0;
            }

            return ResidentialBuildings[_nextResidential];
        }

        public Vector3 GetCommercialBuilding ()
        {
            _nextCommercial++;
            if (_nextCommercial >= CommercialBuildings.Length)
            {
                _nextCommercial = 0;
            }

            return CommercialBuildings[_nextCommercial];
        }

        protected override void OnUpdate ()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var building = data.Buildings[i];
                if (building.Type == BuildingType.Residential)
                {
                    ResidentialBuildings.Add (building.Position);
                }
                else
                {
                    CommercialBuildings.Add (building.Position);
                }

                PostUpdateCommands.RemoveComponent<BuildingData> (building.Entity);
            }
        }

        protected override void OnDestroyManager ()
        {
            ResidentialBuildings.Dispose ();
            CommercialBuildings.Dispose ();
        }
    }
}