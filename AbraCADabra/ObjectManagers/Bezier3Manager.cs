using AbraCADabra.Serialization;
using OpenTK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AbraCADabra
{
    public abstract class Bezier3Manager : TransformManager<AdjacencyVertex>
    {
        public ObservableCollection<PointManager> Points { get; set; }
        public bool DrawPolygon { get; set; }

        protected Bezier3 bezier;
        protected PolyLine polyLine;
        protected bool shouldUpdate = false;

        protected Bezier3Manager(Bezier3 bezier, PolyLine polyLine, string name = null) : base(bezier, name)
        {
            this.bezier = bezier;
            this.polyLine = polyLine;
        }

        protected static IEnumerable<PointManager> GetPointsFromDictionary(XmlBezierPointRef[] pointRefs, Dictionary<string, PointManager> points)
        {
            var pms = new List<PointManager>();
            foreach (var pointRef in pointRefs)
            {
                if (points.TryGetValue(pointRef.Name, out PointManager pm))
                {
                    pms.Add(pm);
                }
                else
                {
                    throw new KeyNotFoundException("Required point was not found in dictionary");
                }
            }
            return pms;
        }

        public override void Update()
        {
            shouldUpdate = true;
        }

        protected abstract void ActualUpdate();

        public override void Render(ShaderManager shader)
        {
            if (shouldUpdate)
            {
                shouldUpdate = false;
                ActualUpdate();
            }
            if (DrawPolygon)
            {
                polyLine.Render(shader);
            }
            shader.UseBezier();
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
            point.PointReplaced -= ReplacePoint;
            Update();
        }

        public void AddPoint(PointManager point, int index = -1)
        {
            if (!Points.Contains(point))
            {
                if (index == -1)
                {
                    Points.Add(point);
                }
                else
                {
                    Points.Insert(index, point);
                }
                point.PropertyChanged += PointChanged;
                point.ManagerDisposing += PointDisposing;
                point.PointReplaced += ReplacePoint;
                Update();
            }
        }

        public void ReplacePoint(PointManager oldPoint, PointManager newPoint)
        {
            int index = Points.IndexOf(oldPoint);
            RemovePoint(oldPoint);
            AddPoint(newPoint, index);
        }

        public void MovePoint(int oldIndex, int newIndex)
        {
            Points.Move(oldIndex, newIndex);
            Update();
        }

        protected XmlBezierPointRef[] GetSerializablePoints()
        {
            var pts = new XmlBezierPointRef[Points.Count];
            int i = 0;
            foreach (var point in Points)
            {
                pts[i++] = new XmlBezierPointRef { Name = point.Name };
            }
            return pts;
        }
    }
}
