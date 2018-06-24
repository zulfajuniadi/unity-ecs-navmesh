using System;
using System.Collections.Generic;
using System.Linq;
using Town.Geom;

namespace Town
{
    public abstract class Area
    {
        public const float MainStreet = 5.0f;
        public const float RegularStreet = 3.0f;
        public const float Alley = 2f;
        public const float NarrowBuilding = 0.3f;

        protected Area (Patch patch)
        {
            Patch = patch;
        }

        protected Patch Patch { get; }

        public abstract IEnumerable<Polygon> GetGeometry ();
    }

    public class CastleArea : TownArea
    {
        public CastleArea (Patch patch) : base (patch) { }

        protected override void GetBuildingSpecs (out float gridChaos, out float sizeChaos, out Func<Polygon, float> emptyProbabilityFunc, out float minArea)
        {
            minArea = 200 + 200 * (float) Rnd.NextDouble () * (float) Rnd.NextDouble ();
            gridChaos = 0;
            sizeChaos = 0;
            emptyProbabilityFunc = p => 0.8f;
        }
    }

    public class PoorArea : TownArea
    {
        public PoorArea (Patch patch) : base (patch) { }

        protected override void GetBuildingSpecs (out float gridChaos, out float sizeChaos, out Func<Polygon, float> emptyProbabilityFunc, out float minArea)
        {
            minArea = 10 + 30 * (float) Rnd.NextDouble () * (float) Rnd.NextDouble ();
            gridChaos = 0.2f + (float) Rnd.NextDouble () * 0.02f;
            sizeChaos = 1f;
            emptyProbabilityFunc = p => 0.01f;
        }
    }
    public class OutsideWallArea : TownArea
    {
        public OutsideWallArea (Patch patch) : base (patch) { }

        protected override void GetBuildingSpecs (out float gridChaos, out float sizeChaos, out Func<Polygon, float> emptyProbabilityFunc, out float minArea)
        {
            minArea = 5 + 30 * (float) Rnd.NextDouble () * (float) Rnd.NextDouble ();
            gridChaos = 0.4f + (float) Rnd.NextDouble () * 0.02f;
            sizeChaos = 1f;
            var closestWallPoint = Patch.Town.CityWall.Circumference.OrderBy (p => (p - Patch.Center).Length).First ();
            var centerDistance = (Patch.Center - closestWallPoint).Length;
            emptyProbabilityFunc = p =>
            {
                var distanceToWall = (p.Center - closestWallPoint).Length;
                var normalizedDistanceSq = (float) Math.Pow (distanceToWall / (1.5f * centerDistance), 3);
                return Math.Max (Math.Min (normalizedDistanceSq, 0.95f), 0.05f);
            };
        }
    }

    public class RichArea : TownArea
    {
        public RichArea (Patch patch) : base (patch) { }

        protected override void GetBuildingSpecs (out float gridChaos, out float sizeChaos, out Func<Polygon, float> emptyProbabilityFunc, out float minArea)
        {
            minArea = 60 + 160 * (float) Rnd.NextDouble () * (float) Rnd.NextDouble ();
            gridChaos = 0.05f + (float) Rnd.NextDouble () * 0.02f;
            sizeChaos = 0.2f;
            emptyProbabilityFunc = p => 0.2f;
        }
    }

    public class TownArea : Area
    {
        private readonly Town _town;

        private Polygon GetCityBlock ()
        {
            var insetDist = new List<float> ();

            // var innerPatch = true; // model.wall == null || patch.withinWalls;

            foreach (var edge in Patch.Edges)
            {
                if (_town.CityWall.Borders (edge) && _town.Options.Walls)
                {
                    insetDist.Add (MainStreet / 2);
                }
                else
                {
                    var streetVertices = _town.Streets.SelectMany (s => s).ToList ();
                    var onStreet = streetVertices.Contains (edge.A) && streetVertices.Contains (edge.B);
                    if (!onStreet)
                    {
                        onStreet = _town.Market.Edges.Contains (edge);
                    }

                    if (onStreet)
                    {
                        insetDist.Add (MainStreet / 2);
                    }
                    else
                    {
                        insetDist.Add (RegularStreet / 2);
                    }
                }
            }

            if (Patch.Shape.IsConvex)
            {
                return Patch.Shape.Shrink (insetDist);
            }

            return Patch.Shape.Buffer (insetDist);
        }

        private static IEnumerable<Polygon> CreateAlleys (Polygon block, float minArea, float gridChaos, float sizeChaos, Func<Polygon, float> emptyProbabilityFunc, bool split, int levels = 0)
        {
            Vector2 point = Vector2.Zero;
            var length = float.MinValue;
            block.ForEachEdge ((p1, p2, index) =>
            {
                var len = (p1 - p2).Length;
                if (len > length)
                {
                    length = len;
                    point = p1;
                }
            });

            var spread = 0.8f * gridChaos;
            var ratio = (1 - spread) / 2 + (float) Rnd.NextDouble () * spread;

            // Trying to keep buildings rectangular even in chaotic wards
            var angleSpread = (float) (Math.PI / 6 * gridChaos * (block.Area () < minArea * 4 ? 0 : 1));
            var angle = ((float) Rnd.NextDouble () - 0.5f) * angleSpread;

            var halves = block.Bisect (point, ratio, angle, split ? Alley : 0f);

            var buildings = new List<Polygon> ();

            foreach (var half in halves)
            {
                if (half.Area () < minArea * Math.Pow (2, 4 * sizeChaos * (Rnd.NextDouble () - 0.5f)) || levels > 5)
                {
                    if (!Rnd.NextBool (emptyProbabilityFunc (half)))
                    {
                        buildings.Add (half);
                    }
                }
                else
                {
                    buildings.AddRange (CreateAlleys (half, minArea, gridChaos, sizeChaos, emptyProbabilityFunc, half.Area () > minArea / (Rnd.NextDouble () * Rnd.NextDouble ()), levels + 1));
                }
            }

            return buildings;
        }

        private IEnumerable<Polygon> GetBuildings ()
        {
            if (Patch.Equals (_town.Market))
            {
                yield break;
            }

            var block = GetCityBlock ();
            GetBuildingSpecs (out var gridChaos, out var sizeChaos, out var emptyProbabilityFunc, out var minArea);

            foreach (var building in CreateAlleys (block, minArea, gridChaos, sizeChaos, emptyProbabilityFunc, true))
            {
                building.GetLongestEdge (out var e1, out var e2, out var len);

                var angle = (e2 - e1).Angle ();
                var bounds = building.Rotate (angle).GetBoundingBox ();

                var hwRatio = bounds.Height / bounds.Width;
                var whRatio = bounds.Width / bounds.Height;

                if (hwRatio > NarrowBuilding && whRatio > NarrowBuilding)
                {
                    yield return building;
                }
            }
        }

        protected virtual void GetBuildingSpecs (out float gridChaos, out float sizeChaos, out Func<Polygon, float> emptyProbabilityFunc, out float minArea)
        {
            minArea = 10 + 80 * (float) Rnd.NextDouble () * (float) Rnd.NextDouble ();
            gridChaos = 0.05f + (float) Rnd.NextDouble () * 0.02f;
            sizeChaos = 0.06f;
            emptyProbabilityFunc = p => 0.02f;
        }

        public override IEnumerable<Polygon> GetGeometry ()
        {
            var buildings = GetBuildings ().ToList ();

            foreach (var b in buildings)
            {
                var building = b.SortPointsClockwise ().RemoveSharpEdges (); //.Simplify();
                if (building.Vertices.Count > 3)
                {
                    yield return building;
                }
            }
        }

        public TownArea (Patch patch) : base (patch)
        {
            _town = patch.Town;
        }
    }

    public class FarmArea : TownArea
    {
        protected override void GetBuildingSpecs (out float gridChaos, out float sizeChaos, out Func<Polygon, float> emptyProbabilityFunc, out float minArea)
        {
            minArea = 10 + 80 * (float) Rnd.NextDouble () * (float) Rnd.NextDouble ();
            gridChaos = 0.05f + (float) Rnd.NextDouble () * 0.02f;
            sizeChaos = 2f;
            emptyProbabilityFunc = p => 0.99f;
        }

        public FarmArea (Patch patch) : base (patch) { }
    }

    public class EmptyArea : Area
    {
        public EmptyArea (Patch patch) : base (patch) { }

        public override IEnumerable<Polygon> GetGeometry ()
        {
            return new Polygon[0];
        }
    }
}