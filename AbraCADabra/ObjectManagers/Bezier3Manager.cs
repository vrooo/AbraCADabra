using OpenTK;
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

        public override void Update()
        {
            var points = Points.Select(p => p.Transform.Position);
            bezier.Update(points);
            polyLine.Update(points);
        }

        public override void Render(ShaderManager shader)
        {
            if (DrawPolygon)
            {
                polyLine.Render(shader);
            }
            shader.UseAdapt();
            base.Render(shader);
            shader.UseBasic();
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
