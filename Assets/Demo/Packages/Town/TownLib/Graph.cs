using System.Collections.Generic;
using System.Linq;

namespace Town
{
    public class Graph
    {
        public readonly List<Node> Nodes = new List<Node>();

        public Node Add(Node node = null)
        {
            if (node == null)
            {
                node = new Node();
            }
            Nodes.Add(node);
            return node;
        }

        public void Remove(Node node)
        {
            node.UnlinkAll();
            Nodes.Remove(node);
        }

        public List<Node> AStar(Node start, Node goal, List<Node> exclude = null)
        {
            var closedSet = new List<Node>(exclude ?? new List<Node>()).Distinct().ToList();
            var openSet = new List<Node> { start };
            var cameFrom = new Dictionary<Node, Node>();

            closedSet.Remove(goal);

            var gScore = new Dictionary<Node, float> { { start, 0 } };

            while (openSet.Any())
            {
                var current = openSet[0];
                if (current == goal)
                {
                    // DONE!
                    return BuildPath(cameFrom, current).ToList();
                }

                openSet.Remove(current);
                closedSet.Add(current);

                var currentScore = gScore[current];

                foreach (var neighbour in current.Links.Keys.Except(closedSet))
                {
                    var score = currentScore + current.Links[neighbour];
                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }

                    var neighbourScore = float.MaxValue;
                    if (gScore.ContainsKey(neighbour))
                    {
                        neighbourScore = gScore[neighbour];
                    }

                    if (score <= neighbourScore)
                    {
                        cameFrom[neighbour] = current;
                        gScore[neighbour] = score;
                    }
                }
            }

            return null;
        }

        private IEnumerable<Node> BuildPath(Dictionary<Node, Node> cameFrom, Node current)
        {
            yield return current;
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                yield return current;
            }
        }

        public float CalculatePrice(IList<Node> path)
        {
            if (path.Count < 2)
            {
                return 0;
            }

            var price = 0f;
            var current = path[0];
            var next = path[1];

            for (var i = 1; i < path.Count - 1; i++)
            {
                if (current.Links.ContainsKey(next))
                {
                    price += current.Links[next];
                }
                else
                {
                    return float.NaN;
                }
                current = next;
                next = path[i + 1];
            }

            return price;
        }
    }
}