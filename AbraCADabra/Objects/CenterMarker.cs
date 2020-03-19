using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    public class CenterMarker : Transform
    {
        private float pointSize = 8.0f;
        public bool Visible { get; set; } = false;

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

        public CenterMarker()
        {
            primitiveType = PrimitiveType.Points;
            Color = new Vector4(0.8f, 0.0f, 0.0f, 1.0f);
            Initialize();
        }

        public override void Render(Shader shader)
        {
            if (Visible)
            {
                GL.PointSize(pointSize);
                base.Render(shader);
                GL.PointSize(1.0f);
            }
        }
    }
}
