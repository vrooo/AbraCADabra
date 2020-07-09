using AbraCADabra.Serialization;
using OpenTK;
using System;

namespace AbraCADabra
{
    public class TorusManager : FloatTransformManager, ISurface
    {
        private const double TWO_PI = Math.PI * 2;
        public override string DefaultName => "Torus";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public float MajorR { get; set; } = 6;
        public float MinorR { get; set; } = 4;
        public uint DivMajorR { get; set; } = 50;
        public uint DivMinorR { get; set; } = 50;

        private Torus torus;

        // TODO: remove maxDivs
        public TorusManager(Vector3 position, uint maxDivMajorR, uint maxDivMinorR) 
            : this(new Torus(position, maxDivMajorR, maxDivMinorR)) { }

        public TorusManager(XmlTorus torus)
            : this(torus, new Torus(torus.Position.ToVector3(), 100, 100), torus.Name) { }

        public TorusManager(Torus torus) : base(torus)
        {
            this.torus = torus;
            Update();
        }

        private TorusManager(XmlTorus xmlTorus, Torus torus, string name) : base(torus, name)
        {
            this.torus = torus;
            torus.Rotation = xmlTorus.Rotation.ToVector3();
            torus.Scale = xmlTorus.Scale.ToVector3();
            MajorR = xmlTorus.MajorRadius;
            MinorR = xmlTorus.MinorRadius;
            DivMajorR = (uint)xmlTorus.VerticalSlices;
            DivMinorR = (uint)xmlTorus.HorizontalSlices;

            Update();
        }

        public override void Update()
        {
            torus.Update(MajorR, MinorR, DivMajorR, DivMinorR);
        }

        private (double sin, double cos) SinCos(double val)
        {
            return (Math.Sin(val), Math.Cos(val));
        }

        public int UScale => 1;
        public int VScale => 1;

        public bool IsUVValid(float u, float v) => true;

        public Vector2 ClampUV(float u, float v)
        {
            if (u < 0 || u > 1)
            {
                u -= (float)Math.Floor(u);
            }
            if (v < 0 || v > 1)
            {
                v -= (float)Math.Floor(v);
            }
            return new Vector2(u, v);
        }

        public Vector2 ClampScaledUV(float u, float v) => ClampUV(u, v);

        public Vector3 GetUVPoint(float u, float v)
        {
            var (sinTheta, cosTheta) = SinCos(TWO_PI * u);
            var (sinPhi, cosPhi) = SinCos(TWO_PI * v);
            double d = MajorR + MinorR * cosTheta;
            //return MathHelper.MakeVector3(d * cosPhi, MinorR * sinTheta, d * sinPhi);
            return (MathHelper.MakeVector4(d * cosPhi, MinorR * sinTheta, d * sinPhi, 1) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDu(float u, float v)
        {
            var (sinTheta, cosTheta) = SinCos(TWO_PI * u);
            var (sinPhi, cosPhi) = SinCos(TWO_PI * v);
            double pir = TWO_PI * MinorR;
            //return MathHelper.MakeVector3(-pir * cosPhi * sinTheta, pir * cosTheta, -pir * sinPhi * sinTheta);
            return (MathHelper.MakeVector4(-pir * cosPhi * sinTheta, pir * cosTheta, -pir * sinPhi * sinTheta, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDv(float u, float v)
        {
            double cosTheta = Math.Cos(TWO_PI * u);
            var (sinPhi, cosPhi) = SinCos(TWO_PI * v);
            double pid = TWO_PI * (MajorR + MinorR * cosTheta);
            //return MathHelper.MakeVector3(-pid * sinPhi, 0, pid * cosPhi);
            return (MathHelper.MakeVector4(-pid * sinPhi, 0, pid * cosPhi, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDuDu(float u, float v)
        {
            var (sinTheta, cosTheta) = SinCos(TWO_PI * u);
            var (sinPhi, cosPhi) = SinCos(TWO_PI * v);
            double pir = TWO_PI * TWO_PI * MinorR;
            //return MathHelper.MakeVector3(-pir * cosPhi * cosTheta, -pir * sinTheta, -pir * sinPhi * cosTheta);
            return (MathHelper.MakeVector4(-pir * cosPhi * cosTheta, -pir * sinTheta, -pir * sinPhi * cosTheta, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDvDv(float u, float v)
        {
            double cosTheta = Math.Cos(TWO_PI * u);
            var (sinPhi, cosPhi) = SinCos(TWO_PI * v);
            double pid = TWO_PI * TWO_PI * (MajorR + MinorR * cosTheta);
            //return MathHelper.MakeVector3(-pid * cosPhi, 0, -pid * sinPhi);
            return (MathHelper.MakeVector4(-pid * cosPhi, 0, -pid * sinPhi, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDuDv(float u, float v)
        {
            double sinTheta = Math.Sin(TWO_PI * u);
            var (sinPhi, cosPhi) = SinCos(TWO_PI * v);
            double pid = TWO_PI * TWO_PI * MinorR * sinTheta;
            //return MathHelper.MakeVector3(pid * sinPhi, 0, -pid * cosPhi);
            return (MathHelper.MakeVector4(pid * sinPhi, 0, -pid * cosPhi, 0) * torus.GetModelMatrix()).Xyz;
        }

        public override XmlNamedType GetSerializable()
        {
            return new XmlTorus
            {
                Name = Name,
                Position = new XmlVector3(Transform.Position),
                Rotation = new XmlVector3(Transform.Rotation),
                Scale = new XmlVector3(Transform.Scale),
                MajorRadius = MajorR,
                MinorRadius = MinorR,
                VerticalSlices = (int)DivMajorR,
                HorizontalSlices = (int)DivMinorR
            };
        }
    }
}
