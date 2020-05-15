using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;

namespace AbraCADabra
{
    public class Patch : Transform<PatchVertex>
    {
        private PatchVertex[] vertexArray;
        protected override PatchVertex[] vertices => vertexArray;
        private uint[] indexArray;
        protected override uint[] indices => indexArray;

        private int patchCountX;
        private int patchCountZ;

        public Patch(int patchesCountX, int patchesCountZ, int divX, int divZ)
        {
            primitiveType = PrimitiveType.Lines;
            Color = new Vector4(0.9f, 0.5f, 0.9f, 1.0f);

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
            GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Int, false, PatchVertex.Size, PatchVertex.OffsetIndex);
        }

        //public override void Render(ShaderManager shader)
        //{
        //    shader.SetupTransform(Color, GetModelMatrix());
        //    GL.BindVertexArray(vao);
        //    for (int start = 1; start < indices.Length; start += 3)
        //    {
        //        GL.DrawArrays(primitiveType, start, 4);
        //    }
        //    GL.BindVertexArray(0);
        //}

        public void Update(int divX, int divZ)
        {
            CalculateVertices(divX, divZ);
            CreateBuffers();
        }

        private void CalculateVertices(int divX, int divZ)
        {
            var vertexList = new List<PatchVertex>();
            var indexList = new List<uint>();

            //int width = patchesCountX * divX + 1;
            //int height = patchesCountZ * divZ + 1;
            int width = divX + 1; // for now
            int height = divZ + 1; // for now
            Func<int, int, uint> ind = (i, j) => (uint)(i * height + j);

            float stepX = 1.0f / divX, stepZ = 1.0f / divZ;
            int patchInd = 0;
            //for (int px = 0; px < patchesCountX; px++)
            //{
            //    for (int pz = 0; pz < patchesCountZ; pz++)
            //    {

            //    }
            //}

            // to remove
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    vertexList.Add(new PatchVertex(i * stepX, j * stepZ, 0));
                    if (i < width - 1)
                    {
                        indexList.AddMany(ind(i, j), ind(i + 1, j));
                    }
                    if (j < height - 1)
                    {
                        indexList.AddMany(ind(i, j), ind(i, j + 1));
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
