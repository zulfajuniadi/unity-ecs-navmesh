using System;

namespace Town.Geom
{
    public struct Vector2 : IEquatable<Vector2>
    {

        public float x,
        y;

        public static readonly Vector2 Zero = new Vector2 (0, 0);
        public static readonly Vector2 One = new Vector2 (1, 1);

        public static readonly Vector2 Right = new Vector2 (1, 0);
        public static readonly Vector2 Left = new Vector2 (-1, 0);

        public static readonly Vector2 Up = new Vector2 (0, 1);
        public static readonly Vector2 Down = new Vector2 (0, -1);

        public static readonly Vector2 Invalid = new Vector2 (double.MaxValue, double.MaxValue);

        public Vector2 (float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2 (double x, double y)
        {
            this.x = (float) x;
            this.y = (float) y;
        }

        public float Length => (float) Math.Sqrt (x * x + y * y);

        public bool IsValid => x < double.MaxValue - 1 && y < double.MaxValue - 1;

        public Vector2 Normalize ()
        {
            var m = Length;
            return new Vector2 (x / m, y / m);
        }

        public static Vector2 Normalize (Vector2 a)
        {
            float magnitude = a.Length;
            return new Vector2 (a.x / magnitude, a.y / magnitude);
        }

        public static Vector2 Scale (Vector2 a, float factor)
        {
            return new Vector2 (a.x * factor, a.y * factor);
        }

        public override bool Equals (object other)
        {
            if (ReferenceEquals (null, other)) return false;
            return other is Vector2 && Equals ((Vector2) other);
        }

        public override string ToString ()
        {
            return string.Format (x + "," + y);
        }

        public override int GetHashCode ()
        {
            unchecked
            {
                return (x.GetHashCode () * 397) ^ y.GetHashCode ();
            }
        }

        public float DistanceSquare (Vector2 v)
        {
            return Vector2.DistanceSquare (this, v);
        }

        public static float DistanceSquare (Vector2 a, Vector2 b)
        {
            float cx = b.x - a.x;
            float cy = b.y - a.y;
            return cx * cx + cy * cy;
        }

        public float Angle ()
        {
            return AngleThreePoints (this, Zero, Right);
        }

        public float AngleComparedTo (Vector2 other)
        {
            return AngleThreePoints (this, Zero, other);
        }

        public static float AngleThreePoints (Vector2 a, Vector2 b, Vector2 c)
        {
            var v1 = b - a;
            var v2 = c - b;

            var cross = Cross (v1, v2);
            var dot = Dot (v1, v2);

            return (float) Math.Atan2 (cross, dot);
        }

        public Vector2 Rotate90 ()
        {
            return Rotate90 (this);
        }

        public static Vector2 Rotate90 (Vector2 a)
        {
            return new Vector2 (-a.y, a.x);
        }

        public Vector2 RotateAoundPoint (Vector2 center, float angle)
        {
            return RotateAroundPoint (this, center, angle);
        }

        public static Vector2 RotateAroundPoint (Vector2 point, Vector2 center, float angle)
        {
            var cosTheta = Math.Cos (angle);
            var sinTheta = Math.Sin (angle);
            var newX = (cosTheta * (point.x - center.x) - sinTheta * (point.y - center.y) + center.x);
            var newY = (sinTheta * (point.x - center.x) + cosTheta * (point.y - center.y) + center.y);
            return new Vector2 (newX, newY);
        }

        public static bool operator == (Vector2 a, Vector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator != (Vector2 a, Vector2 b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public static Vector2 operator - (Vector2 a, Vector2 b)
        {
            return new Vector2 (a.x - b.x, a.y - b.y);
        }

        public static Vector2 operator + (Vector2 a, Vector2 b)
        {
            return new Vector2 (a.x + b.x, a.y + b.y);
        }

        public static Vector2 operator * (Vector2 a, int i)
        {
            return new Vector2 (a.x * i, a.y * i);
        }

        public static Vector2 Min (Vector2 a, Vector2 b)
        {
            return new Vector2 (Math.Min (a.x, b.x), Math.Min (a.y, b.y));
        }

        public static Vector2 Max (Vector2 a, Vector2 b)
        {
            return new Vector2 (Math.Max (a.x, b.x), Math.Max (a.y, b.y));
        }

        public bool Equals (Vector2 other, float delta)
        {
            return Math.Abs (x - other.x) < delta && Math.Abs (y - other.y) < delta;
        }

        public bool Equals (Vector2 other)
        {
            return Equals (other, 0.1f);
        }

        public static float Dot (Vector2 a, Vector2 b)
        {
            return a.x * b.x + a.y * b.y;
        }

        public static float Cross (Vector2 a, Vector2 b)
        {
            return GeometryHelpers.CrossProduct (a.x, a.y, b.x, b.y);
        }

        public static Vector2 SmoothVertex (Vector2 vertex, Vector2 prev, Vector2 next, float amount = 1f)
        {
            return new Vector2 ((prev.x + vertex.x * amount + next.x) / (2 + amount),
                (prev.y + vertex.y * amount + next.y) / (2 + amount));
        }
    }
}