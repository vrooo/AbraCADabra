using AbraCADabra.Serialization;
using OpenTK;

namespace AbraCADabra
{
    public class TorusManager : FloatTransformManager
    {
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
