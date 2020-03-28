using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    class Bezier3C2 : Bezier3
    {
        public Bezier3C2(IEnumerable<Vector3> points) : base(points) { }

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

        protected override void CalculateVertices(IEnumerable<Vector3> points)
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
    }
}
