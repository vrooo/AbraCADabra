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
            : this(patchesCountX, patchesCountZ, divX, divZ, new Vector4(-1.0f)) { }

        public Patch(int patchesCountX, int patchesCountZ, int divX, int divZ, Vector4 color)
        {
            primitiveType = PrimitiveType.Lines;
            //Color = new Vector4(0.5f, 0.95f, 0.4f, 1.0f);
            if (color.W <= 0.0f)
            {
                double minColor = 0.1, colorMult = 1 - minColor;
                float r = (float)(colorRandom.NextDouble() * colorMult + minColor);
                float g = (float)(colorRandom.NextDouble() * colorMult + minColor);
                float b = (float)(colorRandom.NextDouble() * colorMult + minColor);
                Color = new Vector4(r, g, b, 1.0f);
            }
            else
            {
                Color = color;
            }

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

        public void Update(int divX, int divZ, ISurface parent, List<IntersectionCurveManager> curves)
        {
            CalculateVertices(divX, divZ, parent, curves);
            CreateBuffers();
        }

        private void CalculateVertices(int divX, int divZ, ISurface parent = null, List<IntersectionCurveManager> curves = null)
        {
            var vertexList = new List<PatchVertex>();
            var indexList = new List<uint>();
            var uvs = new List<Vector2>();

            int width = divZ + 1;
            int height = divX + 1;
            Func<int, int, int, int, uint> ind = (i, j, px, pz)
                => (uint)(((pz * patchCountX + px) * width + i) * height + j);

            float stepU = 1.0f / divZ, stepV = 1.0f / divX;
            for (int px = 0; px < patchCountX; px++)
            {
                for (int pz = 0; pz < patchCountZ; pz++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            uvs.Add(new Vector2(px + i * stepU, pz + j * stepV));
                            vertexList.Add(new PatchVertex(i * stepU, j * stepV, px, pz));
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

            if (curves != null)
            {
                foreach (var curve in curves)
                {
                    curve.Trim(parent, uvs, indexList);
                }
                for (int i = vertexList.Count; i < uvs.Count; i++)
                {
                    int indexX = (int)Math.Floor(uvs[i].X), indexZ = (int)Math.Floor(uvs[i].Y);
                    indexX = Math.Max(0, Math.Min(patchCountX - 1, indexX));
                    indexZ = Math.Max(0, Math.Min(patchCountZ - 1, indexZ));
                    float u = uvs[i].X - indexX;
                    float v = uvs[i].Y - indexZ;
                    vertexList.Add(new PatchVertex(u, v, indexX, indexZ));
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
