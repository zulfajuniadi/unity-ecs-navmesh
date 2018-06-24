using Unity.Entities;

public struct NavAgentPartitioning : ISharedComponentData
{
    int partition;
}

public class NavAgentPartitioningComponent : SharedComponentDataWrapper<NavAgentPartitioning> { }