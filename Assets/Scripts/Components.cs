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

public struct IsPendingNavMeshQuery : IComponentData { }

public struct IsNotPendingNavMeshQuery : IComponentData { }

public struct WaypointStatus : IComponentData {
    // flags:
    // 0: pending navmesh
    // 1: pending waypoint
    // 2: set heading
    // 3: set distance
    // 4: moving
    // 5: waiting
    public int StateFlag;

    public float WaitTime;
    public float RemainingDistance;
    public float3 NextWaypoint;
    public int NextWaypointIndex;
    public int TotalWaypoints;
}

public struct Waypoint : ISharedComponentData {
    public NativeList<Vector3> Data;
}

public struct PendingSpawn : IComponentData {
    public Entity Entity;
    public int Quantity;
}