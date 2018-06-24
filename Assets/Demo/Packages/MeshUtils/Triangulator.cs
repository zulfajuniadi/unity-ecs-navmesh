using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils
{
    public class Triangulator
    {
        private List<Vector3> m_points = new List<Vector3> ();

        public Triangulator (List<Vector3> points)
        {
            m_points = points;
        }

        public int[] Triangulate ()
        {
            List<int> indices = new List<int> ();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray ();

            int[] V = new int[n];
            if (Area () > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                    return indices.ToArray ();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip (u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add (a);
                    indices.Add (b);
                    indices.Add (c);
                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse ();
            return indices.ToArray ();
        }

        private float Area ()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector3 pval = m_points[p];
                Vector3 qval = m_points[q];
                A += pval.x * qval.z - qval.x * pval.z;
            }
            return (A * 0.5f);
        }

        private bool Snip (int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector3 A = m_points[V[u]];
            Vector3 B = m_points[V[v]];
            Vector3 C = m_points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.z - A.z)) - ((B.z - A.z) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector3 P = m_points[V[p]];
                if (InsideTriangle (A, B, C, P))
                    return false;
            }
            return true;
        }

        private bool InsideTriangle (Vector3 A, Vector3 B, Vector3 C, Vector3 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x;
            ay = C.z - B.z;
            bx = A.x - C.x;
            by = A.z - C.z;
            cx = B.x - A.x;
            cy = B.z - A.z;
            apx = P.x - A.x;
            apy = P.z - A.z;
            bpx = P.x - B.x;
            bpy = P.z - B.z;
            cpx = P.x - C.x;
            cpy = P.z - C.z;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
}