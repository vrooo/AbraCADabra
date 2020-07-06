using AbraCADabra.Serialization;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class IntersectionCurveManager : FloatTransformManager
    {
        public override string DefaultName => "Intersection Curve";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        private IEnumerable<Vector3> points;
        public bool IsLoop { get; }
        public IList<Vector4> Xs { get; }
        public ISurface P { get; }
        public ISurface Q { get; }

        public bool Draw { get; set; } = true;

        private PolyLine polyLine;

        public IntersectionCurveManager(ISurface p, ISurface q, IEnumerable<Vector3> points, IList<Vector4> xs, bool loop)
            : this(new PolyLine(points, new Vector4(0.9f, 0.1f, 0.1f, 1.0f), 2, loop))
        {
            IsLoop = loop;
            P = p;
            Q = q;
            this.points = points;
            Xs = xs;
        }

        private IntersectionCurveManager(PolyLine polyLine) : base(polyLine)
        {
            this.polyLine = polyLine;
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
