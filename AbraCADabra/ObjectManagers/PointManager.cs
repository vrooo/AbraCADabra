using AbraCADabra.Serialization;
using OpenTK;

namespace AbraCADabra
{
    public class PointManager : FloatTransformManager
    {
        public override string DefaultName => "Point";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public override bool Deletable => SurfaceCount == 0;
        public int SurfaceCount { get; set; }

        private Point point;

        public delegate void PointReplacedEventHandler(PointManager oldPoint, PointManager newPoint);
        public event PointReplacedEventHandler PointReplaced;

        public PointManager(Vector3 position, bool isSurface = false)
            : this(new Point(position, new Vector4(1.0f, 1.0f, 0.0f, 1.0f)), isSurface) { }

        public PointManager(XmlPoint point)
            : this(new Point(point.Position.ToVector3(), new Vector4(1.0f, 1.0f, 0.0f, 1.0f)), false, point.Name) { }

        public PointManager(Point point, bool isSurface = false, string name = null) : base(point, name)
        {
            this.point = point;
            SurfaceCount = isSurface ? 1 : 0;
        }

        public void Replace(PointManager newPoint)
        {
            PointReplaced?.Invoke(this, newPoint);
        }

        public override void Update() { }

        public override XmlNamedType GetSerializable()
        {
            return new XmlPoint { Name = Name, Position = new XmlVector3(Transform.Position) };
        }
    }
}
