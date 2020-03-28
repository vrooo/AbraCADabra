using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    abstract class Bezier3 : Transform<AdjacencyVertex>
    {
        protected List<AdjacencyVertex> vertexList = new List<AdjacencyVertex>();
        protected override AdjacencyVertex[] vertices => vertexList.ToArray();
        protected List<uint> indexList = new List<uint>();
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

        public void Update(IEnumerable<Vector3> points)
        {
            CalculateVertices(points);
            CreateBuffers();
        }

        protected abstract void CalculateVertices(IEnumerable<Vector3> points);

        protected void AddVertex(Vector3 point, bool valid = true)
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
