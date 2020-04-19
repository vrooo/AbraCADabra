using OpenTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AbraCADabra
{
    class Bezier3C0Manager : Bezier3Manager
    {
        public override string DefaultName => "Bezier C0";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public Bezier3C0Manager(IEnumerable<PointManager> points)
            : base(new Bezier3(points.Select(p => p.Transform.Position)),
                   new PolyLine(points.Select(p => p.Transform.Position), new Vector4(0.7f, 0.7f, 0.0f, 1.0f)))
        {
            Points = new ObservableCollection<PointManager>();
            foreach (var point in points)
            {
                Points.Add(point);
                point.PropertyChanged += PointChanged;
                point.ManagerDisposing += PointDisposing;
            }
        }

        public override void Translate(float x, float y, float z)
        {
            foreach (var pm in Points)
            {
                pm.PropertyChanged -= PointChanged;
                pm.Translate(x, y, z);
                pm.PropertyChanged += PointChanged;
            }
            base.Translate(x, y, z);
            Update();
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
            Update();
        }
    }
}
