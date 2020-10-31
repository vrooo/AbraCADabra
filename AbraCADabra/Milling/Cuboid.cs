using OpenTK;
using System.Collections.Generic;

namespace AbraCADabra.Milling
{
    public class Cuboid : IndexedTransform
    {
        private IndexedVertex[] _vertices = new IndexedVertex[0];
        protected override IndexedVertex[] vertices => _vertices;

        private uint[] _indices = new uint[0];
        protected override uint[] indices => _indices;

        public float SizeX { get; set; } = 15;
        public float SizeY { get; set; } = 5;
        public float SizeZ { get; set; } = 15;

        public uint DivX { get; set; } = 500;
        public uint DivZ { get; set; } = 500;

        bool shouldUpdate = true;

        public Cuboid()
        {
            Initialize();
        }

        public override void Render(ShaderManager shader)
        {
            if (shouldUpdate)
            {
                ActualUpdate();
                shouldUpdate = false;
            }
            base.Render(shader);
        }

        public void Update()
        {
            shouldUpdate = true;
        }

        private void ActualUpdate()
        {
            _vertices = new IndexedVertex[6 * DivX * DivZ + 12 * DivX + 12 * DivZ + 6];
            var indexList = new List<uint>();

            // top
            float stepX = SizeX / DivX, stepZ = SizeZ / DivZ;
            float xHalf = SizeX / 2, zHalf = SizeZ / 2;
            uint ind = 0;
            for (int ix = 0; ix < DivX; ix++)
            {
                float x = ix * stepX - xHalf;
                for (int iz = 0; iz < DivZ; iz++)
                {
                    float z = iz * stepZ - zHalf;

                    ind = AddTriangle(indexList, ind, x, SizeY, z, ix, iz,
                                                      x + stepX, SizeY, z + stepZ, ix + 1, iz + 1,
                                                      x + stepX, SizeY, z, ix + 1, iz);
                    ind = AddTriangle(indexList, ind, x, SizeY, z, ix, iz,
                                                      x, SizeY, z + stepZ, ix, iz + 1,
                                                      x + stepX, SizeY, z + stepZ, ix + 1, iz + 1);

                }
            }

            // sides
            for (int iz = 0; iz < DivZ; iz++)
            {
                float z = iz * stepZ - SizeZ / 2;

                ind = AddTriangle(indexList, ind, -xHalf, 0, z, -1, -1,
                                                  -xHalf, SizeY, z + stepZ, 0, iz + 1,
                                                  -xHalf, SizeY, z, 0, iz);
                ind = AddTriangle(indexList, ind, -xHalf, 0, z, -1, -1,
                                                  -xHalf, 0, z + stepZ, -1, -1,
                                                  -xHalf, SizeY, z + stepZ, 0, iz + 1);

                ind = AddTriangle(indexList, ind, xHalf, 0, z, -1, -1,
                                                  xHalf, SizeY, z, (int)DivX, iz,
                                                  xHalf, SizeY, z + stepZ, (int)DivX, iz + 1);
                ind = AddTriangle(indexList, ind, xHalf, 0, z, -1, -1,
                                                  xHalf, SizeY, z + stepZ, (int)DivX, iz + 1,
                                                  xHalf, 0, z + stepZ, -1, -1);
            }

            for (int ix = 0; ix < DivX; ix++)
            {
                float x = ix * stepX - SizeX / 2;

                ind = AddTriangle(indexList, ind, x, 0, -zHalf, -1, -1,
                                                  x, SizeY, -zHalf, ix, 0,
                                                  x + stepX, SizeY, -zHalf, ix + 1, 0);
                ind = AddTriangle(indexList, ind, x, 0, -zHalf, -1, -1,
                                                  x + stepX, SizeY, -zHalf, ix + 1, 0,
                                                  x + stepX, 0, -zHalf, -1, -1);

                ind = AddTriangle(indexList, ind, x, 0, zHalf, -1, -1,
                                                  x + stepX, SizeY, zHalf, ix + 1, (int)DivZ,
                                                  x, SizeY, zHalf, ix, (int)DivZ);
                ind = AddTriangle(indexList, ind, x, 0, zHalf, -1, -1,
                                                  x + stepX, 0, zHalf, -1, -1,
                                                  x + stepX, SizeY, zHalf, ix + 1, (int)DivZ);
            }

            //bottom
            ind = AddTriangle(indexList, ind, -xHalf, 0, -zHalf, -1, -1,
                                              xHalf, 0, -zHalf, -1, -1,
                                              xHalf, 0, zHalf, -1, -1);
            ind = AddTriangle(indexList, ind, -xHalf, 0, -zHalf, -1, -1,
                                              xHalf, 0, zHalf, -1, -1,
                                              -xHalf, 0, zHalf, -1, -1);

            _indices = indexList.ToArray();
            CreateBuffers();
        }

        private uint AddTriangle(List<uint> indexList, uint ind,
                                 float x1, float y1, float z1, int indx1, int indz1,
                                 float x2, float y2, float z2, int indx2, int indz2,
                                 float x3, float y3, float z3, int indx3, int indz3)
        {
            _vertices[ind]     = new IndexedVertex(new Vector3(x1, y1, z1), indx1, indz1);
            _vertices[ind + 1] = new IndexedVertex(new Vector3(x2, y2, z2), indx2, indz2);
            _vertices[ind + 2] = new IndexedVertex(new Vector3(x3, y3, z3), indx3, indz3);
            indexList.AddMany(ind, ind + 1, ind + 2);
            return ind + 3;
        }
    }
}
