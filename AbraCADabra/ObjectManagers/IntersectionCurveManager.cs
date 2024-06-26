﻿using AbraCADabra.Serialization;
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
    public enum EdgeType
    {
        Left, Top, Right, Bottom
    }

    public class IntersectionCurveManager : FloatTransformManager
    {
        private struct CurvePointData
        {
            public int SegmentIndex;
            public int LineIndex;
            public uint Index;
            public float LineParameter;
            public CurvePointData(int segmentIndex, int lineIndex, uint index, float lineParam)
            {
                SegmentIndex = segmentIndex;
                LineIndex = lineIndex;
                Index = index;
                LineParameter = lineParam;
            }
        }

        public override string DefaultName => "Intersection Curve";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public IEnumerable<Vector3> Points { get; }

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
            Points = points;
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
            if (surface == P && TrimModeP != TrimMode.None && IsTrimmableP)
            {
                Trim(surface, vertices, indices, PolygonsP, SegmentsP, TrimModeP, IsClosedP);
            }
            if (surface == Q && TrimModeQ != TrimMode.None && IsTrimmableQ)
            {
                Trim(surface, vertices, indices, PolygonsQ, SegmentsQ, TrimModeQ, IsClosedQ);
            }
        }

        public bool IsPointInside(ISurface surface, float u, float v)
        {
            List<List<Vector2>> polygons = new List<List<Vector2>>();
            TrimMode trimMode = TrimMode.None;
            if (surface == P && IsTrimmableP)
            {
                polygons = PolygonsP;
                trimMode = TrimModeP;
            }
            if (surface == Q && IsTrimmableQ)
            {
                polygons = PolygonsQ;
                trimMode = TrimModeQ;
            }
            if (trimMode == TrimMode.None)
            {
                return false;
            }

            var uv = new Vector2(u, v);
            uv.X /= surface.UScale;
            uv.Y /= surface.VScale;
            bool isInside = trimMode != TrimMode.SideA;
            foreach (var polygon in polygons)
            {
                if (MathHelper.IsPointInPolygon(uv, polygon))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        private void Trim(ISurface surface, List<Vector2> vertices, List<uint> indices,
                          List<List<Vector2>> polygons, List<List<Vector2>> segments, TrimMode trimMode, bool isClosed)
        {
            Vector2 scaleVector = new Vector2(surface.UScale, surface.VScale);
            int n = vertices.Count;
            bool[] isInside = new bool[n];
            for (int i = 0; i < n; i++)
            {
                isInside[i] = trimMode == TrimMode.SideA ? false : true;
                foreach (var polygon in polygons)
                {
                    Vector2 point = vertices[i];
                    point.X /= scaleVector.X;
                    point.Y /= scaleVector.Y;
                    if (MathHelper.IsPointInPolygon(point, polygon))
                    {
                        isInside[i] = !isInside[i];
                    }
                }
            }
            var borderPoints = new List<CurvePointData>();
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
                    Vector2 inner = vertices[(int)indices[inside]], outer = vertices[(int)indices[outside]];
                    inner.X /= scaleVector.X;
                    inner.Y /= scaleVector.Y;
                    outer.X /= scaleVector.X;
                    outer.Y /= scaleVector.Y;
                    bool found = false;
                    for (int j = 0; j < segments.Count; j++)
                    {
                        var segment = segments[j];
                        for (int k = 0; k < segment.Count - (isClosed ? 0 : 1); k++)
                        {
                            if (MathHelper.HasIntersection(inner, outer, segment[k], segment[(k + 1) % segment.Count]))
                            {
                                Vector2 intersection = MathHelper.GetIntersection(inner, outer, segment[k], segment[(k + 1) % segment.Count], out float s);
                                intersection *= scaleVector;
                                vertices.Add(intersection);
                                uint newIndex = (uint)(vertices.Count - 1);
                                indices[inside] = newIndex;

                                borderPoints.Add(new CurvePointData(j, k, newIndex, s));
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }
            }

            // connect border
            borderPoints.Sort((a, b) => {
                if (a.SegmentIndex == b.SegmentIndex)
                {
                    if (a.LineIndex == b.LineIndex)
                    {
                        return a.LineParameter.CompareTo(b.LineParameter);
                    }
                    return a.LineIndex.CompareTo(b.LineIndex);
                }
                return a.SegmentIndex.CompareTo(b.SegmentIndex);
            });
            for (int i = 0; i < borderPoints.Count - (IsLoop ? 0 : 1); i++)
            {
                indices.Add(borderPoints[i].Index);
                indices.Add(borderPoints[(i + 1) % borderPoints.Count].Index);
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

            List<List<Vector2>> MakePolygons(ISurface surface, List<List<Vector2>> segments)
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
                        Vector2 point = last;
                        EdgeType firstEdge = GetEdgeType(first), curEdge = GetEdgeType(point);
                        if (surface.IsEdgeAllowed(firstEdge, curEdge))
                        {
                            var polygon = new List<Vector2>(segment);
                            polygon.Add(MoveAway(last));
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
                }
                return polygons;
            }
            PolygonsP = MakePolygons(P, SegmentsP);
            PolygonsQ = MakePolygons(Q, SegmentsQ);
        }

        public (List<PointManager> points, Bezier3InterManager curve) ToBezierInter()
        {
            var pms = new List<PointManager>();
            foreach (var point in Points)
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
