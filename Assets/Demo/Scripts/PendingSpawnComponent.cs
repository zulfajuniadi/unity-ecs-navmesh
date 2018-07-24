using UnityEngine;
using Unity.Entities;
namespace Demo
{
    [System.Serializable]
    public struct PendingSpawn : IComponentData
    {
        public int Quantity;
        public float AgentStoppingDistance;
        public float AgentAcceleration;
        public float AgentMoveSpeed;
        public float AgentRotationSpeed;
        public float AgentAvoidanceDiameter;
        public int AgentAreaMask;
    }

    public class PendingSpawnComponent : ComponentDataWrapper<PendingSpawn> { }

}