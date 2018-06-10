using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Components
{
    public struct NavAgent : IComponentData
    {
        public int EntityIndex;
        public float3 Position;
        public quaternion Rotation;
        public Matrix4x4 Matrix;
        public float WaitTime;
        public float RemainingDistance;
        public float3 NextWaypoint;
        public int NextWaypointIndex;
        public int TotalWaypoints;
    }
}