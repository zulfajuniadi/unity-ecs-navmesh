using Unity.Collections;
using Unity.Entities;

namespace NavJob.Components
{
    public struct SyncRotationToNavAgent : IComponentData { }

    public class SyncRotationToNavAgentComponent : ComponentDataWrapper<SyncRotationToNavAgent> { };
}