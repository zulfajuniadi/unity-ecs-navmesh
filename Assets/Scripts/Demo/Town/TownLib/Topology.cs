using System.Collections.Generic;
using System.Linq;
using Town.Geom;

namespace Town
{
    public class Topology
    {
        private readonly Graph _graph = new Graph();
        public readonly Dictionary<Vector2, Node> V2Node = new Dictionary<Vector2, Node>();
        public readonly Dictionary<Node, Vector2> Node2V = new Dictionary<Node, Vector2>();

        private readonly List<Vector2> _blocked = new List<Vector2>();

        public readonly List<Node> Inner = new List<Node>();
        public readonly List<Node> Outer = new List<Node>();

        public Topology(Town town)
        {
            if (town.Castle != null)
            {
                _blocked.AddRange(town.Castle.Patch.Shape.Vertices);
            }

            if (town.CityWall != null)
            {
                _blocked.AddRange(town.CityWall.Circumference);
                _blocked = _blocked.Except(town.Gates).ToList();
            }

            _blocked.AddRange(town.Patches.Where(p => p.Water).SelectMany(p => p.Shape.Vertices).Distinct());

            var border = town.CityWall.Circumference;

            BuildTopology(town, border);
        }

        private void BuildTopology(Town town, List<Vector2> border)
        {
            foreach (var patch in town.Patches)
            {
                var inCity = patch.WithinCity;

                var v1 = patch.Shape.Vertices.Last();
                var n1 = ProcessPoint(v1);

                foreach (var vertex in patch.Shape.Vertices)
                {
                    var v0 = v1;
                    v1 = vertex;
                    var n0 = n1;
                    n1 = ProcessPoint(v1);

                    if (n0 != null && !border.Contains(v0))
                    {
                        if (inCity)
                        {
                            Inner.Add(n0);
                        }
                        else
                        {
                            Outer.Add(n0);
                        }
                    }

                    if (n1 != null && !border.Contains(v1))
                    {
                        if (inCity)
                        {
                            Inner.Add(n1);
                        }
                        else
                        {
                            Outer.Add(n1);
                        }
                    }

                    if (n0 != null && n1 != null)
                    {
                        n0.Link(n1, (v0 - v1).Length);
                    }
                }
            }
        }

        private Node ProcessPoint(Vector2 vector)
        {
            Node node;
            if (V2Node.ContainsKey(vector))
            {
                node = V2Node[vector];
            }
            else
            {
                node = _graph.Add();
                V2Node[vector] = node;
                Node2V[node] = vector;
            }

            return _blocked.Contains(vector) ? null : node;
        }

        public List<Vector2> BuildPath(Vector2 from, Vector2 to, List<Node> exclude = null)
        {
            var path = _graph.AStar(V2Node[from], V2Node[to], exclude);
            return path?.Select(n => Node2V[n]).ToList();
        }
    }
}