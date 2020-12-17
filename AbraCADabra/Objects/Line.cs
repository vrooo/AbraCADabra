using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AbraCADabra
{
    public class Line : FloatTransform
    {
        private float[] _vertices =
        {
            0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f
        };
        protected override float[] vertices => _vertices;

        private uint[] _indices = {
             0, 1
        };
        protected override uint[] indices => _indices;

        public Line(Vector4 color)
        {
            primitiveType = PrimitiveType.Lines;
            Color = color;
            Initialize();
        }
    }

    public class CustomLine : FloatTransform
    {
        private float[] _vertices;
        protected override float[] vertices => _vertices;

        private uint[] _indices = {
             0, 1
        };
        protected override uint[] indices => _indices;

        public CustomLine(Vector3 p1, Vector3 p2, Vector4 color)
        {
            primitiveType = PrimitiveType.Lines;
            _vertices = new float[] { p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z };
            Color = color;
            Initialize();
        }
    }
}
