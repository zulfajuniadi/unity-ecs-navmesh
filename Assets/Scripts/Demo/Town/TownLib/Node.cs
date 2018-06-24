using System.Collections.Generic;

namespace Town
{
    public class Node
    {
        private static int _counter;
        public Dictionary<Node, float> Links = new Dictionary<Node, float>();
        public readonly int Id;

        public Node()
        {
            Id = _counter++;
        }

        public void Link(Node node, float price = 1f, bool symmetrical = true)
        {
            Links[node] = price;
            if (symmetrical)
            {
                node.Links[this] = price;
            }
        }

        public void Unlink(Node node, bool symmetrical = true)
        {
            Links.Remove(node);
            if (symmetrical)
            {
                node.Links.Remove(this);
            }
        }

        public void UnlinkAll()
        {
            Links.Clear();
        }

        public override string ToString()
        {
            return $"Node({Id})";
        }
    }
}