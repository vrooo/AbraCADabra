using OpenTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AbraCADabra
{
    abstract class Bezier3Manager : TransformManager<AdjacencyVertex>
    {
        public ObservableCollection<PointManager> Points { get; set; }
        public bool DrawPolygon { get; set; }

        protected Bezier3 bezier;
        protected PolyLine polyLine;

        protected Bezier3Manager(Bezier3 bezier, PolyLine polyLine) : base(bezier)
        {
            this.bezier = bezier;
            this.polyLine = polyLine;
        }

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

        protected void PointChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }

        protected void PointDisposing(TransformManager sender)
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
    }
}
