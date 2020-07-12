using AbraCADabra.Serialization;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public enum TrimMode
    {
        None, SideA, SideB
    }

    public class IntersectionCurveManager : FloatTransformManager
    {
        private enum EdgeType
        {
            Left, Top, Right, Bottom
        }
        public override string DefaultName => "Intersection Curve";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        private IEnumerable<Vector3> points;

        public ISurface P { get; }
        public ISurface Q { get; }
        // scaled to [0; 1]
        public List<List<Vector2>> SegmentsP { get; private set; }
        public List<List<Vector2>> SegmentsQ { get; private set; }
        public List<List<Vector2>> PolygonsP { get; private set; }
        public List<List<Vector2>> PolygonsQ { get; private set; }
        public bool IsLoop { get; }
        public bool IsClosedP { get; private set; }
        public bool IsClosedQ { get; private set; }
        public bool IsTrimmableP => PolygonsP.Count > 0;
        public bool IsTrimmableQ => PolygonsQ.Count > 0;
        public TrimMode TrimModeP { get; set; } = TrimMode.None;
        public TrimMode TrimModeQ { get; set; } = TrimMode.None;

        public bool Draw { get; set; } = true;

        private PolyLine polyLine;

        public IntersectionCurveManager(ISurface p, ISurface q, IEnumerable<Vector3> points, IList<Vector4> xs, bool loop)
            : this(new PolyLine(points, new Vector4(0.9f, 0.1f, 0.1f, 1.0f), 2, loop))
        {
            IsLoop = loop;
            P = p;
            Q = q;
            this.points = points;
            CalculateSegments(xs);
            CalculatePolygons();

            P.AddIntersectionCurve(this);
            Q.AddIntersectionCurve(this);
        }

        private IntersectionCurveManager(PolyLine polyLine) : base(polyLine)
        {
            this.polyLine = polyLine;
        }

        public void Trim(ISurface surface, List<Vector2> vertices, List<uint> indices)
        {
            List<List<Vector2>> polygons = null, segments = null;
            TrimMode trimMode = TrimMode.None;
            bool isClosed = false;
            if (surface == P && TrimModeP != TrimMode.None && IsTrimmableP)
            {
                polygons = PolygonsP;
                segments = SegmentsP;
                trimMode = TrimModeP;
                isClosed = IsClosedP;
            }
            else if (surface == Q && TrimModeQ != TrimMode.None && IsTrimmableQ)
            {
                polygons = PolygonsQ;
                segments = SegmentsQ;
                trimMode = TrimModeQ;
                isClosed = IsClosedQ;
            }
            if (segments != null)
            {
                int n = vertices.Count;
                bool[] isInside = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    isInside[i] = trimMode == TrimMode.SideA ? false : true;
                    foreach (var polygon in polygons)
                    {
                        if (MathHelper.IsPointInPolygon(vertices[i], polygon))
                        {
                            isInside[i] = !isInside[i];
                        }
                    }
                }
                for (int i = 0; i < indices.Count / 2; i++)
                {
                    bool crosses = false;
                    int inside = 0, outside = 0;
                    bool isFirstIn = isInside[indices[2 * i]], isSecondIn = isInside[indices[2 * i + 1]];
                    if (isFirstIn && !isSecondIn)
                    {
                        crosses = true;
                        inside = 2 * i;
                        outside = 2 * i + 1;
                    }
                    else if (!isFirstIn && isSecondIn)
                    {
                        crosses = true;
                        inside = 2 * i + 1;
                        outside = 2 * i;
                    }
                    else if (isFirstIn && isSecondIn)
                    {
                        indices.RemoveAt(2 * i + 1);
                        indices.RemoveAt(2 * i);
                        i--;
                    }
                    if (crosses)
                    {
                        Vector2 scaleVector = new Vector2(surface.UScale, surface.VScale);
                        Vector2 inner = vertices[(int)indices[inside]], outer = vertices[(int)indices[outside]];
                        inner.X /= scaleVector.X;
                        inner.Y /= scaleVector.Y;
                        outer.X /= scaleVector.X;
                        outer.Y /= scaleVector.Y;
                        bool found = false;
                        foreach (var segment in segments)
                        {
                            for (int j = 0; j < segment.Count - (isClosed ? 0 : 1); j++)
                            {
                                if (MathHelper.HasIntersection(inner, outer, segment[j], segment[(j + 1) % segment.Count]))
                                {
                                    Vector2 intersection = MathHelper.GetIntersection(inner, outer, segment[j], segment[(j + 1) % segment.Count]);
                                    intersection *= scaleVector;
                                    vertices.Add(intersection);
                                    indices[inside] = (uint)(vertices.Count - 1);
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                    }
                }
            }
        }

        private void CalculateSegments(IList<Vector4> xs)
        {
            SegmentsP = new List<List<Vector2>>();
            SegmentsQ = new List<List<Vector2>>();
            Vector4 prevX = new Vector4();
            for (int i = 0; i < xs.Count; i++)
            {
                var x = new Vector4(xs[i].X / P.UScale, xs[i].Y / P.VScale, xs[i].Z / Q.UScale, xs[i].W / Q.VScale);
                var pointP = P.ClampScaledUV(x.X, x.Y);
                var pointQ = Q.ClampScaledUV(x.Z, x.W);

                if (i == 0)
                {
                    SegmentsP.Add(new List<Vector2> { pointP });
                    SegmentsQ.Add(new List<Vector2> { pointQ });
                }
                else
                {
                    if ((int)Math.Floor(x.X) != (int)Math.Floor(prevX.X) && x.X != 1 && prevX.X != 1)
                    {
                        var midPoint = MathHelper.FindPointAtX(prevX.Xy, x.Xy, Math.Max(Math.Floor(x.X), Math.Floor(prevX.X)), out _);
                        midPoint = P.ClampScaledUV(0, midPoint.Y);
                        var midPointZero = new Vector2(0, midPoint.Y);
                        var midPointOne  = new Vector2(1, midPoint.Y);
                        if (x.X > prevX.X)
                        {
                            SegmentsP[SegmentsP.Count - 1].Add(midPointOne);
                            SegmentsP.Add(new List<Vector2> { midPointZero });
                        }
                        else
                        {
                            SegmentsP[SegmentsP.Count - 1].Add(midPointZero);
                            SegmentsP.Add(new List<Vector2> { midPointOne });
                        }
                    }
                    if ((int)Math.Floor(x.Y) != (int)Math.Floor(prevX.Y) && x.Y != 1 && prevX.Y != 1)
                    {
                        var midPoint = MathHelper.FindPointAtY(prevX.Xy, x.Xy, Math.Max(Math.Floor(x.Y), Math.Floor(prevX.Y)), out _);
                        midPoint = P.ClampScaledUV(midPoint.X, 0);
                        var midPointZero = new Vector2(midPoint.X, 0);
                        var midPointOne  = new Vector2(midPoint.X, 1);
                        if (x.Y > prevX.Y)
                        {
                            SegmentsP[SegmentsP.Count - 1].Add(midPointOne);
                            SegmentsP.Add(new List<Vector2> { midPointZero });
                        }
                        else
                        {
                            SegmentsP[SegmentsP.Count - 1].Add(midPointZero);
                            SegmentsP.Add(new List<Vector2> { midPointOne });
                        }
                    }
                    if ((int)Math.Floor(x.Z) != (int)Math.Floor(prevX.Z) && x.Z != 1 && prevX.Z != 1)
                    {
                        var midPoint = MathHelper.FindPointAtX(prevX.Zw, x.Zw, Math.Max(Math.Floor(x.Z), Math.Floor(prevX.Z)), out _);
                        midPoint = Q.ClampScaledUV(0, midPoint.Y);
                        var midPointZero = new Vector2(0, midPoint.Y);
                        var midPointOne  = new Vector2(1, midPoint.Y);
                        if (x.Z > prevX.Z)
                        {
                            SegmentsQ[SegmentsQ.Count - 1].Add(midPointOne);
                            SegmentsQ.Add(new List<Vector2> { midPointZero });
                        }
                        else
                        {
                            SegmentsQ[SegmentsQ.Count - 1].Add(midPointZero);
                            SegmentsQ.Add(new List<Vector2> { midPointOne });
                        }
                    }
                    if ((int)Math.Floor(x.W) != (int)Math.Floor(prevX.W) && x.W != 1 && prevX.W != 1)
                    {
                        var midPoint = MathHelper.FindPointAtY(prevX.Zw, x.Zw, Math.Max(Math.Floor(x.W), Math.Floor(prevX.W)), out _);
                        midPoint = Q.ClampScaledUV(midPoint.X, 0);
                        var midPointZero = new Vector2(midPoint.X, 0);
                        var midPointOne  = new Vector2(midPoint.X, 1);
                        if (x.W > prevX.W)
                        {
                            SegmentsQ[SegmentsQ.Count - 1].Add(midPointOne);
                            SegmentsQ.Add(new List<Vector2> { midPointZero });
                        }
                        else
                        {
                            SegmentsQ[SegmentsQ.Count - 1].Add(midPointZero);
                            SegmentsQ.Add(new List<Vector2> { midPointOne });
                        }
                    }
                    SegmentsP[SegmentsP.Count - 1].Add(pointP);
                    SegmentsQ[SegmentsQ.Count - 1].Add(pointQ);
                }
                prevX = x;
            }
            if (IsLoop)
            {
                double maxDistSq = 0.25;
                bool ConnectFirstLast(List<List<Vector2>> segments)
                {
                    int segLast = segments.Count - 1, pointLast = segments[segLast].Count - 1;
                    Vector2 first = segments[0][0], last = segments[segLast][pointLast];
                    if (Vector2.DistanceSquared(first, last) < maxDistSq)
                    {
                        if (segments.Count > 1)
                        {
                            segments[segLast].AddRange(segments[0]);
                            segments.RemoveAt(0);
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else if (!IsOnEdge(first) || !IsOnEdge(last))
                    {
                        if (Vector2.DistanceSquared(first - Vector2.UnitX, last) < maxDistSq)
                        {
                            if (first.X == 1)
                            {
                                segments[segLast].Add(new Vector2(0, first.Y));
                            }
                            else if (last.X == 0)
                            {
                                segments[0].Insert(0, new Vector2(1, last.Y));
                            }
                            else
                            {
                                var midPoint = MathHelper.FindPointAtX(first - Vector2.UnitX, last, 0, out _);
                                var midPointZero = new Vector2(0, midPoint.Y);
                                var midPointOne = new Vector2(1, midPoint.Y);
                                segments[0].Insert(0, midPointOne);
                                segments[segLast].Add(midPointZero);
                            }
                        }
                        else if (Vector2.DistanceSquared(first, last - Vector2.UnitX) < maxDistSq)
                        {
                            if (first.X == 0)
                            {
                                segments[segLast].Add(new Vector2(1, first.Y));
                            }
                            else if (last.X == 1)
                            {
                                segments[0].Insert(0, new Vector2(0, last.Y));
                            }
                            else
                            {
                                var midPoint = MathHelper.FindPointAtX(first, last - Vector2.UnitX, 0, out _);
                                var midPointZero = new Vector2(0, midPoint.Y);
                                var midPointOne = new Vector2(1, midPoint.Y);
                                segments[0].Insert(0, midPointZero);
                                segments[segLast].Add(midPointOne);
                            }
                        }
                        else if (Vector2.DistanceSquared(first - Vector2.UnitY, last) < maxDistSq)
                        {
                            if (first.Y == 1)
                            {
                                segments[segLast].Add(new Vector2(first.X, 0));
                            }
                            else if (last.Y == 0)
                            {
                                segments[0].Insert(0, new Vector2(last.X, 1));
                            }
                            else
                            {
                                var midPoint = MathHelper.FindPointAtY(first - Vector2.UnitY, last, 0, out _);
                                var midPointZero = new Vector2(midPoint.X, 0);
                                var midPointOne = new Vector2(midPoint.X, 1);
                                segments[0].Insert(0, midPointOne);
                                segments[segLast].Add(midPointZero);
                            }
                        }
                        else if (Vector2.DistanceSquared(first, last - Vector2.UnitY) < maxDistSq)
                        {
                            if (first.Y == 0)
                            {
                                segments[segLast].Add(new Vector2(first.X, 1));
                            }
                            else if (last.Y == 1)
                            {
                                segments[0].Insert(0, new Vector2(last.X, 0));
                            }
                            else
                            {
                                var midPoint = MathHelper.FindPointAtY(first, last - Vector2.UnitY, 0, out _);
                                var midPointZero = new Vector2(midPoint.X, 0);
                                var midPointOne = new Vector2(midPoint.X, 1);
                                segments[0].Insert(0, midPointZero);
                                segments[segLast].Add(midPointOne);
                            }
                        }
                    }
                    return false;
                }
                IsClosedP = ConnectFirstLast(SegmentsP);
                IsClosedQ = ConnectFirstLast(SegmentsQ);
            }
        }

        private bool IsOnEdge(Vector2 uv)
        {
            return uv.X == 0 || uv.X == 1 || uv.Y == 0 || uv.Y == 1;
        }

        private void CalculatePolygons()
        {
            // left:   [00; 01)
            // top:    [01; 11)
            // right:  [11; 10)
            // bottom: [10; 00)
            EdgeType GetEdgeType(Vector2 point)
            {
                if (point.X == 0 && point.Y != 1) return EdgeType.Left;
                if (point.X != 1 && point.Y == 1) return EdgeType.Top;
                if (point.X == 1 && point.Y != 0) return EdgeType.Right;
                return EdgeType.Bottom;
            }

            Vector2 GetNextCorner(EdgeType edgeType)
            {
                switch (edgeType)
                {
                    case EdgeType.Left:
                        return new Vector2(0, 1);
                    case EdgeType.Top:
                        return new Vector2(1, 1);
                    case EdgeType.Right:
                        return new Vector2(1, 0);
                    default:
                        return new Vector2(0, 0);
                }
            }

            Vector2 MoveAway(Vector2 point)
            {
                float shift = 0.001f;
                for (int i = 0; i < 2; i++)
                {
                    if (point[i] == 0) point[i] -= shift;
                    else if (point[i] == 1) point[i] += shift;
                }
                return point;
            }

            List<List<Vector2>> MakePolygons(List<List<Vector2>> segments)
            {
                var polygons = new List<List<Vector2>>();
                if (segments.Count == 1 && IsLoop && !IsOnEdge(segments[0][0])) // only case in which segment ends don't have to be on edges
                {
                    polygons.Add(new List<Vector2>(segments[0]));
                }
                else
                {
                    foreach (var segment in segments)
                    {
                        Vector2 first = segment[0], last = segment[segment.Count - 1];
                        if (!IsOnEdge(first) || !IsOnEdge(last))
                        {
                            return new List<List<Vector2>>();
                        }
                        var polygon = new List<Vector2>(segment);
                        polygon.Add(MoveAway(last));
                        Vector2 point = last;
                        EdgeType firstEdge = GetEdgeType(first), curEdge = GetEdgeType(point);
                        while (curEdge != firstEdge)
                        {
                            point = GetNextCorner(curEdge);
                            curEdge = GetEdgeType(point);
                            polygon.Add(MoveAway(point));
                        }
                        polygon.Add(MoveAway(first));
                        polygons.Add(polygon);
                    }
                }
                return polygons;
            }
            PolygonsP = MakePolygons(SegmentsP);
            PolygonsQ = MakePolygons(SegmentsQ);
        }

        public (List<PointManager> points, Bezier3InterManager curve) ToBezierInter()
        {
            var pms = new List<PointManager>();
            foreach (var point in points)
            {
                pms.Add(new PointManager(point));
            }
            if (IsLoop && pms.Count > 1)
            {
                pms.Add(pms[0]);
            }
            return (pms, new Bezier3InterManager(pms));
        }

        public override void Render(ShaderManager shader)
        {
            if (Draw)
            {
                GL.Disable(EnableCap.DepthTest);
                base.Render(shader);
                GL.Enable(EnableCap.DepthTest);
            }
        }
        public override void Update() { }

        public override void Translate(float x, float y, float z) { }
        public override void Rotate(float x, float y, float z) { }
        public override void RotateAround(float xAngle, float yAngle, float zAngle, Vector3 center) { }
        public override void ScaleUniform(float delta) { }
        public override XmlNamedType GetSerializable()
        {
            return null;
        }
    }
}
