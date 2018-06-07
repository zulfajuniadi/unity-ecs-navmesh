using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;
using UnityEngine.UI;

public class NeedsMovementTagBarrier : BarrierSystem { }
public class NeedsWaypointTagBarrier : BarrierSystem { }
public class NeedsPathTagBarrier : BarrierSystem { }
public class NeedsToChillTagBarrier : BarrierSystem { }

public class WaypointSystem : JobComponentSystem {
    struct SetNextWaypointJob : IJobParallelFor {

        [ReadOnly] public EntityArray entities;
        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public SharedComponentDataArray<Waypoint> waypoints;

        public ComponentDataArray<WaypointStatus> statuses;
        public ComponentDataArray<Rotation> rotations;
        public NativeQueue<Entity>.Concurrent needsToMove;
        public float dt;

        public void Execute (int i) {
            var status = statuses[i];
            var position = positions[i];
            status.NextWaypoint = waypoints[i].Data[status.NextWaypointIndex];
            status.NextWaypointIndex++;
            var diff = ((Vector3) status.NextWaypoint - (Vector3) position.Value);
            var magnitude = diff.magnitude;
            if (magnitude > 0.00001f) {
                var rotation = rotations[i];
                status.RemainingDistance = magnitude;
                rotation.Value = Quaternion.LookRotation (diff, Vector3.up);
                rotations[i] = rotation;
                needsToMove.Enqueue (entities[i]);
            }
            statuses[i] = status;
        }
    }

    struct NeedsToMoveJob : IJob {
        public NativeQueue<Entity> entities;
        public EntityCommandBuffer buffer;
        public void Execute () {
            while (entities.Count > 0) {
                var entity = entities.Dequeue ();
                buffer.AddComponent<IsMovingTag> (entity, new IsMovingTag ());
                buffer.RemoveComponent<NeedsWaypointTag> (entity);
            }
        }
    }

    struct InjectData {
        public int Length;
        public ComponentDataArray<Rotation> rotations;
        public ComponentDataArray<WaypointStatus> statuses;
        [ReadOnly] public ComponentDataArray<Position> positions;
        [ReadOnly] public EntityArray entities;
        [ReadOnly] public SharedComponentDataArray<Waypoint> waypoints;
        [ReadOnly] public SubtractiveComponent<NeedsPathTag> sub0;
        [ReadOnly] public SubtractiveComponent<IsPathFindingTag> sub1;
        [ReadOnly] public SubtractiveComponent<IsMovingTag> sub2;
        [ReadOnly] public SubtractiveComponent<IsChillingTag> sub3;
    }

    NativeQueue<Entity> needsToMove;

    [Inject] InjectData data;
    [Inject] NeedsWaypointTagBarrier barrier;
    protected override JobHandle OnUpdate (JobHandle deps) {
        deps = new SetNextWaypointJob {
            dt = Time.deltaTime,
                entities = data.entities,
                positions = data.positions,
                rotations = data.rotations,
                waypoints = data.waypoints,
                statuses = data.statuses,
                needsToMove = needsToMove,
        }.Schedule (data.Length, 64, deps);
        var buffer = barrier.CreateCommandBuffer ();
        return new NeedsToMoveJob { entities = needsToMove, buffer = buffer }.Schedule (deps);
    }

    protected override void OnCreateManager (int i) {
        needsToMove = new NativeQueue<Entity> (Allocator.Persistent);
    }

    protected override void OnDestroyManager () {
        needsToMove.Dispose ();
    }
}

public class ChillingSystem : JobComponentSystem {

    struct ChillingJob : IJobProcessComponentData<WaypointStatus, IsChillingTag> {
        public float dt;
        public void Execute (ref WaypointStatus status, ref IsChillingTag tag) {
            status.WaitTime -= dt;
        }
    }

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        return new ChillingJob { dt = Time.deltaTime }.Schedule (this, 64, inputDeps);
    }
}

public class ChillingBarrierSystem : JobComponentSystem {

    public NativeQueue<Entity> needsPath;

    struct DetectNeedsPathJob : IJobParallelFor {
        [ReadOnly] public ComponentDataArray<WaypointStatus> statuses;
        [ReadOnly] public EntityArray entities;
        public NativeQueue<Entity>.Concurrent needsPath;
        public void Execute (int index) {
            if (statuses[index].WaitTime <= 0) {
                needsPath.Enqueue (entities[index]);
            }
        }
    }

    struct NeedsPathTagJob : IJob {
        public EntityCommandBuffer buffer;
        public NativeQueue<Entity> entities;
        public void Execute () {
            while (entities.Count > 0) {
                var entity = entities.Dequeue ();
                buffer.RemoveComponent<IsChillingTag> (entity);
                buffer.AddComponent<NeedsPathTag> (entity, new NeedsPathTag ());
            }
        }
    }

    struct InjectData {
        public int Length;
        [ReadOnly] public EntityArray entities;
        [ReadOnly] public ComponentDataArray<WaypointStatus> statuses;
        public ComponentDataArray<IsChillingTag> tag;
    }

    [Inject] InjectData data;
    [Inject] NeedsPathTagBarrier barrier;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        inputDeps = new DetectNeedsPathJob { entities = data.entities, statuses = data.statuses, needsPath = needsPath }.Schedule (data.Length, 64, inputDeps);
        var buffer = barrier.CreateCommandBuffer ();
        return new NeedsPathTagJob { buffer = buffer, entities = needsPath }.Schedule (inputDeps);
    }

    protected override void OnCreateManager (int i) {
        needsPath = new NativeQueue<Entity> (Allocator.Persistent);
    }

    protected override void OnDestroyManager () {
        needsPath.Dispose ();
    }
}

public class MovmentSystem : JobComponentSystem {

    [ComputeJobOptimization]
    struct MovementJob : IJobParallelFor {
        [ReadOnly] public ComponentDataArray<Rotation> rotations;
        public float speed;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<WaypointStatus> statuses;
        public void Execute (int index) {
            var position = positions[index];
            var rotation = rotations[index];
            var status = statuses[index];
            position.Value = position.Value + math.forward (rotation.Value) * speed;
            status.RemainingDistance -= speed;
            status.Matrix = Matrix4x4.TRS (position.Value, rotation.Value, Vector3.one);
            positions[index] = position;
            statuses[index] = status;
        }
    }

    struct InjectData {
        public int Length;
        public ComponentDataArray<WaypointStatus> statuses;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<IsMovingTag> tag;
        [ReadOnly] public ComponentDataArray<Rotation> rotations;
        [ReadOnly] public SharedComponentDataArray<Waypoint> waypoints;
        [ReadOnly] public SubtractiveComponent<NeedsPathTag> sub0;
        [ReadOnly] public SubtractiveComponent<IsPathFindingTag> sub1;
        [ReadOnly] public SubtractiveComponent<IsChillingTag> sub2;
    }

    [Inject] InjectData data;
    protected override JobHandle OnUpdate (JobHandle handle) {
        return new MovementJob {
            speed = Time.deltaTime * 2,
                positions = data.positions,
                rotations = data.rotations,
                statuses = data.statuses
        }.Schedule (data.Length, 64, handle);
    }
}

public class MovementBarrierSystem : JobComponentSystem {

    [ComputeJobOptimization]
    struct DetectNeedsWaypointJob : IJobParallelFor {
        [ReadOnly] public EntityArray entities;
        [ReadOnly] public ComponentDataArray<WaypointStatus> statuses;
        public NativeQueue<Entity>.Concurrent needsToChill;
        public NativeQueue<Entity>.Concurrent needsWaypoint;
        public void Execute (int index) {
            var status = statuses[index];
            if (status.RemainingDistance <= 0) {
                if (status.NextWaypointIndex < status.TotalWaypoints) {
                    needsWaypoint.Enqueue (entities[index]);
                } else {
                    needsToChill.Enqueue (entities[index]);
                }
            }
        }
    }

    struct MovementTagJob : IJob {
        public NativeQueue<Entity> needsToChill;
        public NativeQueue<Entity> needsWaypoint;
        public EntityCommandBuffer buffer;
        public void Execute () {
            while (needsWaypoint.Count > 0) {
                var entity = needsWaypoint.Dequeue ();
                buffer.RemoveComponent<IsMovingTag> (entity);
                buffer.AddComponent<NeedsWaypointTag> (entity, new NeedsWaypointTag ());
            }
            while (needsToChill.Count > 0) {
                var entity = needsToChill.Dequeue ();
                buffer.RemoveComponent<IsMovingTag> (entity);
                buffer.AddComponent<IsChillingTag> (entity, new IsChillingTag ());
            }
        }
    }

    NativeQueue<Entity> needsToChill;
    NativeQueue<Entity> needsWaypoint;

    struct InjectData {
        public int Length;
        public ComponentDataArray<IsMovingTag> tag;
        [ReadOnly] public ComponentDataArray<WaypointStatus> statuses;
        [ReadOnly] public EntityArray entities;
        [ReadOnly] public SubtractiveComponent<NeedsPathTag> sub0;
        [ReadOnly] public SubtractiveComponent<IsPathFindingTag> sub1;
        [ReadOnly] public SubtractiveComponent<IsChillingTag> sub2;
    }

    [Inject] InjectData data;
    [Inject] NeedsMovementTagBarrier barrier;
    protected override JobHandle OnUpdate (JobHandle deps) {
        deps = new DetectNeedsWaypointJob {
            needsWaypoint = needsWaypoint,
                needsToChill = needsToChill,
                entities = data.entities,
                statuses = data.statuses
        }.Schedule (data.Length, 64, deps);
        var buffer = barrier.CreateCommandBuffer ();
        return new MovementTagJob { needsWaypoint = needsWaypoint, needsToChill = needsToChill, buffer = buffer }.Schedule (deps);
    }

    protected override void OnCreateManager (int i) {
        needsToChill = new NativeQueue<Entity> (Allocator.Persistent);
        needsWaypoint = new NativeQueue<Entity> (Allocator.Persistent);
    }

    protected override void OnDestroyManager () {
        needsToChill.Dispose ();
        needsWaypoint.Dispose ();
    }
}