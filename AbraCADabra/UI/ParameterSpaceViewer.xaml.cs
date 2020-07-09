using OpenTK;
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
            Func<double, double> TranslateU = u => width * u;
            Func<double, double> TranslateV = v => height * (1 - v);
            List<PathFigure> MakeList(List<List<Vector2>> segmentList, bool close)
            {
                var list = new List<PathFigure>();
                foreach (var segment in segmentList)
                {
                    var fig = new PathFigure
                    {
                        StartPoint = new System.Windows.Point(TranslateU(segment[0].X),
                                                              TranslateV(segment[0].Y)),
                        IsClosed = close
                    };
                    for (int i = 1; i < segment.Count; i++)
                    {
                        fig.Segments.Add(new LineSegment(new System.Windows.Point(TranslateU(segment[i].X),
                                                                                  TranslateV(segment[i].Y)), true));
                    }
                    list.Add(fig);
                }
                return list;
            }

            var paths = new Path[2];
            paths[0] = new Path
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Data = new PathGeometry(MakeList(icm.SegmentsP, icm.IsClosedP))
            };
            paths[1] = new Path
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Data = new PathGeometry(MakeList(icm.SegmentsQ, icm.IsClosedQ))
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
