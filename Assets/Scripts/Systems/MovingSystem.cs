using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;
using UnityEngine.UI;

public class SetNextWaypoint : JobComponentSystem {
    struct SetNextWaypointJob : IJobParallelFor {
        public InjectData data;
        [ComputeJobOptimization]
        public void Execute (int i) {
            var status = data.status[i];
            if (status.StateFlag != 1) return;
            status.NextWaypoint = data.waypoint[i].Data[status.NextWaypointIndex];
            status.StateFlag = 2;
            data.status[i] = status;
        }
    }

    struct InjectData {
        public int Length;
        public ComponentDataArray<WaypointStatus> status;
        [ReadOnly] public SharedComponentDataArray<Waypoint> waypoint;
    }

    [Inject] InjectData data;
    [ComputeJobOptimization]
    protected override JobHandle OnUpdate (JobHandle input) {
        return new SetNextWaypointJob {
            data = data
        }.Schedule (data.Length, 16, input);
    }
}

public class MovingSystem : JobComponentSystem {

    [ComputeJobOptimization]
    public struct MovingJob : IJobParallelFor {
        public InjectData data;
        public float waitTime;
        public float dt;

        public void Execute (int index) {
            if (data.status[index].StateFlag < 1) {
                return;
            }
            var st = data.status[index];
            var spd = data.speed[index];
            var pos = data.position[index];
            var hdg = data.heading[index];

            if (st.StateFlag == 2) {
                var diff = ((Vector3) st.NextWaypoint - (Vector3) pos.Value);
                hdg.Value = diff;
                st.RemainingDistance = diff.magnitude;
                st.StateFlag = 3;
            } else if (st.StateFlag == 3) {
                spd.speed = 4;
                st.RemainingDistance = st.RemainingDistance - dt * 4;
                if (st.RemainingDistance <= 0) {
                    st.RemainingDistance = 0;
                    st.NextWaypointIndex++;
                    if (st.NextWaypointIndex == st.TotalWaypoints) {
                        st.StateFlag = 4;
                        st.WaitTime = waitTime;
                    } else {
                        st.StateFlag = 1;
                    }
                }
            } else if (st.StateFlag == 4) {
                spd.speed = 0;
                st.WaitTime = st.WaitTime - dt;
                if (st.WaitTime <= 0) {
                    st.WaitTime = 0;
                    st.StateFlag = 0;
                }
            }
            data.status[index] = st;
            data.speed[index] = spd;
            data.heading[index] = hdg;
            data.position[index] = pos;
        }
    }

    protected override void OnCreateManager (int i) {
        base.OnCreateManager (i);
    }

    protected override void OnDestroyManager () {
        base.OnDestroyManager ();
    }

    public struct InjectData {
        public int Length;
        public ComponentDataArray<WaypointStatus> status;
        public ComponentDataArray<Position> position;
        public ComponentDataArray<MoveSpeed> speed;
        public ComponentDataArray<Heading> heading;
    }

    [Inject] InjectData data;
    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var dt = Time.deltaTime;
        var waitTime = Random.Range (5f, 10f);
        var job = new MovingJob { dt = dt, waitTime = waitTime, data = data };
        return job.Schedule (data.Length, 16, inputDeps);
    }
}