using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for ParameterSpaceViewer.xaml
    /// </summary>
    public partial class ParameterSpaceViewer : Window
    {
        public int UDivs { get; set; } = 10;
        public int VDivs { get; set; } = 10;
        public ParameterSpaceViewer()
        {
            InitializeComponent();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = CanvasLeft.ActualWidth, height = CanvasLeft.ActualHeight;

            var gridPaths = MakeGridPaths(width, height);
            var curvePaths = MakeCurvePaths(width, height);

            // add all objects
            CanvasLeft.Children.Clear();
            CanvasLeft.Children.Add(gridPaths[0]);
            CanvasLeft.Children.Add(curvePaths[0]);
            CanvasRight.Children.Clear();
            CanvasRight.Children.Add(gridPaths[1]);
            CanvasRight.Children.Add(curvePaths[1]);
        }

        private Path[] MakeCurvePaths(double width, double height)
        {
            var icm = DataContext as IntersectionCurveManager;
            double distSqX = (width / 2) * (width / 2), distSqY = (height / 2) * (height / 2);

            Func<ISurface, float, double> TranslateU = (s, u) => width * (u / s.UScale);
            Func<ISurface, float, double> TranslateV = (s, v) => height * (1 - v / s.VScale);
            Func<double, double, bool> CompSq = (x1, x2sq) => x1 * x1 > x2sq;

            var xs = new List<OpenTK.Vector4>(icm.Xs);
            if (icm.IsLoop && xs.Count > 0)
            {
                xs.Add(xs[0]);
            }

            var listLeft = new List<PathFigure>();
            var listRight = new List<PathFigure>();
            bool first = true;
            var prevLeft = new System.Windows.Point();
            var prevRight = new System.Windows.Point();
            foreach (var x in xs)
            {
                var pointP = icm.P.ClampUV(x.X, x.Y);
                var pointLeft = new System.Windows.Point(TranslateU(icm.P, pointP.X), TranslateV(icm.P, pointP.Y));
                var pointQ = icm.Q.ClampUV(x.Z, x.W);
                var pointRight = new System.Windows.Point(TranslateU(icm.Q, pointQ.X), TranslateV(icm.Q, pointQ.Y));
                if (first)
                {
                    first = false;
                    listLeft.Add(new PathFigure { StartPoint = pointLeft });
                    listRight.Add(new PathFigure { StartPoint = pointRight });
                }
                else
                {
                    if (CompSq(pointLeft.X - prevLeft.X, distSqX) || CompSq(pointLeft.Y - prevLeft.Y, distSqY))
                    {
                        listLeft.Add(new PathFigure { StartPoint = pointLeft });
                    }
                    else
                    {
                        listLeft[listLeft.Count - 1].Segments.Add(new LineSegment(pointLeft, true));
                    }
                    if (CompSq(pointRight.X - prevRight.X, distSqX) || CompSq(pointRight.Y - prevRight.Y, distSqY))
                    {
                        listRight.Add(new PathFigure { StartPoint = pointRight });
                    }
                    else
                    {
                        listRight[listRight.Count - 1].Segments.Add(new LineSegment(pointRight, true));
                    }
                }
                prevLeft = pointLeft;
                prevRight = pointRight;
            }

            var paths = new Path[2];
            paths[0] = new Path
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Data = new PathGeometry(listLeft)
            };
            paths[1] = new Path
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Data = new PathGeometry(listRight)
            };
            return paths;
        }

        private Path[] MakeGridPaths(double width, double height)
        {
            double xStep = width / UDivs, yStep = height / VDivs;
            var gridList = new List<PathFigure>();
            for (int i = 0; i < UDivs + 1; i++)
            {
                double x = i * xStep;
                var segment = new LineSegment(new System.Windows.Point(x, height), true);
                var figure = new PathFigure
                {
                    StartPoint = new System.Windows.Point(x, 0),
                    IsClosed = false
                };
                figure.Segments.Add(segment);
                gridList.Add(figure);
            }
            for (int i = 0; i < VDivs + 1; i++)
            {
                double y = height - i * yStep;
                var segment = new LineSegment(new System.Windows.Point(width, y), true);
                var figure = new PathFigure
                {
                    StartPoint = new System.Windows.Point(0, y),
                    IsClosed = false
                };
                figure.Segments.Add(segment);
                gridList.Add(figure);
            }

            var paths = new Path[2];
            for (int i = 0; i < 2; i++)
            {
                paths[i] = new Path
                {
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    Data = new PathGeometry(gridList)
                };
            }
            return paths;
        }
    }
}
