using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;

namespace AbraCADabra
{
    public class QuadPatch : PatchTransform
    {
        private PatchVertex[] vertexArray;
        protected override PatchVertex[] vertices => vertexArray;
        private uint[] indexArray;
        protected override uint[] indices => indexArray;

        private int patchCountX;
        private int patchCountZ;

        public QuadPatch(int patchesCountX, int patchesCountZ, int divX, int divZ)
        {
            primitiveType = PrimitiveType.Quads;
            Color = new Vector4(0.5f, 0.95f, 0.4f, 1.0f);

            patchCountX = patchesCountX;
            patchCountZ = patchesCountZ;

            CalculateVertices(divX, divZ);
            Initialize();
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
                            if (i < width - 1 && j < height - 1)
                            {
                                indexList.AddMany(ind(i, j, px, pz),
                                                  ind(i + 1, j, px, pz),
                                                  ind(i + 1, j + 1, px, pz),
                                                  ind(i, j + 1, px, pz));
                            }
                        }
                    }
                }
            }

            vertexArray = vertexList.ToArray();
            indexArray = indexList.ToArray();
        }
    }
}
