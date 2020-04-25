using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    class Arrow : FloatTransform
    {
        private float arrowRadius = 0.1f;
        private float arrowHeight = 0.4f;
        private Line line;
        private List<float> _vertices = new List<float>();
        protected override float[] vertices => _vertices.ToArray();

        private uint[] _indices = {
             0, 1, 2, 3, 4, 5
        };
        protected override uint[] indices => _indices;

        public Arrow(Vector4 color)
        {
            primitiveType = PrimitiveType.TriangleFan;
            line = new Line(color);
            Color = color;
            CalculateVertices();
            Initialize();
        }

        private void CalculateVertices()
        {
            _vertices.AddMany(0.0f, 1.0f, 0.0f);
            _vertices.AddMany(-arrowRadius, 1.0f - arrowHeight, -arrowRadius);
            _vertices.AddMany(+arrowRadius, 1.0f - arrowHeight, -arrowRadius);
            _vertices.AddMany(+arrowRadius, 1.0f - arrowHeight, +arrowRadius);
            _vertices.AddMany(-arrowRadius, 1.0f - arrowHeight, +arrowRadius);
            _vertices.AddMany(-arrowRadius, 1.0f - arrowHeight, -arrowRadius);
        }

        public override void Render(ShaderManager shader)
        {
            line.Position = Position;
            line.Rotation = Rotation;
            line.Scale = Scale;
            line.Render(shader);
            base.Render(shader);
        }
    }
}
