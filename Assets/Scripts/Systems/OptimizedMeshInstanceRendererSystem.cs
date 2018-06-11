using System;
using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Systems
{
    [ExecuteInEditMode]
    public class OptimizedMeshInstanceRendererSystem : ComponentSystem
    {
        readonly Matrix4x4[] matrices = new Matrix4x4[1023];

        [Inject] InjectData data;
        NativeArray<JobHandle> handles = new NativeArray<JobHandle> (0, Allocator.Persistent);
        RenderJob[] jobs = new RenderJob[0];
        Matrix4x4[] lastMatrices = new Matrix4x4[1023];
        NativeArray<Matrix4x4>[] arrays = new NativeArray<Matrix4x4>[0];

        [BurstCompile]
        protected override void OnUpdate ()
        {
            // 1.47ms @ 10000
            var renderer = data.Renderers[0];
            var jobLength = Mathf.CeilToInt (data.Length / 1023f);
            var finalLength = jobLength - 1;
            var dataLength = data.Length;
            var remaining = data.Length;

            if (arrays.Length != jobLength)
            {
                DestroyArrays ();
                Array.Resize (ref arrays, jobLength);
                Array.Resize (ref jobs, jobLength);
                handles = new NativeArray<JobHandle> (jobLength, Allocator.Persistent);
                for (var i = 0; i < jobLength; i++)
                {
                    arrays[i] = new NativeArray<Matrix4x4> (1023, Allocator.Persistent);
                }
            }

            for (var i = 0; i < dataLength; i += 1023)
            {
                var currentIndex = i / 1023;
                var length = math.min (1023, remaining);
                var job = new RenderJob
                {
                    Statuses = data.Statuses,
                    Indexes = arrays[currentIndex],
                    Start = i,
                    Length = length
                };
                var handle = job.Schedule ();
                jobs[currentIndex] = job;
                handles[currentIndex] = handle;
                remaining -= length;
            }

            JobHandle.CombineDependencies (handles).Complete ();

            for (var i = 0; i < jobLength; i++)
            {
                arrays[i].CopyTo (matrices);
                Graphics.DrawMeshInstanced (
                    renderer.mesh,
                    renderer.subMesh,
                    renderer.material,
                    matrices,
                    jobs[i].Length,
                    null,
                    ShadowCastingMode.Off,
                    false
                );
            }

            // 1.87 ms @ 10000
            // Vector3 scale = Vector3.one;
            // int beginIndex = 0;
            // var renderer = data.Renderers[0];
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

        protected override void OnDestroyManager ()
        {
            DestroyArrays ();
        }

        void DestroyArrays ()
        {
            handles.Dispose ();
            for (var i = 0; i < arrays.Length; i++)
            {
                arrays[i].Dispose ();
            }
        }

        [BurstCompile]
        struct RenderJob : IJob
        {
            [ReadOnly] public ComponentDataArray<NavAgent> Statuses;
            public int Start;
            public int Length;
            public NativeArray<Matrix4x4> Indexes;

            public void Execute ()
            {
                for (var i = 0; i < Length; i++)
                {
                    Indexes[i] = Statuses[Start + i].Matrix;
                }
            }
        }

        struct InjectData
        {
            public int Length;
            [ReadOnly] public ComponentDataArray<NavAgent> Statuses;
            [ReadOnly] public SharedComponentDataArray<MeshInstanceRenderer> Renderers;
        }
    }
}