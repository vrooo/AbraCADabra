using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Linq;

namespace AbraCADabra
{
    public class PolyLine : FloatTransform
    {
        private List<float> vertexList = new List<float>();
        protected override float[] vertices => vertexList.ToArray();
        private List<uint> indexList = new List<uint>();
        protected override uint[] indices => indexList.ToArray();

        public PolyLine(IEnumerable<Vector3> points, Vector4 color)
        {
            primitiveType = PrimitiveType.LineStrip;
            Color = color;

            CalculateVertices(points);
            Initialize();
        }

        public void Update(IEnumerable<Vector3> points)
        {
            CalculateVertices(points);
            CreateBuffers();
        }

        private void CalculateVertices(IEnumerable<Vector3> points)
        {
            vertexList.Clear();
            indexList.Clear();

            uint i = 0;
            Vector3 position = new Vector3();
            foreach (var point in points)
            {
                vertexList.Add(point.X);
                vertexList.Add(point.Y);
                vertexList.Add(point.Z);
                indexList.Add(i);
                i++;
                position += point;
            }
            Position = position / (i > 0 ? i : 1);
        }

        public override Matrix4 GetModelMatrix()
        {
            return Matrix4.Identity;
        }
        public override void Translate(float x, float y, float z) { }
        public override void Rotate(float x, float y, float z) { }
        public override void ScaleUniform(float delta) { }
    }
}
