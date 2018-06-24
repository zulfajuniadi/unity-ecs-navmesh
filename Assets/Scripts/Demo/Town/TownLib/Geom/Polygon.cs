using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;

namespace Town.Geom
{
    public class Polygon : IEquatable<Polygon>
    {
        public const float Delta = 0.00001f;

        public readonly List<Vector2> Vertices;
        public Vector2 Center => GetCenter();

        public Polygon(IEnumerable<Vector2> vertices)
        {
            Vertices = vertices.Select(v => new Vector2(v.x, v.y)).ToList();
        }

        public Polygon(Polygon source)
        {
            Vertices = source.Vertices.ToList();
        }

        public Polygon()
        {
            Vertices = new List<Vector2>();
        }

        public Polygon(params Vector2[] vertices) : this((IEnumerable<Vector2>)vertices)
        {

        }

        public static Polygon Rectangle(Vector2 location, float width, float height)
        {
            return new Polygon(location, new Vector2(location.x + width, location.y), new Vector2(location.x + width, location.y + height), new Vector2(location.x, location.y + height));
        }

        public Polygon Subtract(Polygon other)
        {
            return Subtract(new[] { other });
        }

        public Polygon Subtract(IEnumerable<Polygon> others)
        {
            var clipper = new Clipper();

            var thisPoints = Vertices.Select(v => new IntPoint(v.x, v.y)).ToList();
            clipper.AddPath(thisPoints, PolyType.ptSubject, true);

            foreach (var other in others)
            {
                var overlapPoints = other.Vertices.Select(v => new IntPoint(v.x, v.y)).ToList();
                clipper.AddPath(overlapPoints, PolyType.ptClip, true);
            }

            var solution = new List<List<IntPoint>>();
            var success = clipper.Execute(ClipType.ctDifference, solution);

            if (success && solution.Any())
            {
                var newPoly = solution.First().Select(p => new Vector2(p.X, p.Y));
                return new Polygon(newPoly);
            }

            return this;
        }

        public float Area()
        {
            return Math.Abs(SignedDoubleArea() * 0.5f);
        }

        public bool IsConvex
        {
            get
            {
                foreach (var vertex in Vertices)
                {
                    var prev = GetPreviousVertex(vertex);
                    var next = GetNextVertex(vertex);
                    var crossProduct = GeometryHelpers.CrossProduct(vertex.x - prev.x, vertex.y - prev.y, next.x - vertex.x, next.y - vertex.y);
                    if (crossProduct <= 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public Rectangle GetBoundingBox()
        {
            var minX = Vertices.Min(v => v.x);
            var maxX = Vertices.Max(v => v.x);
            var minY = Vertices.Min(v => v.y);
            var maxY = Vertices.Max(v => v.y);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private float SignedDoubleArea()
        {
            int index, nextIndex;
            int n = Vertices.Count;
            Vector2 point, next;
            float signedDoubleArea = 0;

            for (index = 0; index < n; index++)
            {
                nextIndex = (index + 1) % n;
                point = Vertices[index];
                next = Vertices[nextIndex];
                signedDoubleArea += point.x * next.y - next.x * point.y;
            }

            return signedDoubleArea;
        }

        public Vector2 GetCenter()
        {
            var sum = new Vector2();
            Vertices.ForEach(v => sum += v);

            sum = Vector2.Scale(sum, 1f / Vertices.Count);
            return sum;
        }

        public int GetVertexIndex(Vector2 vertex)
        {
            var idx = Vertices.IndexOf(vertex);
            if (idx < 0)
            {
                throw new InvalidOperationException("Vertex " + vertex + " is not part of the polygon!");
            }
            return idx;
        }

        public Vector2 GetNextVertex(Vector2 vertex)
        {
            return GetNextVertex(GetVertexIndex(vertex));
        }

        public Vector2 GetNextVertex(int index)
        {
            index = (index + 1) % Vertices.Count;
            return Vertices[index];
        }

        public Vector2 GetPreviousVertex(Vector2 vertex)
        {
            return GetPreviousVertex(GetVertexIndex(vertex));
        }

        public Vector2 GetPreviousVertex(int index)
        {
            index = index - 1;
            if (index < 0)
            {
                index += Vertices.Count;
            }

            return Vertices[index];
        }

        public void ReplaceVertex(Vector2 oldVertex, Vector2 newVertex)
        {
            var index = GetVertexIndex(oldVertex);
            Vertices[index] = newVertex;
        }

        public IEnumerable<Polygon> Split(Vector2 p1, Vector2 p2)
        {
            return Split(Vertices.IndexOf(p1), Vertices.IndexOf(p2));
        }

        public IEnumerable<Polygon> Split(int i1, int i2)
        {
            if (i2 == Vertices.Count)
            {
                i2 = Vertices.Count - 1;
            }

            if (i1 > i2)
            {
                var t = i1; i1 = i2; i2 = t;
            }

            var firstVertices = Vertices.GetRange(i1, i2 - i1 + 1);
            var secondVertices = Vertices.GetRange(i2, Vertices.Count - i2).Concat(Vertices.Take(i1 + 1));

            return new[]
            {
                new Polygon(firstVertices),
                new Polygon(secondVertices)
            };
        }

        public void ForEachEdge(Action<Vector2, Vector2, int> action)
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                var p1 = Vertices[i];
                var p2 = Vertices[(i + 1) % Vertices.Count];
                action(p1, p2, i);
            }
        }

        public void GetLongestEdge(out Vector2 start, out Vector2 end, out float length)
        {
            var e1 = Vector2.Zero;
            var e2 = Vector2.Zero;

            var len = float.MinValue;

            ForEachEdge((p1, p2, i) =>
            {
                var l = (p1 - p2).Length;
                if (l > len)
                {
                    len = l;
                    e1 = p1;
                    e2 = p2;
                }
            });

            start = e1;
            end = e2;
            length = len;
        }

        public List<Edge> GetEdges()
        {
            var edges = new List<Edge>();
            ForEachEdge((a, b, i) =>
            {
                edges.Add(new Edge(a, b));
            });
            return edges;
        }

        public Polygon Shrink(float amount)
        {
            return Shrink(Vertices.Select(v => amount).ToList());
        }

        public Polygon Shrink(IList<float> shrinkAmounts)
        {
            var newPoly = new Polygon(this);
            ForEachEdge((p1, p2, index) =>
            {
                var amount = shrinkAmounts[index];
                if (amount > 0)
                {
                    var n = Vector2.Scale(Vector2.Normalize(Vector2.Rotate90(p2 - p1)), amount);
                    newPoly = newPoly.Cut(p1 + n, p2 + n, 0).First();
                }
            });
            return newPoly;
        }

        public Polygon Buffer(IList<float> shrinkAmounts)
        {
            var newPoly = new Polygon();
            ForEachEdge((p1, p2, index) =>
            {
                var amount = shrinkAmounts[index];
                if (amount <= 0.01)
                {
                    newPoly.Vertices.Add(p1);
                    newPoly.Vertices.Add(p2);
                }
                else
                {
                    var n = Vector2.Scale(Vector2.Normalize(Vector2.Rotate90(p2 - p1)), amount);
                    newPoly.Vertices.Add(p1 + n);
                    newPoly.Vertices.Add(p2 + n);
                }
            });

            bool wasCut;
            var lastEdge = 0;

            do
            {
                wasCut = false;

                var n = newPoly.Vertices.Count;

                for (var i = lastEdge; i < n - 2; i++)
                {
                    lastEdge = i;

                    var p11 = newPoly.Vertices[i];
                    var p12 = newPoly.Vertices[i + 1];
                    var x1 = p11.x;
                    var y1 = p11.y;
                    var dx1 = p12.x - x1;
                    var dy1 = p12.y - y1;

                    var maxJ = i > 0 ? n : n - 1;
                    for (var j = i + 2; j < maxJ; j++)
                    {
                        var p21 = newPoly.Vertices[j];
                        var p22 = j < n - 1 ? newPoly.Vertices[j + 1] : newPoly.Vertices[0];
                        var x2 = p21.x;
                        var y2 = p21.y;
                        var dx2 = p22.x - x2;
                        var dy2 = p22.y - y2;

                        var intersect = GeometryHelpers.IntersectLines(x1, y1, dx1, dy1, x2, y2, dx2, dy2);
                        if (intersect != null && intersect.x > Delta && intersect.x < (1 - Delta) && intersect.y > Delta && intersect.y < (1 - Delta))
                        {
                            var pn = new Vector2(x1 + dx1 * intersect.x, y1 + dy1 * intersect.x);
                            newPoly.Vertices.Insert(j + 1, pn);
                            newPoly.Vertices.Insert(i + 1, pn);

                            wasCut = true;
                            break;
                        }
                    }
                }
            } while (wasCut);

            var regular = Enumerable.Range(0, newPoly.Vertices.Count).ToList();

            Polygon bestPart = null;
            var bestPartSq = float.MinValue;

            while (regular.Count > 0)
            {
                var indices = new List<int>();
                var start = regular[0];
                var i = start;
                do
                {
                    indices.Add(i);
                    regular.Remove(i);

                    var next = (i + 1) % newPoly.Vertices.Count;
                    var v = newPoly.Vertices[next];
                    var next1 = newPoly.Vertices.IndexOf(v);
                    if (next1 == next)
                    {
                        next1 = newPoly.Vertices.LastIndexOf(v);
                    }
                    i = next1 == -1 ? next : next1;                    
                } while (i != start && indices.Count < 1000);

                if (indices.Count >= 999)
                {
                    indices = indices.Take(4).ToList();
                }
                
                var poly = new Polygon(indices.Select(v => newPoly.Vertices[v]));

                var s = poly.Area();
                if (s > bestPartSq)
                {
                    bestPart = poly;
                    bestPartSq = s;
                }
            }

            return bestPart;
        }

        public IEnumerable<Polygon> Cut(Vector2 p1, Vector2 p2, float gap)
        {
            var x1 = p1.x;
            var y1 = p1.y;
            var dx1 = p2.x - x1;
            var dy1 = p2.y - y1;

            var len = Vertices.Count;
            var edge1 = 0;
            var ratio1 = 0.0f;
            var edge2 = 0;
            var ratio2 = 0.0f;
            var count = 0;

            for (var i = 0; i < len; i++)
            {
                var v0 = Vertices[i];
                var v1 = Vertices[(i + 1) % len];

                var x2 = v0.x;
                var y2 = v0.y;
                var dx2 = v1.x - x2;
                var dy2 = v1.y - y2;

                var t = GeometryHelpers.IntersectLines(x1, y1, dx1, dy1, x2, y2, dx2, dy2);
                if (t.IsValid && t.y >= 0 && t.y <= 1)
                {
                    switch (count)
                    {
                        case 0:
                            edge1 = i;
                            ratio1 = t.x;
                            break;
                        case 1:
                            edge2 = i;
                            ratio2 = t.x;
                            break;
                    }
                    count++;
                }
            }

            if (count == 2)
            {
                var point1 = p1 + Vector2.Scale(p2 - p1, ratio1);
                var point2 = p1 + Vector2.Scale(p2 - p1, ratio2);

                var half1 = new Polygon(Vertices.GetRange(edge1 + 1, edge2 - edge1));
                half1.Vertices.Insert(0, point1);
                half1.Vertices.Add(point2);

                var half2 = new Polygon(Vertices.GetRange(edge2 + 1, Vertices.Count - edge2 - 1).Concat(Vertices.GetRange(0, edge1 + 1)));
                half2.Vertices.Insert(0, point2);
                half2.Vertices.Add(point1);

                if (gap > 0)
                {
                    half1 = half1.Peel(point2, gap / 2);
                    half2 = half2.Peel(point1, gap / 2);
                }

                var v = VectorI(edge1);

                return GeometryHelpers.CrossProduct(dx1, dy1, v.x, v.y) > 0
                    ? new[] { half1, half2 }
                    : new[] { half2, half1 };
            }

            return new[] { new Polygon(this) };

        }

        public Vector2 VectorI(int index)
        {
            var v1 = Vertices[index];
            var v2 = GetNextVertex(v1);

            return v2 - v1;
        }

        public Polygon Peel(Vector2 v1, float amount)
        {
            var v2 = GetNextVertex(v1);

            var n = Vector2.Scale(Vector2.Normalize(Vector2.Rotate90(v2 - v1)), amount);

            return Cut(v1 + n, v2 + n, 0).First();
        }


        public IEnumerable<Polygon> Bisect(Vector2 vertex, float ratio = 0.5f, float angle = 0.0f, float gap = 0.0f)
        {
            var next = GetNextVertex(vertex);

            var p1 = GeometryHelpers.Interpolate(vertex, next, ratio);
            var d = next - vertex;

            var cosB = Math.Cos(angle);
            var sinB = Math.Sin(angle);
            var vx = d.x * cosB - d.y * sinB;
            var vy = d.y * cosB + d.x * sinB;
            var p2 = new Vector2(p1.x - vy, p1.y + vx);

            return Cut(p1, p2, gap);
        }

        public bool ContainsPoint(Vector2 point)
        {
            var polygonLength = Vertices.Count;
            var i = 0;
            var inside = false;

            // x, y for tested point.
            float pointX = point.x, pointY = point.y;

            // start / end point for the current polygon segment.
            var endPoint = Vertices[polygonLength - 1];
            var endX = endPoint.x;
            var endY = endPoint.y;

            while (i < polygonLength)
            {
                var startX = endX;
                var startY = endY;
                endPoint = Vertices[i++];
                endX = endPoint.x;
                endY = endPoint.y;
                //
                inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                          && /* if so, test if it is under the segment */
                          ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }
            return inside;
        }

        public Polygon Rotate(float angle)
        {
            return new Polygon(Vertices.Select(vertex => vertex.RotateAoundPoint(Center, angle)));
        }

        public Polygon Translate(Vector2 amount)
        {
            return new Polygon(Vertices.Select(v => v - amount));
        }

        public Polygon RemoveSharpEdges()
        {
            var newPoly = new Polygon(this);

            var verticesWithSharpAngle = newPoly.GetVerticeIndexecWithSharpAngles().ToList();
            while (verticesWithSharpAngle.Any())
            {
                newPoly.Vertices.RemoveAt(verticesWithSharpAngle.First());
                verticesWithSharpAngle = newPoly.GetVerticeIndexecWithSharpAngles().ToList();
            }

            return newPoly;
        }

        private IEnumerable<int> GetVerticeIndexecWithSharpAngles()
        {
            return Vertices.Select((v, i) => new { a = AngleAtVertex(i), i }).Where(t => Math.Abs(t.a) < Math.PI / 4).Select(t => t.i);
        }

        public float AngleAtVertex(int index)
        {
            var pt = Vertices[index];
            var prev = GetPreviousVertex(index);
            var next = GetNextVertex(index);

            return Vector2.AngleThreePoints(prev, pt, next);
        }

        public Polygon SortPointsClockwise()
        {
            var sortedVertices = Vertices.OrderBy(v => (v - Center).Angle()).ToList();
            return new Polygon(sortedVertices);
        }

        public Polygon Simplify(float areaThreshold = 0.5f, float distanceThreshhold = 2f)
        {
            var toRemove = Vertices.Select((v, i) =>
            {
                var prev = GetPreviousVertex(i);
                var next = GetNextVertex(i);
                return new { a = new Polygon(prev, v, next).Area(), i };
            }).Where(v => v.a < areaThreshold).Select(v => v.i).ToList();

            var simplified = Vertices.Where((v, i) => !toRemove.Contains(i)).Distinct().ToList();

            var tooClose = simplified.Where(v1 => simplified.Where(v2 => !v2.Equals(v1)).Any(v2 => (v2 - v1).Length < distanceThreshhold)).ToList();
            while (tooClose.Any())
            {
                simplified.Remove(tooClose.First());
                tooClose = simplified.Where(v1 => simplified.Where(v2 => !v2.Equals(v1)).Any(v2 => (v2 - v1).Length < distanceThreshhold)).ToList();
            }

            return new Polygon(simplified);
        }

        public Polygon SmoothVertices(float amount = 1f)
        {
            var len = Vertices.Count;
            var v1 = Vertices[len - 1];
            var v2 = Vertices[0];

            var newVertices = new List<Vector2>();

            for (var v = 0; v < Vertices.Count; v++)
            {
                var v0 = v1;
                v1 = v2;
                v2 = Vertices[(v + 1) % len];
                newVertices.Add(Vector2.SmoothVertex(v1, v0, v2));
            }

            return new Polygon(newVertices);
        }
        

        public Polygon RectangleInside()
        {
            var p1 = Vector2.Zero;
            var p2 = Vector2.Zero;
            var maxLength = float.MinValue;

            ForEachEdge((a, b, i) =>
            {
                var len = (a - b).Length;
                if (len > maxLength)
                {
                    p1 = a;
                    p2 = b;
                    maxLength = len;
                }
            });

            var rotated = Rotate((p1 - p2).Angle());
            var minY = rotated.Vertices.Min(v => v.y);
            var greatestDistance = rotated.Vertices.Max(v => v.y - minY);

            var normal = (p2 - p1).Rotate90().Normalize();
            var newHouse = new Polygon(p1, p2, p2 - Vector2.Scale(normal, greatestDistance),
                p1 - Vector2.Scale(normal, greatestDistance)).ZoomShrink(0.2f);

            newHouse = newHouse.Translate(newHouse.Center- Center);


            return newHouse;          
        }

        public Polygon ZoomShrink(float amount)
        {
            var newVertices = Vertices.Select(v =>
            {
                var d = Center - v;
                return v + Vector2.Scale(d, amount);
            }).ToList();
            return new Polygon(newVertices);
        }

        public bool Equals(Polygon other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Vertices.TrueForAll(v => other.Vertices.Contains(v));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Polygon)obj);
        }

        public override int GetHashCode()
        {
            return (Vertices != null ? Vertices.GetHashCode() : 0);
        }
        
        public string ToSvgPolygon(string className, string extra = "")
        {
            var points = string.Join(" ", Vertices.Select(p => p.ToString()));
            return $"<polygon class=\"{className}\" points=\"{points}\" {extra} />";
        }
    }
}
