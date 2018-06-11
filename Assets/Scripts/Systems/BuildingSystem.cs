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
        public NativeList<Vector3> CommercialBuildings = new NativeList<Vector3> (Allocator.Persistent);
        public NativeList<Vector3> ResidentialBuildings = new NativeList<Vector3> (Allocator.Persistent);
        private PopulationSpawner spawner;
        private int nextCommercial = 0;
        private int nextResidential = 0;

        [Inject] private InjectData data;

        private struct InjectData
        {
            public int Length;
            [ReadOnly] public ComponentDataArray<BuildingData> Buildings;
        }

        private PopulationSpawner Spawner
        {
            get
            {
                if (spawner == null)
                {
                    spawner = Object.FindObjectOfType<PopulationSpawner> ();
                }

                return spawner;
            }
        }

        public Vector3 GetResidentialBuilding ()
        {
            nextResidential++;
            if (nextResidential >= ResidentialBuildings.Length)
            {
                nextResidential = 0;
            }

            return ResidentialBuildings[nextResidential];
        }

        public Vector3 GetCommercialBuilding ()
        {
            var building = CommercialBuildings[0];
            try
            {
                if (nextCommercial < CommercialBuildings.Length)
                {
                    building = CommercialBuildings[nextCommercial];
                    nextCommercial++;
                }
                else
                {
                    nextCommercial = 0;
                }
                return building;
            }
            catch
            {
                return building;
            }
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