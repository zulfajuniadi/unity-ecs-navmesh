using Unity.Collections;
using Unity.Entities;

namespace NavJob.Components
{
    public struct SyncPositionFromNavAgent : IComponentData { }

    public class SyncPositionFromNavAgentComponent : ComponentDataWrapper<SyncPositionFromNavAgent> { };
}