using System.Collections.Generic;

namespace Town.Geom
{
    public class Vector2LengthComparer : IComparer<Vector2>
    {
        public int Compare(Vector2 x, Vector2 y)
        {
            return x.Length.CompareTo(y.Length);
        }
    }
}