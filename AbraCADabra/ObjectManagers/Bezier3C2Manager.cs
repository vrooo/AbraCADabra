using OpenTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AbraCADabra
{
    class Bezier3C2Manager : Bezier3Manager
    {
        private class PointWrapper
        {
            public Point Point;
            public int Index;
            public PointWrapper(Vector3 position, int index)
            {
                Point = new Point(position, new Vector4(0.5f, 0.0f, 0.9f, 1.0f), 8.0f);
                Index = index;
            }
        }

        public override string DefaultName => "Bezier C2";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public bool DrawVirtualPolygon { get; set; }
        public bool DrawVirtualPoints { get; set; } = true;

        private PolyLine virtualPolyLine;
        private List<PointWrapper> virtualPoints = new List<PointWrapper>();
        private PointWrapper selected;

        public Bezier3C2Manager(IEnumerable<PointManager> points)
            : base(new Bezier3(GetBernsteinFromDeBoor(points.Select(p => p.Transform.Position))),
                   new PolyLine(points.Select(p => p.Transform.Position), new Vector4(0.5f, 0.5f, 0.0f, 1.0f)))
        {
            Points = new ObservableCollection<PointManager>();
            foreach (var point in points)
            {
                Points.Add(point);
                point.PropertyChanged += PointChanged;
                point.ManagerDisposing += PointDisposing;
            }

            var bPoints = UpdateVirtualPoints(points.Select(p => p.Transform.Position));
            virtualPolyLine = new PolyLine(bPoints, new Vector4(0.3f, 0.0f, 0.7f, 1.0f));
        }

        private static List<Vector3> GetBernsteinFromDeBoor(IEnumerable<Vector3> points)
        {
            var pointsList = new List<Vector3>(points);
            var bPoints = new List<Vector3>();

            int pc = pointsList.Count;
            if (pc > 0)
            {
                bPoints.Add(pointsList[0]);
                for (int i = 1; i < pc; i++)
                {
                    Vector3 third = (pointsList[i] - pointsList[i - 1]) / 3;
                    Vector3 v1 = pointsList[i - 1] + third, v2 = pointsList[i - 1] + 2 * third;
                    if (i > 1)
                    {
                        bPoints.Add((v1 + bPoints[3 * i - 4]) / 2);
                    }
                    bPoints.Add(v1);
                    bPoints.Add(v2);
                }
                bPoints.Add(pointsList[pc - 1]);
            }

            return bPoints;
        }

        private IEnumerable<Vector3> UpdateVirtualPoints(IEnumerable<Vector3> points)
        {
            var bPoints = GetBernsteinFromDeBoor(points);
            if (bPoints.Count() != virtualPoints.Count)
            {
                foreach (var point in virtualPoints)
                {
                    point.Point?.Dispose();
                }
                virtualPoints.Clear();

                for (int i = 0; i < bPoints.Count; i++)
                {
                    virtualPoints.Add(new PointWrapper(bPoints[i], i));
                }
            }
            else
            {
                for (int i = 0; i < bPoints.Count; i++)
                {
                    virtualPoints[i].Point.Position = bPoints[i];
                }
            }

            return bPoints;
        }

        public override void Update()
        {
            var points = Points.Select(p => p.Transform.Position);
            polyLine.Update(points);

            var bPoints = UpdateVirtualPoints(points);
            bezier.Update(bPoints);
            virtualPolyLine.Update(bPoints);
        }

        public override void Render(ShaderManager shader)
        {
            base.Render(shader);
            if (DrawVirtualPolygon)
            {
                virtualPolyLine.Render(shader);
            }
            if (DrawVirtualPoints)
            {
                foreach (var pw in virtualPoints)
                {
                    pw.Point.Render(shader);
                }
            }
        }

        public override bool TestHit(Camera camera, float width, float height, float x, float y, out float z)
        {
            if (DrawVirtualPoints)
            {
                foreach (var pw in virtualPoints)
                {
                    if (pw.Point.TestHit(camera, width, height, x, y, out float zz))
                    {
                        selected = pw;
                        z = zz;
                        return true;
                    }
                }
            }
            selected = null;
            z = 0.0f;
            return false;
        }

        public override void Translate(float x, float y, float z)
        {
            if (selected != null)
            {
                TranslateVirtual(selected, x, y, z);
            }
            else
            {
                foreach (var pm in Points)
                {
                    pm.PropertyChanged -= PointChanged;
                    pm.Translate(x, y, z);
                    pm.PropertyChanged += PointChanged;
                }
            }
            base.Translate(x, y, z);
            Update();
        }

        private void TranslateVirtual(PointWrapper point, float x, float y, float z)
        {
            if (point.Index == 0)
            {
                Points[0].Translate(x, y, z);
            }
            else if (point.Index == virtualPoints.Count - 1)
            {
                Points[Points.Count - 1].Translate(x, y, z);
            }
            else
            {
                int index = point.Index / 3;
                switch ((point.Index - 1) % 3)
                {
                    case 0:
                        Points[index].Translate(3 * x, 3 * y, 3 * z);
                        Points[index + 1].Translate(-3 * x, -3 * y, -3 * z);
                        break;
                    case 1:
                        Points[index].Translate(-3 * x, -3 * y, -3 * z);
                        Points[index + 1].Translate(3 * x, 3 * y, 3 * z);
                        break;
                    case 2:
                        Points[index].Translate(x, y, z);
                        break;
                }
            }
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
