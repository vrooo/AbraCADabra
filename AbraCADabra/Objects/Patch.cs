using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;

namespace AbraCADabra
{
    public class Patch : Transform<PatchVertex>
    {
        private static Random colorRandom = new Random();

        private PatchVertex[] vertexArray;
        protected override PatchVertex[] vertices => vertexArray;
        private uint[] indexArray;
        protected override uint[] indices => indexArray;

        private int patchCountX;
        private int patchCountZ;

        public Patch(int patchesCountX, int patchesCountZ, int divX, int divZ)
        {
            primitiveType = PrimitiveType.Lines;
            //Color = new Vector4(0.5f, 0.95f, 0.4f, 1.0f);
            double minColor = 0.1, colorMult = 1 - minColor;
            float r = (float)(colorRandom.NextDouble() * colorMult + minColor);
            float g = (float)(colorRandom.NextDouble() * colorMult + minColor);
            float b = (float)(colorRandom.NextDouble() * colorMult + minColor);
            Color = new Vector4(r, g, b, 1.0f);

            patchCountX = patchesCountX;
            patchCountZ = patchesCountZ;

            CalculateVertices(divX, divZ);
            Initialize();
        }

        protected override void SetVertexAttribPointer()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, PatchVertex.Size, PatchVertex.OffsetUV);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribIPointer(1, 1, VertexAttribIntegerType.Int, PatchVertex.Size, (IntPtr)PatchVertex.OffsetIndexX);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribIPointer(2, 1, VertexAttribIntegerType.Int, PatchVertex.Size, (IntPtr)PatchVertex.OffsetIndexZ);
        }

        public void Update(int divX, int divZ)
        {
            CalculateVertices(divX, divZ);
            CreateBuffers();
        }

        private void CalculateVertices(int divX, int divZ)
        {
            var vertexList = new List<PatchVertex>();
            var indexList = new List<uint>();

            int width = divX + 1; // for now
            int height = divZ + 1; // for now
            Func<int, int, int, int, uint> ind = (i, j, px, pz)
                => (uint)(((pz * patchCountX + px) * width + i) * height + j);

            float stepX = 1.0f / divX, stepZ = 1.0f / divZ;
            for (int px = 0; px < patchCountX; px++)
            {
                for (int pz = 0; pz < patchCountZ; pz++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            vertexList.Add(new PatchVertex(i * stepX, j * stepZ, px, pz));
                            if (i < width - 1)
                            {
                                indexList.AddMany(ind(i, j, px, pz), ind(i + 1, j, px, pz));
                            }
                            if (j < height - 1)
                            {
                                indexList.AddMany(ind(i, j, px, pz), ind(i, j + 1, px, pz));
                            }
                        }
                    }
                }
            }

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
