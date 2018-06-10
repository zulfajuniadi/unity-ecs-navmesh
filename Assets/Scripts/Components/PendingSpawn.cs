using Unity.Entities;

namespace Components
{
    public struct PendingSpawn : IComponentData
    {
        public Entity Entity;
        public int Quantity;
    }
}