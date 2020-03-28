using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    public class CrossCursor : FloatTransform
    {
        private float[] _vertices =
        {
            +1.0f, +0.0f, +0.0f,
            -1.0f, +0.0f, +0.0f,
            +0.0f, +1.0f, +0.0f,
            +0.0f, -1.0f, +0.0f,
            +0.0f, +0.0f, +1.0f,
            +0.0f, +0.0f, -1.0f
        };
        protected override float[] vertices => _vertices;

        private uint[] _indices =
        {
            0, 1,
            2, 3,
            4, 5
        };
        protected override uint[] indices => _indices;

        public CrossCursor()
        {
            primitiveType = PrimitiveType.Lines;
            Color = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            Initialize();
        }

        public override void Rotate(float x, float y, float z) { }
        public override void ScaleUniform(float delta) { }
    }
}
