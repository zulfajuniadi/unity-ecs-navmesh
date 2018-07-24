using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace NavJob.Components
{
    [System.Serializable]
    public struct NavAgentAvoidance : IComponentData
    {
        public float radius;
        public float3 partition { get; set; }

        public NavAgentAvoidance (
            float radius = 1f
        )
        {
            this.radius = radius;
            this.partition = new float3 (0);
        }
    }

    public class NavAgentAvoidanceComponent : ComponentDataWrapper<NavAgentAvoidance> { }
}