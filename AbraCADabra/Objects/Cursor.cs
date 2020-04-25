using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    public class Cursor : FloatTransform
    {
        private float pointSize = 10.0f;
        private float outlineThickness = 2.0f;

        private float[] _vertices =
        {
            0.0f, 0.0f, 0.0f
        };
        protected override float[] vertices => _vertices;

        private uint[] _indices =
        {
            0
        };
        protected override uint[] indices => _indices;

        public Vector4 OutlineColor { get; set; }

        public Cursor()
        {
            primitiveType = PrimitiveType.Points;
            Color = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
            OutlineColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            Initialize();
        }

        public override void Render(ShaderManager shader)
        {
            GL.Disable(EnableCap.DepthTest);

            GL.BindVertexArray(vao);

            shader.SetupTransform(OutlineColor, GetModelMatrix());
            GL.PointSize(pointSize + 2 * outlineThickness);
            GL.DrawElements(primitiveType, indices.Length, DrawElementsType.UnsignedInt, 0);

            shader.SetupColor(Color);
            GL.PointSize(pointSize);
            GL.DrawElements(primitiveType, indices.Length, DrawElementsType.UnsignedInt, 0);

            GL.PointSize(1.0f);
            GL.BindVertexArray(0);
            GL.Enable(EnableCap.DepthTest);
        }

        public override void Rotate(float x, float y, float z) { }
        public override void ScaleUniform(float delta) { }
    }
}
