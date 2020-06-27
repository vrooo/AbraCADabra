using AbraCADabra.Serialization;
using OpenTK;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class IntersectionCurveManager : FloatTransformManager
    {
        public override string DefaultName => "Intersection Curve";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public bool Draw { get; set; } = false;

        private PolyLine polyLine;

        public IntersectionCurveManager(IEnumerable<Vector3> points)
            : this(new PolyLine(points, new Vector4(0.9f, 0.1f, 0.1f, 1.0f), 2)) { } // TODO: true param

        public IntersectionCurveManager(PolyLine polyLine) : base(polyLine)
        {
            this.polyLine = polyLine;
        }

        public override void Update() { }
        public override void Render(ShaderManager shader)
        {
            if (Draw)
            {
                base.Render(shader);
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
