using Unity.Entities;
using UnityEngine;
namespace Demo
{
    [System.Serializable]
    public struct PendingSpawn : IComponentData
    {
        public int Quantity;
        public float AgentStoppingDistance;
        public float AgentAccelleration;
        public float AgentMaxMoveSpeed;
        public float AgentRotationSpeed;
        public int AgentAreaMask;
    }

    public class PendingSpawnComponent : ComponentDataWrapper<PendingSpawn> { }

}