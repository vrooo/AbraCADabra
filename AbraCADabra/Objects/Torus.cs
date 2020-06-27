using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    public class Torus : FloatTransform
    {
        private List<float> vertexList = new List<float>();
        protected override float[] vertices => vertexList.ToArray();
        private List<uint> indexList = new List<uint>();
        protected override uint[] indices => indexList.ToArray();

        private uint maxDivMajorR, maxDivMinorR;

        public Torus(Vector3 position, uint maxDivMajorR, uint maxDivMinorR) : base(position)
        {
            this.maxDivMajorR = maxDivMajorR;
            this.maxDivMinorR = maxDivMinorR;

            primitiveType = PrimitiveType.Lines;
            Color = new Vector4(0.7f, 0.7f, 0.3f, 1.0f);

            //CalculateVertices(majorR, minorR, divMajorR, divMinorR);
            uint maxVertices = (maxDivMajorR + 1) * (maxDivMinorR + 1) * 3;
            uint maxIndices = (maxDivMajorR + 1) * (maxDivMinorR + 1) * 4;
            Initialize((int)maxVertices, (int)maxIndices);
        }

        public void Update(float majorR, float minorR, uint divMajorR, uint divMinorR)
        {
            if (divMajorR < 3 || divMajorR > maxDivMajorR || divMinorR < 3 || divMinorR > maxDivMinorR)
            {
                return;
            }
            CalculateVertices(majorR, minorR, divMajorR, divMinorR);
            UpdateBuffers();
        }

        private void CalculateVertices(float majorR, float minorR, uint divMajorR, uint divMinorR)
        {
            vertexList.Clear();
            indexList.Clear();

            double thetaStep = 2 * Math.PI / divMinorR;
            double phiStep = 2 * Math.PI / divMajorR;
            uint maxInd = (divMajorR + 1) * (divMinorR + 1);
            for (uint iTheta = 0; iTheta < divMinorR + 1; iTheta++)
            {
                double theta = iTheta * thetaStep;
                double sinTheta = Math.Sin(theta), cosTheta = Math.Cos(theta);
                double y = minorR * sinTheta;
                for (uint iPhi = 0; iPhi < divMajorR + 1; iPhi++)
                {
                    double phi = iPhi * phiStep;
                    double sinPhi = Math.Sin(phi), cosPhi = Math.Cos(phi);
                    double d = majorR + minorR * cosTheta;
                    double x = d * cosPhi, z = d * sinPhi;

                    vertexList.Add((float)x);
                    vertexList.Add((float)y);
                    vertexList.Add((float)z);

                    uint index = iPhi * (divMinorR + 1) + iTheta;
                    if (index + 1 < maxInd)
                    {
                        indexList.Add(index);
                        indexList.Add(index + 1);
                    }
                    if (index + divMajorR + 1 < maxInd)
                    {
                        indexList.Add(index);
                        indexList.Add(index + divMajorR + 1);
                    }
                }
            }
        }
    }
}
