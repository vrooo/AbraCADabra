using OpenTK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AbraCADabra
{
    class Bezier3Manager : TransformManager
    {
        public override string DefaultName => "Bezier3";
        private static int counter = 0;
        protected override int instanceCounter => counter++;

        public ObservableCollection<PointManager> Points { get; set; }
        public bool DrawPolygon { get; set; }

        private Bezier3 bezier;
        private PolyLine polyLine;

        public Bezier3Manager(IEnumerable<PointManager> points)
            : this(new Bezier3(points.Select(p => p.Transform.Position)),
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

        private Bezier3Manager(Bezier3 bezier, PolyLine polyLine) : base(bezier)
        {
            this.bezier = bezier;
            this.polyLine = polyLine;
        }

        //private static List<int> CalculateDivisions(IEnumerable<PointManager> points)
        //{
        //    Func<float, int> lenToDivs = l => (int)(l);

        //    List<int> divs = new List<int>();
        //    float len = 0.0f;
        //    int count = 0;
        //    Vector3 prev = new Vector3();
        //    bool first = true;
        //    foreach(var point in points)
        //    {
        //        Vector3 screen = point.GetScreenSpaceCoords(Camera, GLControl.Width, GLControl.Height);
        //        screen.X = Math.Abs(screen.X);
        //        screen.Y = Math.Abs(screen.Y);
        //        screen.Z = Math.Max(0.0f, screen.Z);
        //        if (first)
        //        {
        //            first = false;
        //        }
        //        else
        //        {
        //            len += (prev - screen).Length;
        //        }

        //        count++;
        //        if (count == 4)
        //        {
        //            divs.Add(lenToDivs(len));
        //            len = 0.0f;
        //            count = 1;
        //        }
        //        prev = screen;
        //    }
        //    if (count > 1)
        //    {
        //        divs.Add(lenToDivs(len));
        //    }
        //    return divs;
        //}

        public override void Update() { }

        public override void Render(ShaderManager shader)
        {
            var points = Points.Select(p => p.Transform.Position);
            bezier.Update(points);
            if (DrawPolygon)
            {
                polyLine.Update(points);
                polyLine.Render(shader);
            }
            shader.UseAdapt();
            base.Render(shader);
            shader.UseBasic();
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

        private void PointChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }

        private void PointDisposing(TransformManager sender)
        {
            RemovePoint(sender as PointManager);
        }

        public void RemovePoint(PointManager point)
        {
            Points.Remove(point);
            point.PropertyChanged -= PointChanged;
            point.ManagerDisposing -= PointDisposing;
            Update();
        }

        public void AddPoint(PointManager point)
        {
            if (!Points.Contains(point))
            {
                Points.Add(point);
                point.PropertyChanged += PointChanged;
                point.ManagerDisposing += PointDisposing;
                Update();
            }
        }

        public void MovePoint(int oldIndex, int newIndex)
        {
            Points.Move(oldIndex, newIndex);
            Update();
        }

        // TODO: add and remove points
    }
}
