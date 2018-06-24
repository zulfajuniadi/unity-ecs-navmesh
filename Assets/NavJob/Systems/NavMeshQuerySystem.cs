using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace NavJob.Systems
{
    public enum PathfindingFailedReason
    {
        InvalidToOrFromLocation,
        FailedToBegin,
        FailedToResolve,
    }

    public class NavMeshQuerySystem : JobComponentSystem
    {

        /// <summary>
        /// How many navmesh queries are run on each update.
        /// </summary>
        public int MaxQueries = 256;

        /// <summary>
        /// Maximum path size of each query
        /// </summary>
        public int MaxPathSize = 1024;

        /// <summary>
        /// Maximum iteration on each update cycle
        /// </summary>
        public int MaxIterations = 1024;

        /// <summary>
        /// Max map width
        /// </summary>
        public int MaxMapWidth = 10000;

        /// <summary>
        /// Cache query results
        /// </summary>
        public bool UseCache = false;

        /// <summary>
        /// Pending nav mesh count
        /// </summary>
        public int PendingCount
        {
            get
            {
                return QueryQueue.Count;
            }
        }

        /// <summary>
        /// Cached path count
        /// </summary>
        public int CachedCount
        {
            get
            {
                return cachedPaths.Count;
            }
        }

        private NavMeshWorld world;
        private NavMeshQuery locationQuery;
        private ConcurrentQueue<PathQueryData> QueryQueue;
        private NativeList<PathQueryData> ProgressQueue;
        private ConcurrentQueue<int> availableSlots;
        private List<int> takenSlots;
        private List<JobHandle> handles;
        private List<NativeArray<int>> statuses;
        private List<NativeArray<NavMeshLocation>> results;
        private PathQueryData[] queryDatas;
        private NavMeshQuery[] queries;
        private Dictionary<int, UpdateQueryStatusJob> jobs;
        private static NavMeshQuerySystem instance;
        public delegate void SuccessQueryDelegate (int id, Vector3[] corners);
        public delegate void FailedQueryDelegate (int id, PathfindingFailedReason reason);
        private SuccessQueryDelegate pathResolvedCallbacks;
        private FailedQueryDelegate pathFailedCallbacks;
        private ConcurrentDictionary<int, Vector3[]> cachedPaths = new ConcurrentDictionary<int, Vector3[]> ();

        private struct PathQueryData
        {
            public int id;
            public int key;
            public float3 from;
            public float3 to;
            public int areaMask;
        }

        /// <summary>
        /// Register a callback that is called upon successful request
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterPathResolvedCallback (SuccessQueryDelegate callback)
        {
            pathResolvedCallbacks += callback;
        }

        /// <summary>
        /// Register a callback that is called upon failed request
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterPathFailedCallback (FailedQueryDelegate callback)
        {
            pathFailedCallbacks += callback;
        }

        /// <summary>
        /// Request a path. The ID is for you to identify the path
        /// </summary>
        /// <param name="id"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void RequestPath (int id, Vector3 from, Vector3 to, int areaMask = -1)
        {
            var key = GetKey ((int) from.x, (int) from.z, (int) to.x, (int) to.z);
            if (UseCache)
            {
                if (cachedPaths.TryGetValue (key, out Vector3[] waypoints))
                {
                    pathResolvedCallbacks?.Invoke (id, waypoints);
                    return;
                }
            }
            var data = new PathQueryData { id = id, from = from, to = to, areaMask = areaMask, key = key };
            QueryQueue.Enqueue (data);
        }

        /// <summary>
        /// Purge the cached paths
        /// </summary>
        public void PurgeCache ()
        {
            cachedPaths.Clear ();
        }

        /// <summary>
        /// Static counterpart of RegisterPathResolvedCallback.
        /// </summary>
        /// <param name="callback"></param>
        public static void RegisterPathResolvedCallbackStatic (SuccessQueryDelegate callback)
        {
            instance.pathResolvedCallbacks += callback;
        }

        /// <summary>
        /// Static counterpart of RegisterPathFailedCallback
        /// </summary>
        /// <param name="callback"></param>
        public static void RegisterPathFailedCallbackStatic (FailedQueryDelegate callback)
        {
            instance.pathFailedCallbacks += callback;
        }

        /// <summary>
        /// Static counterpart of RequestPath
        /// </summary>
        /// <param name="id"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static void RequestPathStatic (int id, Vector3 from, Vector3 to, int areaMask = -1)
        {
            instance.RequestPath (id, from, to, areaMask);
        }

        /// <summary>
        /// Static counterpart of PurgeCache
        /// </summary>
        public static void PurgeCacheStatic ()
        {
            instance.PurgeCache ();
        }

        private struct UpdateQueryStatusJob : IJob
        {
            public NavMeshQuery query;
            public PathQueryData data;
            public int maxIterations;
            public int maxPathSize;
            public int index;
            public NativeArray<int> statuses;
            public NativeArray<NavMeshLocation> results;

            public void Execute ()
            {
                var status = query.UpdateFindPath (maxIterations, out int performed);

                if (status == PathQueryStatus.InProgress | status == (PathQueryStatus.InProgress | PathQueryStatus.OutOfNodes))
                {
                    statuses[0] = 0;
                    return;
                }

                statuses[0] = 1;

                if (status == PathQueryStatus.Success)
                {
                    var endStatus = query.EndFindPath (out int polySize);
                    if (endStatus == PathQueryStatus.Success)
                    {
                        var polygons = new NativeArray<PolygonId> (polySize, Allocator.Temp);
                        query.GetPathResult (polygons);
                        var straightPathFlags = new NativeArray<StraightPathFlags> (maxPathSize, Allocator.Temp);
                        var vertexSide = new NativeArray<float> (maxPathSize, Allocator.Temp);
                        var cornerCount = 0;
                        var pathStatus = PathUtils.FindStraightPath (
                            query,
                            data.from,
                            data.to,
                            polygons,
                            polySize,
                            ref results,
                            ref straightPathFlags,
                            ref vertexSide,
                            ref cornerCount,
                            maxPathSize
                        );

                        if (pathStatus == PathQueryStatus.Success && cornerCount > 1 && cornerCount <= maxPathSize)
                        {
                            statuses[1] = 1;
                            statuses[2] = cornerCount;
                        }
                        else
                        {
                            Debug.LogWarning (pathStatus);
                            statuses[0] = 1;
                            statuses[1] = 2;
                        }
                        polygons.Dispose ();
                        straightPathFlags.Dispose ();
                        vertexSide.Dispose ();
                    }
                    else
                    {
                        Debug.LogWarning (endStatus);
                        statuses[0] = 1;
                        statuses[1] = 2;
                    }
                }
                else
                {
                    statuses[0] = 1;
                    statuses[1] = 3;
                }
            }
        }

        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {

            if (QueryQueue.Count == 0 && availableSlots.Count == MaxQueries)
            {
                return inputDeps;
            }

            int j = 0;
            while (QueryQueue.Count > 0 && availableSlots.Count > 0)
            {
                if (QueryQueue.TryDequeue (out PathQueryData pending))
                {
                    if (UseCache && cachedPaths.TryGetValue (pending.key, out Vector3[] waypoints))
                    {
                        pathResolvedCallbacks?.Invoke (pending.id, waypoints);
                    }
                    else if (availableSlots.TryDequeue (out int index))
                    {
                        j++;
                        var query = new NavMeshQuery (world, Allocator.Persistent, MaxPathSize);
                        var from = query.MapLocation (pending.from, Vector3.one * 10, 0);
                        var to = query.MapLocation (pending.to, Vector3.one * 10, 0);
                        if (!query.IsValid (from) || !query.IsValid (to))
                        {
                            query.Dispose ();
                            pathFailedCallbacks?.Invoke (pending.id, PathfindingFailedReason.InvalidToOrFromLocation);
                            continue;
                        }
                        var status = query.BeginFindPath (from, to, pending.areaMask);
                        if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
                        {
                            takenSlots.Add (index);
                            queries[index] = query;
                            queryDatas[index] = pending;
                        }
                        else
                        {
                            QueryQueue.Enqueue (pending);
                            availableSlots.Enqueue (index);
                            pathFailedCallbacks?.Invoke (pending.id, PathfindingFailedReason.FailedToBegin);
                            query.Dispose ();
                        }
                    }
                    else
                    {
                        QueryQueue.Enqueue (pending);
                    }
                }
                if (j > MaxQueries)
                {
                    Debug.LogError ("Infinite loop detected");
                    break;
                }
            }

            for (int i = 0; i < takenSlots.Count; i++)
            {
                int index = takenSlots[i];
                var job = new UpdateQueryStatusJob ()
                {
                    maxIterations = MaxIterations,
                    maxPathSize = MaxPathSize,
                    data = queryDatas[index],
                    statuses = statuses[index],
                    query = queries[index],
                    index = index,
                    results = results[index]
                };
                jobs[index] = job;
                handles[index] = job.Schedule (inputDeps);
            }

            for (int i = takenSlots.Count - 1; i > -1; i--)
            {
                int index = takenSlots[i];
                handles[index].Complete ();
                var job = jobs[index];
                if (job.statuses[0] == 1)
                {
                    if (job.statuses[1] == 1)
                    {
                        var waypoints = new Vector3[job.statuses[2]];
                        for (int k = 0; k < job.statuses[2]; k++)
                        {
                            waypoints[k] = job.results[k].position;
                        }
                        if (UseCache)
                        {
                            cachedPaths[job.data.key] = waypoints;
                        }
                        pathResolvedCallbacks?.Invoke (job.data.id, waypoints);
                    }
                    else if (job.statuses[1] == 2)
                    {
                        pathFailedCallbacks?.Invoke (job.data.id, PathfindingFailedReason.FailedToResolve);
                    }
                    else if (job.statuses[1] == 3)
                    {
                        if (MaxPathSize < job.maxPathSize * 2)
                        {
                            MaxPathSize = job.maxPathSize * 2;
                            // Debug.Log ("Setting path to: " + MaxPathSize);
                        }
                        QueryQueue.Enqueue (job.data);
                    }
                    queries[index].Dispose ();
                    availableSlots.Enqueue (index);
                    takenSlots.RemoveAt (i);
                }
            }

            return inputDeps;
        }

        protected override void OnCreateManager (int capacity)
        {
            world = NavMeshWorld.GetDefaultWorld ();
            locationQuery = new NavMeshQuery (world, Allocator.Persistent);
            ProgressQueue = new NativeList<PathQueryData> (MaxQueries, Allocator.Persistent);
            availableSlots = new ConcurrentQueue<int> ();
            handles = new List<JobHandle> (MaxQueries);
            takenSlots = new List<int> (MaxQueries);
            statuses = new List<NativeArray<int>> (MaxQueries);
            results = new List<NativeArray<NavMeshLocation>> (MaxQueries);
            jobs = new Dictionary<int, UpdateQueryStatusJob> (MaxQueries);
            queries = new NavMeshQuery[MaxQueries];
            queryDatas = new PathQueryData[MaxQueries];
            for (int i = 0; i < MaxQueries; i++)
            {
                handles.Add (new JobHandle ());
                statuses.Add (new NativeArray<int> (3, Allocator.Persistent));
                results.Add (new NativeArray<NavMeshLocation> (MaxPathSize, Allocator.Persistent));
                availableSlots.Enqueue (i);
            }
            QueryQueue = new ConcurrentQueue<PathQueryData> ();
            instance = this;
        }

        protected override void OnDestroyManager ()
        {
            ProgressQueue.Dispose ();
            locationQuery.Dispose ();
            for (int i = 0; i < takenSlots.Count; i++)
            {
                queries[takenSlots[i]].Dispose ();
            }
            for (int i = 0; i < MaxQueries; i++)
            {
                statuses[i].Dispose ();
                results[i].Dispose ();
            }
        }

        private int GetKey (int fromX, int fromZ, int toX, int toZ)
        {
            var fromKey = MaxMapWidth * fromX + fromZ;
            var toKey = MaxMapWidth * toX + toZ;
            return MaxMapWidth * fromKey + toKey;
        }
    }

    //
    // Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
    //
    // This software is provided 'as-is', without any express or implied
    // warranty.  In no event will the authors be held liable for any damages
    // arising from the use of this software.
    // Permission is granted to anyone to use this software for any purpose,
    // including commercial applications, and to alter it and redistribute it
    // freely, subject to the following restrictions:
    // 1. The origin of this software must not be misrepresented; you must not
    //    claim that you wrote the original software. If you use this software
    //    in a product, an acknowledgment in the product documentation would be
    //    appreciated but is not required.
    // 2. Altered source versions must be plainly marked as such, and must not be
    //    misrepresented as being the original software.
    // 3. This notice may not be removed or altered from any source distribution.
    //

    // The original source code has been modified by Unity Technologies and Zulfa Juniadi.

    [Flags]
    public enum StraightPathFlags
    {
        Start = 0x01, // The vertex is the start position.
        End = 0x02, // The vertex is the end position.
        OffMeshConnection = 0x04 // The vertex is start of an off-mesh link.
    }

    public class PathUtils
    {
        public static float Perp2D (Vector3 u, Vector3 v)
        {
            return u.z * v.x - u.x * v.z;
        }

        public static void Swap (ref Vector3 a, ref Vector3 b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        // Retrace portals between corners and register if type of polygon changes
        public static int RetracePortals (NavMeshQuery query, int startIndex, int endIndex, NativeSlice<PolygonId> path, int n, Vector3 termPos, ref NativeArray<NavMeshLocation> straightPath, ref NativeArray<StraightPathFlags> straightPathFlags, int maxStraightPath)
        {
            for (var k = startIndex; k < endIndex - 1; ++k)
            {
                var type1 = query.GetPolygonType (path[k]);
                var type2 = query.GetPolygonType (path[k + 1]);
                if (type1 != type2)
                {
                    Vector3 l, r;
                    var status = query.GetPortalPoints (path[k], path[k + 1], out l, out r);
                    float3 cpa1, cpa2;
                    GeometryUtils.SegmentSegmentCPA (out cpa1, out cpa2, l, r, straightPath[n - 1].position, termPos);
                    straightPath[n] = query.CreateLocation (cpa1, path[k + 1]);

                    straightPathFlags[n] = (type2 == NavMeshPolyTypes.OffMeshConnection) ? StraightPathFlags.OffMeshConnection : 0;
                    if (++n == maxStraightPath)
                    {
                        return maxStraightPath;
                    }
                }
            }
            straightPath[n] = query.CreateLocation (termPos, path[endIndex]);
            straightPathFlags[n] = query.GetPolygonType (path[endIndex]) == NavMeshPolyTypes.OffMeshConnection ? StraightPathFlags.OffMeshConnection : 0;
            return ++n;
        }

        public static PathQueryStatus FindStraightPath (NavMeshQuery query, Vector3 startPos, Vector3 endPos, NativeSlice<PolygonId> path, int pathSize, ref NativeArray<NavMeshLocation> straightPath, ref NativeArray<StraightPathFlags> straightPathFlags, ref NativeArray<float> vertexSide, ref int straightPathCount, int maxStraightPath)
        {
            if (!query.IsValid (path[0]))
            {
                straightPath[0] = new NavMeshLocation (); // empty terminator
                return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
            }

            straightPath[0] = query.CreateLocation (startPos, path[0]);

            straightPathFlags[0] = StraightPathFlags.Start;

            var apexIndex = 0;
            var n = 1;

            if (pathSize > 1)
            {
                var startPolyWorldToLocal = query.PolygonWorldToLocalMatrix (path[0]);

                var apex = startPolyWorldToLocal.MultiplyPoint (startPos);
                var left = new Vector3 (0, 0, 0); // Vector3.zero accesses a static readonly which does not work in burst yet
                var right = new Vector3 (0, 0, 0);
                var leftIndex = -1;
                var rightIndex = -1;

                for (var i = 1; i <= pathSize; ++i)
                {
                    var polyWorldToLocal = query.PolygonWorldToLocalMatrix (path[apexIndex]);

                    Vector3 vl, vr;
                    if (i == pathSize)
                    {
                        vl = vr = polyWorldToLocal.MultiplyPoint (endPos);
                    }
                    else
                    {
                        var success = query.GetPortalPoints (path[i - 1], path[i], out vl, out vr);
                        if (!success)
                        {
                            return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
                        }

                        vl = polyWorldToLocal.MultiplyPoint (vl);
                        vr = polyWorldToLocal.MultiplyPoint (vr);
                    }

                    vl = vl - apex;
                    vr = vr - apex;

                    // Ensure left/right ordering
                    if (Perp2D (vl, vr) < 0)
                        Swap (ref vl, ref vr);

                    // Terminate funnel by turning
                    if (Perp2D (left, vr) < 0)
                    {
                        var polyLocalToWorld = query.PolygonLocalToWorldMatrix (path[apexIndex]);
                        var termPos = polyLocalToWorld.MultiplyPoint (apex + left);

                        n = RetracePortals (query, apexIndex, leftIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
                        if (vertexSide.Length > 0)
                        {
                            vertexSide[n - 1] = -1;
                        }

                        //Debug.Log("LEFT");

                        if (n == maxStraightPath)
                        {
                            straightPathCount = n;
                            return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                        }

                        apex = polyWorldToLocal.MultiplyPoint (termPos);
                        left.Set (0, 0, 0);
                        right.Set (0, 0, 0);
                        i = apexIndex = leftIndex;
                        continue;
                    }
                    if (Perp2D (right, vl) > 0)
                    {
                        var polyLocalToWorld = query.PolygonLocalToWorldMatrix (path[apexIndex]);
                        var termPos = polyLocalToWorld.MultiplyPoint (apex + right);

                        n = RetracePortals (query, apexIndex, rightIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
                        if (vertexSide.Length > 0)
                        {
                            vertexSide[n - 1] = 1;
                        }

                        //Debug.Log("RIGHT");

                        if (n == maxStraightPath)
                        {
                            straightPathCount = n;
                            return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
                        }

                        apex = polyWorldToLocal.MultiplyPoint (termPos);
                        left.Set (0, 0, 0);
                        right.Set (0, 0, 0);
                        i = apexIndex = rightIndex;
                        continue;
                    }

                    // Narrow funnel
                    if (Perp2D (left, vl) >= 0)
                    {
                        left = vl;
                        leftIndex = i;
                    }
                    if (Perp2D (right, vr) <= 0)
                    {
                        right = vr;
                        rightIndex = i;
                    }
                }
            }

            // Remove the the next to last if duplicate point - e.g. start and end positions are the same
            // (in which case we have get a single point)
            if (n > 0 && (straightPath[n - 1].position == endPos))
                n--;

            n = RetracePortals (query, apexIndex, pathSize - 1, path, n, endPos, ref straightPath, ref straightPathFlags, maxStraightPath);
            if (vertexSide.Length > 0)
            {
                vertexSide[n - 1] = 0;
            }

            if (n == maxStraightPath)
            {
                straightPathCount = n;
                return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
            }

            // Fix flag for final path point
            straightPathFlags[n - 1] = StraightPathFlags.End;

            straightPathCount = n;
            return PathQueryStatus.Success;
        }
    }

    public class GeometryUtils
    {
        // Calculate the closest point of approach for line-segment vs line-segment.
        public static bool SegmentSegmentCPA (out float3 c0, out float3 c1, float3 p0, float3 p1, float3 q0, float3 q1)
        {
            var u = p1 - p0;
            var v = q1 - q0;
            var w0 = p0 - q0;

            float a = math.dot (u, u);
            float b = math.dot (u, v);
            float c = math.dot (v, v);
            float d = math.dot (u, w0);
            float e = math.dot (v, w0);

            float den = (a * c - b * b);
            float sc, tc;

            if (den == 0)
            {
                sc = 0;
                tc = d / b;

                // todo: handle b = 0 (=> a and/or c is 0)
            }
            else
            {
                sc = (b * e - c * d) / (a * c - b * b);
                tc = (a * e - b * d) / (a * c - b * b);
            }

            c0 = math.lerp (p0, p1, sc);
            c1 = math.lerp (q0, q1, tc);

            return den != 0;
        }
    }
}