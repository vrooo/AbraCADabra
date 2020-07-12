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
            //uint maxVertices = (maxDivMajorR + 1) * (maxDivMinorR + 1) * 3;
            //uint maxIndices = (maxDivMajorR + 1) * (maxDivMinorR + 1) * 4;
            Initialize();
        }

        public void Update(float majorR, float minorR, uint divMajorR, uint divMinorR,
            ISurface parent, List<IntersectionCurveManager> curves)
        {
            if (divMajorR < 3 || divMajorR > maxDivMajorR || divMinorR < 3 || divMinorR > maxDivMinorR)
            {
                return;
            }
            CalculateVertices(majorR, minorR, divMajorR, divMinorR, parent, curves);
            //UpdateBuffers();
            CreateBuffers();
        }

        public Vector3 GetUVPointUntransformed(float u, float v, float majorR, float minorR)
        {
            var (sinTheta, cosTheta) = MathHelper.SinCos(MathHelper.TWO_PI * u);
            var (sinPhi, cosPhi) = MathHelper.SinCos(MathHelper.TWO_PI * v);
            double d = majorR + minorR * cosTheta;
            return MathHelper.MakeVector3(d * cosPhi, minorR * sinTheta, d * sinPhi);
        }

        private void CalculateVertices(float majorR, float minorR, uint divMajorR, uint divMinorR,
            ISurface parent = null, List<IntersectionCurveManager> curves = null)
        {
            vertexList.Clear();
            indexList.Clear();

            var uvs = new List<Vector2>();
            double thetaStep = MathHelper.TWO_PI / divMinorR, phiStep = MathHelper.TWO_PI / divMajorR;
            double uStep = 1.0 / divMinorR, vStep = 1.0 / divMajorR;
            uint maxInd = (divMajorR + 1) * (divMinorR + 1);
            for (uint iTheta = 0; iTheta < divMinorR + 1; iTheta++)
            {
                double u = iTheta * uStep;
                double theta = iTheta * thetaStep;
                double sinTheta = Math.Sin(theta), cosTheta = Math.Cos(theta);
                double y = minorR * sinTheta;
                for (uint iPhi = 0; iPhi < divMajorR + 1; iPhi++)
                {
                    double v = iPhi * vStep;
                    uvs.Add(new Vector2((float)u, (float)v));

                    double phi = iPhi * phiStep;
                    double sinPhi = Math.Sin(phi), cosPhi = Math.Cos(phi);
                    double d = majorR + minorR * cosTheta;
                    double x = d * cosPhi, z = d * sinPhi;
                    vertexList.AddMany((float)x, (float)y, (float)z);

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
            
            if (curves != null)
            {
                foreach (var curve in curves)
                {
                    curve.Trim(parent, uvs, indexList);
                }
                for (int i = vertexList.Count / 3; i < uvs.Count; i++)
                {
                    var point = GetUVPointUntransformed(uvs[i].X, uvs[i].Y, majorR, minorR);
                    vertexList.AddMany(point.X, point.Y, point.Z);
                }
            }
        }
    }
}
