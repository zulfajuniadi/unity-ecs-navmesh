using System;
using System.Collections.Generic;

namespace Town.Geom
{
    public class Edge : IEquatable<Edge>
    {
        public Vector2 A { get; set; }
        public Vector2 B { get; set; }

        public Edge(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }

        public static Edge Reverse(Edge edge)
        {
            return new Edge(edge.B, edge.A);
        }

        public static IEnumerable<Edge> FromPointList(List<Vector2> points)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                yield return new Edge(p1, p2);
            }
        }

        public bool EndsIn(Vector2 point)
        {
            return A.Equals(point) || B.Equals(point);
        }

        public bool Equals(Edge other)
        {
            if (Object.ReferenceEquals(null, other))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return A.Equals(other.A) && B.Equals(other.B) || A.Equals(other.B) && B.Equals(other.A);
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(null, obj))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Edge) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (A.GetHashCode() * 397) ^ B.GetHashCode();
            }
        }
    }

    public class EdgeSameWayComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge x, Edge y)
        {
            if (Object.ReferenceEquals(null, x))
            {
                return false;
            }

            if (Object.ReferenceEquals(null, y))
            {
                return false;
            }

            if (Object.ReferenceEquals(x, y))
            {
                return true;
            }

            return x.A.Equals(y.A) && x.B.Equals(y.B);
        }

        public int GetHashCode(Edge edge)
        {
            return edge.GetHashCode();
        }
    }
}