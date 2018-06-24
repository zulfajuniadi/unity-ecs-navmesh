using System;
using System.Collections.Generic;
using System.Linq;
using Town.Geom;

namespace Town
{
    public class Wall
    {
        private readonly Town _town;
        public List<Vector2> Circumference { get; }
        public List<Vector2> Gates { get; }
        public List<Vector2> Towers { get; }

        public Wall (List<Vector2> allowedTowerPositions, Town town, int minNumberOfGates, int maxNumberOfGates, List<Vector2> illegalGatePositions = null)
        {
            _town = town;
            Circumference = allowedTowerPositions.ToList ();

            Gates = new List<Vector2> ();
            Towers = new List<Vector2> ();

            illegalGatePositions = illegalGatePositions ?? new List<Vector2> ();

            PlaceGatesAndTowers (allowedTowerPositions, minNumberOfGates, maxNumberOfGates, illegalGatePositions);
        }

        public bool Borders (Edge edge)
        {
            var aIndex = Circumference.IndexOf (edge.A);
            if (aIndex == -1)
            {
                return false;
            }

            var nextIndex = (aIndex + 1) % Circumference.Count;
            var prevIndex = aIndex > 0 ? aIndex - 1 : Circumference.Count - 1;

            if (Circumference[nextIndex] == edge.B || Circumference[prevIndex] == edge.B)
            {
                return true;
            }

            return false;
        }

        public IEnumerable<Edge> GetEdges ()
        {
            return Edge.FromPointList (Circumference);
        }

        private void PlaceGatesAndTowers (IEnumerable<Vector2> allowedTowerPositions, int minNumberOfGates, int maxNumberOfGates, List<Vector2> illegalGatePositions)
        {
            var cityPatches = _town.Patches.Where (p => p.WithinCity).ToList ();
            var outsidePatches = _town.Patches.Except (cityPatches).ToList ();
            var possibleGates = allowedTowerPositions
                .Where (v => cityPatches.Count (cp => cp.Shape.Vertices.Contains (v)) > 1)
                .Except (illegalGatePositions)
                .OrderByDescending (v => (_town.Castle.Patch.Center - v).Length)
                .ToList ();

            var towers = Circumference.ToList ();
            var gates = new List<Vector2> ();

            var attempts = 0;
            while ((gates.Count < minNumberOfGates || attempts < 4) && possibleGates.Any () && gates.Count < maxNumberOfGates)
            {
                attempts++;
                var newGate = possibleGates.First ();

                try
                {
                    possibleGates.Remove (newGate);
                    possibleGates.RemoveFirstIfPossible ();
                    possibleGates.RemoveFirstIfPossible ();

                    var outerNeighbours = outsidePatches
                        .Where (p => p.Shape.Vertices.Any (v => v.Equals (newGate)))
                        .ToList ();

                    if (outerNeighbours.Count == 1)
                    {
                        var neighbour = outerNeighbours.Single ();
                        var wallPoint = neighbour.Shape.GetNextVertex (newGate) -
                            neighbour.Shape.GetPreviousVertex (newGate);
                        var outPoint = new Vector2 (wallPoint.y, -wallPoint.x);
                        var possibleSplitPoints = neighbour.Shape.Vertices.Except (Circumference).ToList ();
                        var farthest = possibleSplitPoints.OrderByDescending (p =>
                        {
                            var dir = p - newGate;
                            return Vector2.Dot (dir, outPoint) / dir.Length;
                        }).First ();

                        var newPatches = neighbour.Shape.Split (newGate, farthest).Select (p => Patch.FromPolygon (_town, p)).ToList ();
                        if (newPatches.Any (p => p.Shape.Vertices.Count < 3))
                        {
                            farthest = neighbour.Shape.GetNextVertex (farthest);
                            newPatches = neighbour.Shape.Split (newGate, farthest).Select (p => Patch.FromPolygon (_town, p)).ToList ();
                            if (newPatches.Any (p => p.Shape.Vertices.Count < 3))
                            {
                                throw new InvalidOperationException (
                                    "Splitting patch resulted in polygon with only two points");
                            }
                        }
                        _town.Patches.Remove (neighbour);
                        _town.Patches.AddRange (newPatches);
                    }

                    gates.Add (newGate);
                    towers.Remove (newGate);

                }
                catch (InvalidOperationException) { }

            }

            _town.Gates.AddRange (gates);
            Gates.AddRange (gates);
            Towers.AddRange (towers);
        }

        public void ReplaceWallPoint (Vector2 oldPoint, Vector2 newPoint)
        {
            var index = Circumference.IndexOf (oldPoint);
            if (index >= 0)
            {
                Circumference[index] = newPoint;
            }

            var gateIndex = Gates.IndexOf (oldPoint);
            if (gateIndex >= 0)
            {
                Gates[gateIndex] = newPoint;
            }

            var towerIndex = Towers.IndexOf (oldPoint);
            if (towerIndex >= 0)
            {
                Towers[towerIndex] = newPoint;
            }
        }

    }
}