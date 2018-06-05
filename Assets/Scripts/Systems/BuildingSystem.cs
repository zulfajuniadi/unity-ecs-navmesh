using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingCacheSystem : ComponentSystem {

    PopulationSpawner _spawner;
    PopulationSpawner spawner {
        get {
            if (_spawner == null) {
                _spawner = GameObject.FindObjectOfType<PopulationSpawner> ();
            }
            return _spawner;
        }
    }

    public NativeQueue<Vector3> ResidentialBuildings = new NativeQueue<Vector3> (Allocator.Persistent);
    public NativeQueue<Vector3> CommercialBuildings = new NativeQueue<Vector3> (Allocator.Persistent);
    int residentialBuildingsCount;
    int commercialBuildingsCount;

    public Vector3 GetResidentialBuilding () {
        var pos = ResidentialBuildings.Dequeue ();
        ResidentialBuildings.Enqueue (pos);
        return pos;
    }

    public Vector3 GetCommercialBuilding () {
        var pos = CommercialBuildings.Dequeue ();
        CommercialBuildings.Enqueue (pos);
        return pos;
    }

    struct InjectBuildings {
        public int Length;
        [ReadOnly] public ComponentDataArray<BuildingData> Buildings;
    }

    [Inject] InjectBuildings data;

    protected override void OnUpdate () {
        for (int i = 0; i < data.Length; i++) {
            var building = data.Buildings[i];
            if (building.Type == BuildingType.Residential) {
                ResidentialBuildings.Enqueue (building.Position);
                residentialBuildingsCount++;
            } else {
                CommercialBuildings.Enqueue (building.Position);
                commercialBuildingsCount++;
            }
            PostUpdateCommands.RemoveComponent<BuildingData> (building.Entity);
        }
    }

    protected override void OnDestroyManager () {
        ResidentialBuildings.Dispose ();
        CommercialBuildings.Dispose ();
    }

}