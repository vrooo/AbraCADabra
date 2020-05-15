using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    public class Bezier3 : Transform<AdjacencyVertex>
    {
        private List<AdjacencyVertex> vertexList = new List<AdjacencyVertex>();
        protected override AdjacencyVertex[] vertices => vertexList.ToArray();
        private List<uint> indexList = new List<uint>();
        protected override uint[] indices => indexList.ToArray();

        public Bezier3(IEnumerable<Vector3> points)
        {
            primitiveType = PrimitiveType.LineStripAdjacencyExt;
            Color = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);

            CalculateVertices(points);
            Initialize();
        }

        protected override void SetVertexAttribPointer()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, AdjacencyVertex.Size, AdjacencyVertex.OffsetPoint);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Int, false, AdjacencyVertex.Size, AdjacencyVertex.OffsetValid);
        }

        public override void Render(ShaderManager shader)
        {
            shader.SetupTransform(Color, GetModelMatrix());
            GL.BindVertexArray(vao);
            for (int start = 1; start < indices.Length; start += 3)
            {
                GL.DrawArrays(primitiveType, start, 4);
            }
            GL.BindVertexArray(0);
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
            Vector3 last = new Vector3();
            foreach (var point in points)
            {
                if (i == 0)
                {
                    AddVertex(point, false);
                }
                AddVertex(point);
                indexList.Add(i);
                i++;
                position += point;
                last = point;
            }
            if (i > 0)
            {
                AddVertex(last, false);
                AddVertex(last, false);
            }
            Position = position / (i > 0 ? i : 1);
        }

        private void AddVertex(Vector3 point, bool valid = true)
        {
            vertexList.Add(new AdjacencyVertex(point, valid));
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
