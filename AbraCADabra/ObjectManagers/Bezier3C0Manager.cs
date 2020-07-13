using AbraCADabra.Serialization;
using OpenTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AbraCADabra
{
    public class Bezier3C0Manager : Bezier3Manager
    {
        public override string DefaultName => "Bezier C0";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public Bezier3C0Manager(XmlBezierC0 xmlBezier, Dictionary<string, PointManager> points)
            : this(GetPointsFromDictionary(xmlBezier.Points, points), xmlBezier.Name)
        {
            DrawPolygon = xmlBezier.ShowControlPolygon;
        }

        public Bezier3C0Manager(IEnumerable<PointManager> points, string name = null)
            : base(new Bezier3(points.Select(p => p.Transform.Position)),
                   new PolyLine(points.Select(p => p.Transform.Position), new Vector4(0.7f, 0.7f, 0.0f, 1.0f)), name)
        {
            Points = new ObservableCollection<PointManager>();
            foreach (var point in points)
            {
                Points.Add(point);
                point.PropertyChanged += PointChanged;
                point.ManagerDisposing += PointDisposing;
                point.PointReplaced += ReplacePoint;
            }
        }
        protected override void ActualUpdate()
        {
            var points = Points.Select(p => p.Transform.Position);
            bezier.Update(points);
            polyLine.Update(points);
        }

        public override void Translate(float x, float y, float z)
        {
            foreach (var pm in Points.Distinct())
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
            foreach (var pm in Points.Distinct())
            {
                pm.PropertyChanged -= PointChanged;
                pm.RotateAround(xAngle, yAngle, zAngle, center);
                pm.PropertyChanged += PointChanged;
            }
            base.RotateAround(xAngle, yAngle, zAngle, center);
            Update();
        }

        public override XmlNamedType GetSerializable()
        {
            return new XmlBezierC0
            {
                Name = Name,
                ShowControlPolygon = DrawPolygon,
                Points = GetSerializablePoints()
            };
        }
    }
}
