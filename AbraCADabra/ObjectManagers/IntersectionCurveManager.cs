using AbraCADabra.Serialization;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class IntersectionCurveManager : FloatTransformManager
    {
        public override string DefaultName => "Intersection Curve";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        private IEnumerable<Vector3> points;
        private IList<Vector4> xs;

        public ISurface P { get; }
        public ISurface Q { get; }
        // scaled to [0; 1]
        public List<List<Vector2>> SegmentsP { get; private set; }
        public List<List<Vector2>> SegmentsQ { get; private set; }
        public bool IsLoop { get; }
        public bool IsClosedP { get; private set; }
        public bool IsClosedQ { get; private set; }

        public bool Draw { get; set; } = true;

        private PolyLine polyLine;

        public IntersectionCurveManager(ISurface p, ISurface q, IEnumerable<Vector3> points, IList<Vector4> xs,
                                        bool loop, bool startEdge = false, bool endEdge = false) // TODO: remove defaults
            : this(new PolyLine(points, new Vector4(0.9f, 0.1f, 0.1f, 1.0f), 2, loop))
        {
            IsLoop = loop;
            P = p;
            Q = q;
            this.points = points;
            this.xs = xs;
            CalculateSegments();
        }

        private IntersectionCurveManager(PolyLine polyLine) : base(polyLine)
        {
            this.polyLine = polyLine;
        }

        private void CalculateSegments()
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
                    if ((int)Math.Floor(x.X) != (int)Math.Floor(prevX.X))
                    {
                        double t = (Math.Max(Math.Floor(x.X), Math.Floor(prevX.X)) - x.X) / (prevX.X - x.X);
                        var midVec = P.ClampScaledUV(0, (float)(x.Y + t * (prevX.Y - x.Y)));
                        var midPointZero = new Vector2(0, midVec.Y);
                        var midPointOne  = new Vector2(1, midVec.Y);
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
                    if ((int)Math.Floor(x.Y) != (int)Math.Floor(prevX.Y))
                    {
                        double t = (Math.Max(Math.Floor(x.Y), Math.Floor(prevX.Y)) - x.Y) / (prevX.Y - x.Y);
                        var midVec = P.ClampScaledUV((float)(x.X + t * (prevX.X - x.X)), 0);
                        var midPointZero = new Vector2(midVec.X, 0);
                        var midPointOne  = new Vector2(midVec.X, 1);
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
                    if ((int)Math.Floor(x.Z) != (int)Math.Floor(prevX.Z))
                    {
                        double t = (Math.Max(Math.Floor(x.Z), Math.Floor(prevX.Z)) - x.Z) / (prevX.Z - x.Z);
                        var midVec = Q.ClampScaledUV(0, (float)(x.W + t * (prevX.W - x.W)));
                        var midPointZero = new Vector2(0, midVec.Y);
                        var midPointOne  = new Vector2(1, midVec.Y);
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
                    if ((int)Math.Floor(x.W) != (int)Math.Floor(prevX.W))
                    {
                        double t = (Math.Max(Math.Floor(x.W), Math.Floor(prevX.W)) - x.W) / (prevX.W - x.W);
                        var midVec = Q.ClampScaledUV((float)(x.Z + t * (prevX.Z - x.Z)), 0);
                        var midPointZero = new Vector2(midVec.X, 0);
                        var midPointOne  = new Vector2(midVec.X, 1);
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
                bool IsZeroOne(float x1, float x2)
                {
                    return (x1 == 0 && x2 == 1) || (x2 == 0 && x1 == 1);
                }
                bool ConnectFirstLast(List<List<Vector2>> segments)
                {
                    if (segments.Count > 1)
                    {
                        int segLast = segments.Count - 1, pointLast = segments[segLast].Count - 1;
                        Vector2 first = segments[0][0], last = segments[segLast][pointLast];
                        if (Vector2.DistanceSquared(first, last) < maxDistSq)
                        {
                            segments[segLast].AddRange(segments[0]);
                            segments.RemoveAt(0);
                        }
                    }
                    if (segments.Count == 1 && segments[0].Count > 1)
                    {
                        int pointLast = segments[0].Count - 1;
                        Vector2 first = segments[0][0], last = segments[0][pointLast];
                        if (!IsZeroOne(first.X, last.X) && !IsZeroOne(first.Y, last.Y))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                IsClosedP = ConnectFirstLast(SegmentsP);
                IsClosedQ = ConnectFirstLast(SegmentsQ);
            }
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
