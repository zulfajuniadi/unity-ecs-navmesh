using Unity.Entities;
using Unity.Mathematics;
using Demo.Behaviours;

namespace Demo
{
    public struct BuildingData : IComponentData
    {
        public Entity Entity;
        public float3 Position;
        public BuildingType Type;
    }
}