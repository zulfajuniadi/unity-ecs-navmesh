using System;
using System.Collections.Generic;
using System.Linq;
using Town.Geom;

namespace Town
{
    public class Patch : IEquatable<Patch>
    {
        private static int _id;
        private Patch (Town town, Geom.Polygon shape)
        {
            Town = town;
            Shape = shape;
            Id = ++_id;
        }

        public static Patch FromRegion (Town town, Region region)
        {
            return new Patch (town, new Geom.Polygon (region.Vertices.Select (vv => vv.C)));
        }

        public static Patch FromPolygon (Town town, Geom.Polygon polygon)
        {
            return new Patch (town, polygon);
        }

        public int Id { get; }

        public Town Town { get; }
        public Area Area { get; set; }
        public Polygon Shape { get; }
        public List<Edge> Edges => Shape.GetEdges ();
        public bool WithinCity { get; set; }
        public bool WithinWalls { get; set; }
        public bool Water { get; set; }
        public bool HasCastle { get; set; }

        public Vector2 Center => Shape.Center;

        public bool Neighbours (Patch other)
        {
            if (Equals (other))
            {
                // Not neighbour with itself
                return false;
            }
            return Shape.Vertices.Any (v => other.Shape.Vertices.Contains (v));
            //return Edges.Any(e => other.Edges.Any(oe => oe.Equals(e)));
        }

        public IEnumerable<Patch> GetAllNeighbours ()
        {
            return Town.Patches.Where (Neighbours);
        }

        public bool Equals (Patch other)
        {
            if (ReferenceEquals (null, other))
            {
                return false;
            }
            if (ReferenceEquals (this, other))
            {
                return true;
            }
            return Equals (Shape, other.Shape);
        }

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (null, obj))
            {
                return false;
            }
            if (ReferenceEquals (this, obj))
            {
                return true;
            }
            if (obj.GetType () != this.GetType ())
            {
                return false;
            }
            return Equals ((Patch) obj);
        }

        public override int GetHashCode ()
        {
            return (Shape != null ? Shape.GetHashCode () : 0);
        }
    }
}