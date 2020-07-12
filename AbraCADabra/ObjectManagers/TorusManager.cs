using AbraCADabra.Serialization;
using OpenTK;
using System;
using System.Collections.Generic;

namespace AbraCADabra
{
    public class TorusManager : FloatTransformManager, ISurface
    {
        public override string DefaultName => "Torus";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        private bool shouldUpdate;
        private float majorR = 6;
        public float MajorR
        {
            get { return majorR; }
            set
            {
                shouldUpdate = true;
                majorR = value;
            }
        }
        private float minorR = 4;
        public float MinorR
        {
            get { return minorR; }
            set
            {
                shouldUpdate = true;
                minorR = value;
            }
        }
        private uint divMajorR = 50;
        public uint DivMajorR
        {
            get { return divMajorR; }
            set
            {
                shouldUpdate = true;
                divMajorR = value;
            }
        }
        private uint divMinorR = 50;
        public uint DivMinorR
        {
            get { return divMinorR; }
            set
            {
                shouldUpdate = true;
                divMinorR = value;
            }
        }

        private List<IntersectionCurveManager> curves = new List<IntersectionCurveManager>();

        private Torus torus;

        // TODO: remove maxDivs
        public TorusManager(Vector3 position, uint maxDivMajorR, uint maxDivMinorR) 
            : this(new Torus(position, maxDivMajorR, maxDivMinorR)) { }

        public TorusManager(XmlTorus torus)
            : this(torus, new Torus(torus.Position.ToVector3(), 100, 100), torus.Name) { }

        private TorusManager(Torus torus) : base(torus)
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

        public void UpdateMesh() => Update();

        public override void Update()
        {
            shouldUpdate = true;
        }

        private void ActualUpdate()
        {
            torus.Update(MajorR, MinorR, DivMajorR, DivMinorR, this, curves);
        }

        public override void Render(ShaderManager shader)
        {
            if (shouldUpdate)
            {
                shouldUpdate = false;
                ActualUpdate();
            }
            base.Render(shader);
        }

        public int UScale => 1;
        public int VScale => 1;

        public bool IsUVValid(float u, float v) => true;
        public Vector2 GetClosestValidUV(float u, float v, float uPrev, float vPrev, out double t)
        {
            t = 1;
            return new Vector2(u, v);
        }

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
            return (new Vector4(torus.GetUVPointUntransformed(u, v, MajorR, MinorR), 1) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDu(float u, float v)
        {
            var (sinTheta, cosTheta) = MathHelper.SinCos(MathHelper.TWO_PI * u);
            var (sinPhi, cosPhi) = MathHelper.SinCos(MathHelper.TWO_PI * v);
            double pir = MathHelper.TWO_PI * MinorR;
            //return MathHelper.MakeVector3(-pir * cosPhi * sinTheta, pir * cosTheta, -pir * sinPhi * sinTheta);
            return (MathHelper.MakeVector4(-pir * cosPhi * sinTheta, pir * cosTheta, -pir * sinPhi * sinTheta, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDv(float u, float v)
        {
            double cosTheta = Math.Cos(MathHelper.TWO_PI * u);
            var (sinPhi, cosPhi) = MathHelper.SinCos(MathHelper.TWO_PI * v);
            double pid = MathHelper.TWO_PI * (MajorR + MinorR * cosTheta);
            //return MathHelper.MakeVector3(-pid * sinPhi, 0, pid * cosPhi);
            return (MathHelper.MakeVector4(-pid * sinPhi, 0, pid * cosPhi, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDuDu(float u, float v)
        {
            var (sinTheta, cosTheta) = MathHelper.SinCos(MathHelper.TWO_PI * u);
            var (sinPhi, cosPhi) = MathHelper.SinCos(MathHelper.TWO_PI * v);
            double pir = MathHelper.TWO_PI * MathHelper.TWO_PI * MinorR;
            //return MathHelper.MakeVector3(-pir * cosPhi * cosTheta, -pir * sinTheta, -pir * sinPhi * cosTheta);
            return (MathHelper.MakeVector4(-pir * cosPhi * cosTheta, -pir * sinTheta, -pir * sinPhi * cosTheta, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDvDv(float u, float v)
        {
            double cosTheta = Math.Cos(MathHelper.TWO_PI * u);
            var (sinPhi, cosPhi) = MathHelper.SinCos(MathHelper.TWO_PI * v);
            double pid = MathHelper.TWO_PI * MathHelper.TWO_PI * (MajorR + MinorR * cosTheta);
            //return MathHelper.MakeVector3(-pid * cosPhi, 0, -pid * sinPhi);
            return (MathHelper.MakeVector4(-pid * cosPhi, 0, -pid * sinPhi, 0) * torus.GetModelMatrix()).Xyz;
        }

        public Vector3 GetDuDv(float u, float v)
        {
            double sinTheta = Math.Sin(MathHelper.TWO_PI * u);
            var (sinPhi, cosPhi) = MathHelper.SinCos(MathHelper.TWO_PI * v);
            double pid = MathHelper.TWO_PI * MathHelper.TWO_PI * MinorR * sinTheta;
            //return MathHelper.MakeVector3(pid * sinPhi, 0, -pid * cosPhi);
            return (MathHelper.MakeVector4(pid * sinPhi, 0, -pid * cosPhi, 0) * torus.GetModelMatrix()).Xyz;
        }

        public void AddIntersectionCurve(IntersectionCurveManager icm)
        {
            icm.ManagerDisposing += CurveDisposing;
            curves.Add(icm);
        }

        private void CurveDisposing(TransformManager sender)
        {
            sender.ManagerDisposing -= CurveDisposing;
            curves.Remove(sender as IntersectionCurveManager);
            shouldUpdate = true;
            Update();
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
