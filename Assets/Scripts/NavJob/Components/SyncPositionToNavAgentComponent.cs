using Unity.Collections;
using Unity.Entities;

namespace NavJob.Components
{
    public struct SyncPositionToNavAgent : IComponentData { }

    public class SyncPositionToNavAgentComponent : ComponentDataWrapper<SyncPositionToNavAgent> { };
}