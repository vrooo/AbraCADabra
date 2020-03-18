namespace AbraCADabra
{
    class TorusManager : MeshManager
    {
        public override string DefaultName => "Torus";

        public float MajorR { get; set; } = 6;
        public float MinorR { get; set; } = 4;
        public uint DivMajorR { get; set; } = 50;
        public uint DivMinorR { get; set; } = 50;

        private Torus torus;

        public TorusManager(uint maxDivMajorR, uint maxDivMinorR) : this(new Torus(maxDivMajorR, maxDivMinorR)) { }

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
