using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NavJob.Components
{

    public enum AgentStatus
    {
        Idle = 0,
        PathQueued = 1,
        Moving = 2
    }

    [System.Serializable]
    public struct NavAgent : IComponentData
    {
        public float3 destination;
        public float stoppingDistance;
        public float maxMoveSpeed;
        public float currentMoveSpeed;
        public float accelleration;
        public float rotationSpeed;
        public AgentStatus status;
        public float3 position { get; set; }
        public Quaternion rotation { get; set; }
        public float remainingDistance { get; set; }
        public float3 currentWaypoint { get; set; }
        public int nextWaypointIndex { get; set; }
        public int totalWaypoints { get; set; }
    }

    public class NavAgentComponent : ComponentDataWrapper<NavAgent> { }
}