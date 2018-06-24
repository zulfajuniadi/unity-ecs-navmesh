using System.Collections.Generic;
using Town.Geom;

namespace Town
{
    public class TownGeometry
    {
        public TownGeometry ()
        {
            Buildings = new List<Building> ();
            Walls = new List<Edge> ();
            Towers = new List<Vector2> ();
            Gates = new List<Vector2> ();
            Roads = new List<List<Vector2>> ();
            Overlay = new List<Patch> ();
            Water = new List<Polygon> ();
        }

        public List<Building> Buildings { get; }
        public List<Edge> Walls { get; }
        public List<Vector2> Towers { get; }
        public List<Vector2> Gates { get; }
        public List<List<Vector2>> Roads { get; }
        public List<Patch> Overlay { get; }
        public List<Polygon> Water { get; }
        public Polygon WaterBorder { get; set; }
    }
}