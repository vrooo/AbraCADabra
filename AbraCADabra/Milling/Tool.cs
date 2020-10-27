using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra.Milling
{
    public class Tool : NormalTransform
    {
        private const uint DIVS = 20, SPHERE_DIVS = 4;

        private NormalVertex[] _vertices = new NormalVertex[0];
        protected override NormalVertex[] vertices => _vertices;

        private uint[] _indices = new uint[0];
        protected override uint[] indices => _indices;

        public float Diameter { get; set; }
        public float Height { get; set; }
        public bool IsFlat { get; set; }
        private bool shouldUpdate = true;

        public Tool(bool flat, float diam, float height)
        {
            IsFlat = flat;
            Diameter = diam;
            Height = height;
            Color = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            Initialize();
        }

        public void Update()
        {
            shouldUpdate = true;
        }

        private void ActualUpdate()
        {
            var vertexList = new List<NormalVertex>();
            var indexList = new List<uint>();

            float radius = Diameter / 2;
            float yBase = IsFlat ? 0 : radius;
            Vector3 center = new Vector3(0, yBase, 0);
            double angleStep = 2 * Math.PI / DIVS;

            // sides
            for (uint i = 0; i < DIVS; i++)
            {
                double angle = i * angleStep;
                double sin = Math.Sin(angle), cos = Math.Cos(angle);

                Vector3 point = new Vector3((float)(radius * cos), yBase, (float)(radius * sin));
                Vector3 normal = point - center;
                normal.Normalize();
                vertexList.Add(new NormalVertex(point, normal));

                point.Y = Height;
                vertexList.Add(new NormalVertex(point, normal));

                indexList.AddMany(2 * i, 2 * i + 1, (2 * i + 3) % (2 * DIVS));
                indexList.AddMany(2 * i, (2 * i + 3) % (2 * DIVS), (2 * i + 2) % (2 * DIVS));
            }

            // top
            uint topInd = (uint)vertexList.Count;
            vertexList.Add(new NormalVertex(new Vector3(0, Height, 0), Vector3.UnitY));
            for (uint i = 0; i < DIVS; i++)
            {
                double angle = i * angleStep;
                double sin = Math.Sin(angle), cos = Math.Cos(angle);

                Vector3 point = new Vector3((float)(radius * cos), Height, (float)(radius * sin));
                vertexList.Add(new NormalVertex(point, Vector3.UnitY));

                indexList.AddMany(topInd + i + 1, topInd, topInd + (i + 1) % (DIVS) + 1);
            }

            // bottom
            uint bottomInd = (uint)vertexList.Count;
            if (IsFlat)
            {
                vertexList.Add(new NormalVertex(center, -Vector3.UnitY));
                for (uint i = 0; i < DIVS; i++)
                {
                    double angle = i * angleStep;
                    Vector3 point = new Vector3((float)(radius * Math.Cos(angle)), yBase, (float)(radius * Math.Sin(angle)));
                    vertexList.Add(new NormalVertex(point, -Vector3.UnitY));

                    indexList.AddMany(bottomInd + i + 1, bottomInd + (i + 1) % (DIVS) + 1, bottomInd);
                }
            }
            else
            {
                double angleSphereStep = Math.PI / (SPHERE_DIVS * 2);
                for (uint i = 0; i <= SPHERE_DIVS; i++)
                {
                    double iAngle = i * angleSphereStep + Math.PI / 2;
                    double iSin = Math.Sin(iAngle);
                    double y = radius * Math.Cos(iAngle) + radius;
                    for (uint j = 0; j < DIVS; j++)
                    {
                        double jAngle = j * angleStep;
                        double x = radius * Math.Cos(jAngle) * iSin;
                        double z = radius * Math.Sin(jAngle) * iSin;

                        Vector3 point = new Vector3((float)x, (float)y, (float)z);
                        Vector3 normal = point - center;
                        normal.Normalize();
                        vertexList.Add(new NormalVertex(point, normal));

                        if (i < SPHERE_DIVS)
                        {
                            indexList.AddMany(bottomInd + i * DIVS + j,
                                              bottomInd + i * DIVS + (j + 1) % DIVS,
                                              bottomInd + (i + 1) * DIVS + (j + 1) % DIVS);
                            indexList.AddMany(bottomInd + i * DIVS + j,
                                              bottomInd + (i + 1) * DIVS + (j + 1) % DIVS,
                                              bottomInd + (i + 1) * DIVS + j);
                        }
                    }
                }
            }

            _vertices = vertexList.ToArray();
            _indices = indexList.ToArray();
            CreateBuffers();
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
    }
}
