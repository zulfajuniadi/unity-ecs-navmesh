using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent (typeof (GameObjectEntity))]
public class BuildingData : MonoBehaviour {
    public float Volume;
    public float3 Position;
    public BuildingType Type;
}