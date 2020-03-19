using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    public class Point : Transform
    {
        private float pointSize = 15.0f;

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

        public Point(Vector3 position) : base(position)
        {
            primitiveType = PrimitiveType.Points;
            Color = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            Initialize();
        }

        public override void Render(Shader shader)
        {
            GL.PointSize(pointSize);
            base.Render(shader);
            GL.PointSize(1.0f);
        }

        public override bool TestHit(Camera camera, float width, float height, float x, float y, out float z)
        {
            var coords = GetScreenSpaceCoords(camera, width, height);
            if (x >= coords.X - pointSize / 2.0f && x <= coords.X + pointSize / 2.0f &&
                y >= coords.Y - pointSize / 2.0f && y <= coords.Y + pointSize / 2.0f)
            {
                z = coords.Z;
                return true;
            }
            z = 0.0f;
            return false;
        }
    }
}
