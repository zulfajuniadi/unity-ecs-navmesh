using System;

namespace Town.Geom
{
    public static class GeometryHelpers
    {
        public static Vector2 IntersectLines(float x1, float y1, float dx1, float dy1, float x2, float y2, float dx2,
            float dy2)
        {
            var d = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(d) < 0.01)
            {
                return Vector2.Invalid;
            }

            var t2 = (dy1 * (x2 - x1) - dx1 * (y2 - y1)) / d;
            var t1 = Math.Abs(dx1) > 0.0001 ? (x2 - x1 + dx2 * t2) / dx1 : (y2 - y1 + dy2 * t2) / dy1;

            return new Vector2(t1, t2);
        }

        public static Vector2 Interpolate(Vector2 p1, Vector2 p2, float ratio = 0.5f)
        {
            var d = p2 - p1;
            return new Vector2(p1.x + d.x * ratio, p1.y + d.y * ratio);
        }

        //public static inline function scalar(x1:Float, y1:Float, x2:Float, y2:Float )
        //return x1* x2 + y1* y2;

        public static float CrossProduct(float x1, float y1, float x2, float y2)
        {
            return x1 * y2 - y1 * x2;
        }

        public static float DistanceToLine(Vector2 a, Vector2 b, Vector2 p)
        {
            var ap = p - a;
            var ab = b - a;

            return Vector2.Dot(ap, ab) / (ab.Length * ab.Length);
        }

    }
}