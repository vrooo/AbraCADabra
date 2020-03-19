using OpenTK;

namespace AbraCADabra
{
    class TorusManager : TransformManager
    {
        public override string DefaultName => "Torus";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public float MajorR { get; set; } = 6;
        public float MinorR { get; set; } = 4;
        public uint DivMajorR { get; set; } = 50;
        public uint DivMinorR { get; set; } = 50;

        private Torus torus;

        public TorusManager(Vector3 position, uint maxDivMajorR, uint maxDivMinorR) 
            : this(new Torus(position, maxDivMajorR, maxDivMinorR)) { }

        public TorusManager(Torus torus) : base(torus)
        {
            this.torus = torus;
            Update();
        }

        public override void Update()
        {
            torus.Update(MajorR, MinorR, DivMajorR, DivMinorR);
        }
    }
}
