using OpenTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AbraCADabra
{
    public class Bezier3InterManager : Bezier3Manager
    {
        public override string DefaultName => "Bezier Interpolating";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public Bezier3InterManager(IEnumerable<PointManager> points)
            : base(new Bezier3(GetBernsteinFromInterpolation(points.Select(p => p.Transform.Position))),
                   new PolyLine(GetBernsteinFromInterpolation(points.Select(p => p.Transform.Position)),
                                new Vector4(0.7f, 0.7f, 0.0f, 1.0f)))
        {
            Points = new ObservableCollection<PointManager>();
            foreach (var point in points)
            {
                Points.Add(point);
                point.PropertyChanged += PointChanged;
                point.ManagerDisposing += PointDisposing;
            }
        }

        private static List<Vector3> GetBernsteinFromInterpolation(IEnumerable<Vector3> points)
        {
            var pointsList = new List<Vector3>(points);

            int pc = pointsList.Count;
            if (pc > 1)
            {
                var diffs = new List<float>();
                for (int i = 0; i < pc - 1; i++)
                {
                    float len = (pointsList[i + 1] - pointsList[i]).Length;
                    diffs.Add(len != 0.0f ? len : 1.0f);
                }

                // a = pointsList
                var c = new List<Vector3>(pc) { Vector3.Zero };
                if (pc > 2)
                {
                    var alph = new List<float>(pc - 3);
                    var diag = new List<float>(pc - 2);
                    var beta = new List<float>(pc - 3);
                    var pts = new List<Vector3>(pc - 2);
                    for (int i = 1; i < pc - 1; i++)
                    {
                        float di = diffs[i - 1] + diffs[i];
                        if (i > 1) alph.Add(diffs[i - 1] / di);
                        diag.Add(2.0f);
                        if (i < pc - 2) beta.Add(diffs[i] / di);
                        pts.Add(3.0f * (
                                      (pointsList[i + 1] - pointsList[i]) / diffs[i] -
                                      (pointsList[i] - pointsList[i - 1]) / diffs[i - 1]) / di);
                    }
                    c.AddRange(MathHelper.SolveTriDiag(alph, diag, beta, pts));
                }
                c.Add(Vector3.Zero);
                var b = new List<Vector3>(pc - 1);
                var d = new List<Vector3>(pc - 1);
                for (int i = 0; i < pc - 1; i++)
                {
                    float diff = diffs[i];
                    d.Add((c[i + 1] - c[i]) / (3.0f * diff));
                    b.Add((pointsList[i + 1] - pointsList[i]) / diff - c[i] * diff - d[i] * diff * diff);

                    b[i] *= diff;
                    c[i] *= diff * diff;
                    d[i] *= diff * diff * diff;
                }

                return GetBernsteinFromPower(pointsList, b, c, d);
            }
            return new List<Vector3>();
        }

        private static List<Vector3> GetBernsteinFromPower(List<Vector3> a, List<Vector3> b, List<Vector3> c, List<Vector3> d)
        {
            int n = b.Count; // a and c have one extra point - ignore it
            var points = new List<Vector3>(3 * n + 1) { a[0] };
            for (int i = 0; i < n; i++)
            {
                points.AddMany(b[i] / 3.0f, c[i] / 3.0f, d[i]);
                int ind = 3 * i;
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 3; k > j; k--)
                    {
                        points[ind + k] += points[ind + k - 1];
                    }
                }
            }
            return points;
        }

        public override void Update()
        {
            var bPoints = GetBernsteinFromInterpolation(Points.Select(p => p.Transform.Position));
            bezier.Update(bPoints);
            polyLine.Update(bPoints);
        }

        public override void Render(ShaderManager shader)
        {
            base.Render(shader);
        }

        public override void Translate(float x, float y, float z) // TODO: put it in Bezier3Manager
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

        public override void RotateAround(float xAngle, float yAngle, float zAngle, Vector3 center) // TODO: put it in Bezier3Manager
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
