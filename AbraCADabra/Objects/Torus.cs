using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace AbraCADabra
{
    class Torus : FloatTransform
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

            float thetaStep = (float)(2 * Math.PI / divMinorR);
            float phiStep = (float)(2 * Math.PI / divMajorR);
            uint maxInd = (divMajorR + 1) * (divMinorR + 1);
            for (uint iTheta = 0; iTheta < divMinorR + 1; iTheta++)
            {
                float theta = iTheta * thetaStep;
                float sinTheta = (float)Math.Sin(theta), cosTheta = (float)Math.Cos(theta);
                float y = minorR * sinTheta;
                for (uint iPhi = 0; iPhi < divMajorR + 1; iPhi++)
                {
                    float phi = iPhi * phiStep;
                    float sinPhi = (float)Math.Sin(phi), cosPhi = (float)Math.Cos(phi);
                    float d = majorR + minorR * cosTheta;
                    float x = d * cosPhi, z = d * sinPhi;

                    vertexList.Add(x);
                    vertexList.Add(y);
                    vertexList.Add(z);

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
