using OpenTK;

namespace AbraCADabra
{
    public class PointManager : FloatTransformManager
    {
        public override string DefaultName => "Point";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public override bool Deletable => !IsSurface;
        public bool IsSurface { get; set; }

        private Point point;

        public PointManager(Vector3 position, bool isSurface = false)
            : this(new Point(position, new Vector4(1.0f, 1.0f, 0.0f, 1.0f)), isSurface) { }

        public PointManager(Point point, bool isSurface = false) : base(point)
        {
            this.point = point;
            IsSurface = isSurface;
        }

        public override void Update() { }
    }
}
