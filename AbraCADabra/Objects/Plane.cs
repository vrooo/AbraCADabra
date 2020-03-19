using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    class PlaneXZ : Transform
    {
        private List<float> vertexList = new List<float>();
        protected override float[] vertices => vertexList.ToArray();
        private List<uint> indexList = new List<uint>();
        protected override uint[] indices => indexList.ToArray();

        public PlaneXZ(float sizeX, float sizeZ, uint divX, uint divZ)
        {
            Color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            primitiveType = PrimitiveType.Lines;

            float startX = -sizeX / 2, stepX = sizeX / divX;
            float startZ = -sizeZ / 2, stepZ = sizeZ / divZ;
            for (uint ix = 0; ix < divX + 1; ix++)
            {
                float x = startX + stepX * ix;
                for (uint iz = 0; iz < divZ + 1; iz++)
                {
                    float z = startZ + stepZ * iz;
                    vertexList.Add(x);
                    vertexList.Add(0);
                    vertexList.Add(z);

                    uint index = ix * (divZ + 1) + iz;
                    if (iz < divZ)
                    {
                        indexList.Add(index);
                        indexList.Add(index + 1);
                    }
                    if (ix < divX)
                    {
                        indexList.Add(index);
                        indexList.Add(index + divZ + 1);
                    }
                }
            }

            Initialize();
        }
    }
}
