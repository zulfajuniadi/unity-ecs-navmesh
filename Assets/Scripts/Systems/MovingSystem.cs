using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;

public class MoveToWaypoint : JobComponentSystem {

    public class MoveToWaypointBarrier : BarrierSystem { }

    struct MoveToWaypointJob : IJobProcessComponentData<Person, CurrentWaypointData, IsMovingToWaypoint> {

        [ReadOnly] float speed;
        [ReadOnly] float waitTime;
        EntityCommandBuffer command;

        public MoveToWaypointJob (float speed, float waitTime, EntityCommandBuffer command) {
            this.waitTime = waitTime;
            this.speed = speed;
            this.command = command;
        }

        [ComputeJobOptimization]
        public void Execute (ref Person person, ref CurrentWaypointData currentWaypoint, ref IsMovingToWaypoint waiting) {
            var id = person.id;
            var entity = person.entity;
            currentWaypoint.RemainingDistance -= speed;
            if (
                currentWaypoint.IsValid == 0 ||
                currentWaypoint.RemainingDistance <= 0
            ) {
                if (currentWaypoint.RemainingWaypoints > 0) {
                    command.AddComponent (entity, new IsPendingNextWaypoint ());
                    command.RemoveComponent<IsMovingToWaypoint> (entity);
                } else {
                    command.SetComponent<MoveSpeed> (entity, new MoveSpeed () { speed = 0 });
                    command.AddComponent<WaitingData> (entity, new WaitingData () { WaitTime = waitTime });
                    command.RemoveComponent<IsMovingToWaypoint> (entity);
                    command.RemoveComponent<CurrentWaypointData> (entity);
                }
            }
        }
    }

    [Inject] private MoveToWaypointBarrier waitingBarrier;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var Commands = waitingBarrier.CreateCommandBuffer ();
        var job = new MoveToWaypointJob (Time.deltaTime * 4, Random.Range (5f, 30f), Commands);
        return job.Schedule (this, inputDeps);
    }
}