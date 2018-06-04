using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public struct Person : IComponentData {
    public int id;
    public Entity entity;
}

public struct IsPendingNextWaypoint : IComponentData { }

public struct IsMovingToWaypoint : IComponentData { }

public struct IsPendingWaypoint : IComponentData { }

public struct IsPendingNavMeshQuery : IComponentData { }

public struct WaitingData : IComponentData {
    public float WaitTime;
    public WaitingData (float waitTime) {
        WaitTime = waitTime;
    }
}

public struct CurrentWaypointData : IComponentData {
    public int IsValid;
    public float RemainingDistance;
    public int RemainingWaypoints;
}