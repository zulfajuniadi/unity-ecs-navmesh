using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

// states:
// 0: no path
// 1: calculating path
// 2: moving
// 3: waiting

// public struct MovementState : IComponentData {
//     public int State;
//     public MovementState (int state = 0) {
//         State = state;
//     }
// }

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

// public struct WayPointData : ISharedComponentData {
//     public NativeQueue<Vector3> Paths;
// }