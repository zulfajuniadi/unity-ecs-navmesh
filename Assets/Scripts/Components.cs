using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public struct BuildingData : IComponentData {
    public Entity Entity;
    public float3 Position;
    public BuildingType Type;
}

public struct Person : IComponentData {
    public int id;
    public Entity entity;
}

public struct NeedsPathTag : IComponentData { } // => NavMeshSystem
public struct IsPathFindingTag : IComponentData { } // => NavMeshSystem
public struct IsChillingTag : IComponentData { } // => WaypointSystem
public struct IsMovingTag : IComponentData { } // => MovementSystem
public struct NeedsWaypointTag : IComponentData { } // => MovementSystem

public struct WaypointStatus : IComponentData {
    public float WaitTime;
    public float RemainingDistance;
    public float3 NextWaypoint;
    public int NextWaypointIndex;
    public int TotalWaypoints;
    public Matrix4x4 Matrix;
}

public struct Waypoint : ISharedComponentData {
    public NativeList<Vector3> Data;
}

public struct PendingSpawn : IComponentData {
    public Entity Entity;
    public int Quantity;
}