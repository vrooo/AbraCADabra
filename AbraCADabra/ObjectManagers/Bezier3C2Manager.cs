using OpenTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AbraCADabra
{
    class Bezier3C2Manager : Bezier3Manager
    {
        public override string DefaultName => "Bezier C0";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public Bezier3C2Manager(IEnumerable<PointManager> points)
            : this(new Bezier3C0(points.Select(p => p.Transform.Position)),
                   new PolyLine(points.Select(p => p.Transform.Position)))
        {
            Points = new ObservableCollection<PointManager>();
            foreach (var point in points)
            {
                Points.Add(point);
                point.PropertyChanged += PointChanged;
                point.ManagerDisposing += PointDisposing;
            }
        }

        private Bezier3C2Manager(Bezier3C0 bezier, PolyLine polyLine) : base(bezier, polyLine) { }

        public override void Translate(float x, float y, float z)
        {
            foreach (var pm in Points)
            {
                pm.PropertyChanged -= PointChanged;
                pm.Translate(x, y, z);
                pm.PropertyChanged += PointChanged;
            }
            base.Translate(x, y, z);
        }

        public override void RotateAround(float xAngle, float yAngle, float zAngle, Vector3 center)
        {
            foreach (var pm in Points)
            {
                pm.PropertyChanged -= PointChanged;
                pm.RotateAround(xAngle, yAngle, zAngle, center);
                pm.PropertyChanged += PointChanged;
            }
            base.RotateAround(xAngle, yAngle, zAngle, center);
        }
    }
}
