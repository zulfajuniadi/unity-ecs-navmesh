using Modifiers;
using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct BuildingData : IComponentData
    {
        public Entity Entity;
        public float3 Position;
        public BuildingType Type;
    }
}