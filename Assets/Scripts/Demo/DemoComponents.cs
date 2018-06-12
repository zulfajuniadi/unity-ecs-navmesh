using Demo.Modifiers;
using Unity.Entities;
using Unity.Mathematics;

namespace Demo
{
    public struct BuildingData : IComponentData
    {
        public Entity Entity;
        public float3 Position;
        public BuildingType Type;
    }

    public struct PendingSpawn : IComponentData
    {
        public Entity Entity;
        public int Quantity;
        public float AgentStoppingDistance;
        public float AgentAccelleration;
        public float AgentMaxMoveSpeed;
        public float AgentRotationSpeed;
    }
}