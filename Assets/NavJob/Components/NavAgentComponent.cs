using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NavJob.Components
{

    public enum AgentStatus
    {
        Idle = 0,
        PathQueued = 1,
        Moving = 2,
        Paused = 4
    }

    [System.Serializable]
    public struct NavAgent : IComponentData
    {
        public float stoppingDistance;
        public float moveSpeed;
        public float acceleration;
        public float rotationSpeed;
        public int areaMask;
        public float avoidanceDiameter;
        public float3 destination { get; set; }
        public float currentMoveSpeed { get; set; }
        public int queryVersion { get; set; }
        public AgentStatus status { get; set; }
        public float3 partition { get; set; }
        public float3 position { get; set; }
        public float3 nextPosition { get; set; }
        public Quaternion rotation { get; set; }
        public float remainingDistance { get; set; }
        public float3 currentWaypoint { get; set; }
        public int nextWaypointIndex { get; set; }
        public int totalWaypoints { get; set; }
    }

    public class NavAgentComponent : ComponentDataWrapper<NavAgent> { }
}