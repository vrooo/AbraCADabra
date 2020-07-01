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

        public bool Draw { get; set; } = true;

        private PolyLine polyLine;

        public IntersectionCurveManager(IEnumerable<Vector3> points)
            : this(new PolyLine(points, new Vector4(0.9f, 0.1f, 0.1f, 1.0f), 2, true)) { }

        public IntersectionCurveManager(PolyLine polyLine) : base(polyLine)
        {
            this.polyLine = polyLine;
        }

        public override void Update() { }
        public override void Render(ShaderManager shader)
        {
            if (Draw)
            {
                GL.Disable(EnableCap.DepthTest);
                base.Render(shader);
                GL.Enable(EnableCap.DepthTest);
            }
        }

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
