using System;
using System.Collections.Generic;
using System.Linq;

namespace Town.Geom
{
    public class Voronoi
    {
        public readonly List<Triangle> Triangles = new List<Triangle>();

        public readonly List<Vector2> Points;
        public readonly List<Vector2> Frame;

        private readonly Dictionary<Vector2, Region> _regions;
        private bool _regionsDirty = true;

        public Voronoi(float minx, float miny, float maxx, float maxy)
        {
            var c1 = new Vector2(minx, miny);
            var c2 = new Vector2(minx, maxy);
            var c3 = new Vector2(maxx, miny);
            var c4 = new Vector2(maxx, maxy);
            Frame = new List<Vector2> { c1, c2, c3, c4 };
            Points = new List<Vector2> { c1, c2, c3, c4 };
            Triangles.Add(new Triangle(c1, c2, c3));
            Triangles.Add(new Triangle(c2, c3, c4));

            // Maybe we shouldn't do it beause these temporary
            // regions will be discarded anyway
            _regions = new Dictionary<Vector2, Region>();
            GetRegions();
        }

        /**
        * Adds a Vector2 to the list and updates the list of triangles
        * @param p a Vector2 to add
        **/
        public void AddPoint(Vector2 point)
        {
            var toSplit = Enumerable.Where(Triangles, tr => (point - tr.C).Length < tr.R).ToList();
                
            if (toSplit.Any())
            {
                Points.Add(point);

                var a = new List<Vector2>();
                var b = new List<Vector2>();

                foreach (var t1 in toSplit)
                {
                    var e1 = true;
                    var e2 = true;
                    var e3 = true;

                    foreach (var t2 in toSplit.Where(t => t != t1))
                    {
                        // If triangles have a common edge, it goes in opposite directions
                        if (e1 && t2.HasEdge(t1.P2, t1.P1))
                        {
                            e1 = false;
                        }
                        if (e2 && t2.HasEdge(t1.P3, t1.P2))
                        {
                            e2 = false;
                        }
                        if (e3 && t2.HasEdge(t1.P1, t1.P3))
                        {
                            e3 = false;
                        }
                        if (!(e1 || e2 || e3))
                        {
                            break;
                        }
                    }

                    if (e1)
                    {
                        a.Add(t1.P1);
                        b.Add(t1.P2);
                    }
                    if (e2)
                    {
                        a.Add(t1.P2);
                        b.Add(t1.P3);
                    }
                    if (e3)
                    {
                        a.Add(t1.P3);
                        b.Add(t1.P1);
                    }
                }

                var index = 0;
                var count = 0;
                do
                {
                    Triangles.Add(new Triangle(point, a[index], b[index]));
                    index = a.IndexOf(b[index]);
                    count++;
                    if (count > 10000)
                    {
                        throw new InvalidOperationException();
                    }
                } while (index != 0);

                foreach (var triangle in toSplit)
                {
                    Triangles.Remove(triangle);
                }

                _regionsDirty = true;
            }
        }

        private Region BuildRegion(Vector2 p)
        {
            var region = new Region(p);
            region.Vertices.AddRange(Enumerable.Where(Triangles, tr => tr.P1 == p || tr.P2 == p || tr.P3 == p));

            region.SortVertices();

            return region;
        }

        public Dictionary<Vector2, Region> GetRegions()
        {
            if (_regionsDirty)
            {
                _regions.Clear();
                foreach (var point in Points)
                {
                    _regions[point] = BuildRegion(point);
                }
                _regionsDirty = false;
            }

            return _regions;
        }

        private bool IsReal(Triangle triangle)
        {
            return !(Frame.Contains(triangle.P1) || Frame.Contains(triangle.P2) || Frame.Contains(triangle.P3));
        }

        /**
        * Returns triangles which do not contain "frame" points as their vertices
        * @return List of triangles
        **/
        public List<Triangle> Triangulation()
        {
            return Enumerable.ToList(Triangles.Where(IsReal));
        }

        public List<Region> Partitioning()
        {
            var regions = GetRegions();
            return Enumerable.Where<Vector2>(Points, p => Enumerable.All(regions[p].Vertices, IsReal)).Select(p => _regions[p]).ToList();
        }

        public List<Region> GetNeighbours(Region region1)
        {
            GetRegions();
            return Enumerable.ToList(_regions.Values.Where(region1.Borders));
        }

        public static Voronoi Relax(Voronoi voronoi, List<Vector2> toRelax = null)
        {
            var regions = voronoi.Partitioning();

            var points = Enumerable.Except<Vector2>(voronoi.Points, voronoi.Frame).ToList();

            toRelax = toRelax ?? voronoi.Points;

            foreach (var region in regions)
            {
                if (toRelax.Contains(region.Seed))
                {
                    points.Remove(region.Seed);
                    points.Add(region.Center());
                }
            }
            return Build(points);
        }

        public static Voronoi Build(List<Vector2> points)
        {
            var minx = points.Min(p => p.x);
            var miny = points.Min(p => p.y);
            var maxx = points.Max(p => p.x);
            var maxy = points.Max(p => p.y);

            var dx = (maxx - minx) * 0.5f;
            var dy = (maxy - miny) * 0.5f;

            var voronoi = new Voronoi(minx - dx / 2, miny - dy / 2, maxx + dx / 2, maxy + dy / 2);
            foreach (var point in points)
            {
                voronoi.AddPoint(point);
            }

            return voronoi;
        }
    }

    public class Triangle
    {

        public readonly Vector2 P1;
        public readonly Vector2 P2;
        public readonly Vector2 P3;

        public readonly Vector2 C;
        public readonly float R;

        public Triangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var s = (p2.x - p1.x) * (p2.y + p1.y) + (p3.x - p2.x) * (p3.y + p2.y) + (p1.x - p3.x) * (p1.y + p3.y);
            P1 = p1;
            // CCW
            P2 = s > 0 ? p2 : p3;
            P3 = s > 0 ? p3 : p2;

            var x1 = (p1.x + p2.x) / 2;
            var y1 = (p1.y + p2.y) / 2;
            var x2 = (p2.x + p3.x) / 2;
            var y2 = (p2.y + p3.y) / 2;

            var dx1 = p1.y - p2.y;
            var dy1 = p2.x - p1.x;
            var dx2 = p2.y - p3.y;
            var dy2 = p3.x - p2.x;

            var tg1 = dy1 / dx1;
            var t2 = ((y1 - y2) - (x1 - x2) * tg1) /
                     (dy2 - dx2 * tg1);

            C = new Vector2(x2 + dx2 * t2, y2 + dy2 * t2);
            R = (C - p1).Length;
        }

        public bool HasEdge(Vector2 a, Vector2 b)
        {
            return
                (P1 == a && P2 == b) ||
                (P2 == a && P3 == b) ||
                (P3 == a && P1 == b);
        }
    }

    public class Region
    {
        public readonly Vector2 Seed;
        public readonly List<Triangle> Vertices;

        public Region(Vector2 seed)
        {
            Seed = seed;
            Vertices = new List<Triangle>();
        }

        public void SortVertices()
        {
            var comparer = new AnglesComparer(Seed);
            Vertices.Sort(comparer);
        }

        public Vector2 Center()
        {
            var c = new Vector2();
            foreach (var v in Vertices)
            {
                c = c + v.C;
            }
            return Vector2.Scale(c, 1f / Vertices.Count);
        }

        public bool Borders(Region r)
        {
            var len1 = Vertices.Count;
            var len2 = r.Vertices.Count;
            for (var i = 0; i < len1; i++)
            {
                var j = r.Vertices.IndexOf(Vertices[i]);
                if (j != -1)
                {
                    return Vertices[(i + 1) % len1] == r.Vertices[(j + len2 - 1) % len2];
                }
            }
            return false;
        }

        private class AnglesComparer : IComparer<Triangle>
        {
            private readonly Vector2 _seed;

            public AnglesComparer(Vector2 seed)
            {
                _seed = seed;
            }

            public int Compare(Triangle v1, Triangle v2)
            {
                var x1 = v1.C.x - _seed.x;
                var y1 = v1.C.y - _seed.y;
                var x2 = v2.C.x - _seed.x;
                var y2 = v2.C.y - _seed.y;

                if (x1 >= 0 && x2 < 0)
                {
                    return 1;
                }

                if (x2 >= 0 && x1 < 0)
                {
                    return -1;
                }

                if (Math.Abs((float) x1) < 0.1f && Math.Abs((float) x2) < 0.1f)
                {
                    return y2 > y1 ? 1 : -1;
                }

                return Math.Sign((float) (x2 * y1 - x1 * y2));
            }
        }
    }
}
