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
}
