using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.PlayerLoop;

namespace Unity.Rendering {
    [UnityEngine.ExecuteInEditMode]
    public class OptimizedMeshInstanceRendererSystem : ComponentSystem {
        Matrix4x4[] matrices = new Matrix4x4[1023];
        Matrix4x4[] lastMatrices = new Matrix4x4[1023];

        [ComputeJobOptimization]
        struct RenderJob : IJob {
            [ReadOnly] public ComponentDataArray<WaypointStatus> statuses;
            public int start;
            public int length;
            public NativeArray<Matrix4x4> indexes;
            public void Execute () {
                for (int i = 0; i < length; i++) {
                    indexes[i] = statuses[start + i].Matrix;
                }
            }
        }

        struct InjectData {
            public int Length;
            [ReadOnly] public ComponentDataArray<WaypointStatus> statuses;
            [ReadOnly] public SharedComponentDataArray<MeshInstanceRenderer> renderers;
        }

        [Inject] InjectData data;

        // draw mesh instanced
        [ComputeJobOptimization]
        protected override void OnUpdate () {
            // 1.47ms @ 10000
            var renderer = data.renderers[0];
            var jobLength = Mathf.CeilToInt (data.Length / 1023f);
            var finalLength = jobLength - 1;
            var dataLength = data.Length;
            NativeArray<Matrix4x4>[] arrays = new NativeArray<Matrix4x4>[Mathf.CeilToInt (jobLength)];
            RenderJob[] jobs = new RenderJob[Mathf.CeilToInt (jobLength)];
            JobHandle[] handles = new JobHandle[Mathf.CeilToInt (jobLength)];
            var length = 0;
            var remaining = data.Length;
            for (int i = 0; i < data.Length; i += 1023) {
                int currentIndex = i / 1023;
                length = math.min (1023, remaining);
                var indexes = new NativeArray<Matrix4x4> (length, Allocator.Temp);
                var job = new RenderJob { statuses = data.statuses, indexes = indexes, start = i, length = length };
                var handle = job.Schedule ();
                arrays[currentIndex] = indexes;
                jobs[currentIndex] = job;
                handles[currentIndex] = handle;
                remaining -= length;
                if (remaining == 0 && lastMatrices.Length != length) {
                    System.Array.Resize (ref lastMatrices, length);
                }
            }
            for (int i = 0; i < jobLength; i++) {
                handles[i].Complete ();
            }
            for (int i = 0; i < jobLength; i++) {
                if (i < finalLength) {
                    arrays[i].CopyTo (matrices);
                    Graphics.DrawMeshInstanced (renderer.mesh, renderer.subMesh, renderer.material, matrices, jobs[i].length, null, renderer.castShadows, renderer.receiveShadows);
                } else {
                    arrays[i].CopyTo (lastMatrices);
                    Graphics.DrawMeshInstanced (renderer.mesh, renderer.subMesh, renderer.material, lastMatrices, jobs[i].length, null, renderer.castShadows, renderer.receiveShadows);
                }
                arrays[i].Dispose ();
            }

            // 1.87 ms @ 10000
            // Vector3 scale = Vector3.one;
            // int beginIndex = 0;
            // var renderer = data.renderers[0];
            // var length = math.min (data.Length, 1023);
            // int remaining = data.Length;
            // for (int i = 0; i < length; i++) {
            //     matrices[i] = data.matrices[i + beginIndex].Value;
            //     if (i + 1 == length) {
            //         Graphics.DrawMeshInstanced (renderer.mesh, renderer.subMesh, renderer.material, matrices, length, null, renderer.castShadows, renderer.receiveShadows);
            //         remaining -= length;
            //         beginIndex += length - 1;
            //         if (remaining > 0) {
            //             i = 0;
            //             length = math.min (remaining, 1023);
            //         }
            //     }
            // }
        }
    }
}