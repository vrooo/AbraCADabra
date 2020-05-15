using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;

namespace AbraCADabra
{
    public class PolyGrid : FloatTransform
    {
        private float[] vertexArray;
        protected override float[] vertices => vertexArray;
        private uint[] indexArray;
        protected override uint[] indices => indexArray;

        public PolyGrid(Vector3[,] points, Vector4 color)
        {
            primitiveType = PrimitiveType.Lines;
            Color = color;

            CalculateVertices(points);
            Initialize();
        }

        public void Update(Vector3[,] points)
        {
            CalculateVertices(points);
            CreateBuffers();
        }

        private void CalculateVertices(Vector3[,] points)
        {
            var vertexList = new List<float>();
            var indexList = new List<uint>();

            Vector3 position = new Vector3();
            int width = points.GetLength(0), height = points.GetLength(1);
            Func<int, int, uint> ind = (i, j) => (uint)(i * height + j);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var point = points[i, j];
                    vertexList.AddMany(point.X, point.Y, point.Z);
                    if (i < width - 1)
                    {
                        indexList.AddMany(ind(i, j), ind(i + 1, j));
                    }
                    if (j < height - 1)
                    {
                        indexList.AddMany(ind(i, j), ind(i, j + 1));
                    }
                    position += point;
                }
            }

            Position = position / (vertexList.Count > 0 ? vertexList.Count / 3 : 1);
            vertexArray = vertexList.ToArray();
            indexArray = indexList.ToArray();
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
