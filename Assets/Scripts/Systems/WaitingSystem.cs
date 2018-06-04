using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;

public class WaitingSystem : JobComponentSystem {

    public class WaitingBarrier : BarrierSystem { }

    struct WaitJob : IJobProcessComponentData<Person, WaitingData> {

        [ReadOnly] float dt;
        EntityCommandBuffer command;

        public WaitJob (float dt, EntityCommandBuffer command) {
            this.dt = dt;
            this.command = command;
        }

        [ComputeJobOptimization]
        public void Execute (ref Person person, ref WaitingData waiting) {
            var waitTime = waiting.WaitTime - dt;
            if (waitTime < 0) {
                command.AddComponent (person.entity, new IsPendingNavMeshQuery ());
                command.RemoveComponent<WaitingData> (person.entity);
            } else {
                waiting.WaitTime = waitTime;
            }
        }
    }

    [Inject] private WaitingBarrier waitingBarrier;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var Commands = waitingBarrier.CreateCommandBuffer ();
        var job = new WaitJob (Time.deltaTime, Commands);
        return job.Schedule (this, inputDeps);
    }
}