using OpenTK;
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
        public int UDivs { get; set; } = 50;
        public int VDivs { get; set; } = 50;
        public bool DrawPoints { get; set; } = true;
        private double width => CanvasLeft.ActualWidth;
        private double height => CanvasLeft.ActualHeight;

        public ParameterSpaceViewer()
        {
            InitializeComponent();
        }

        private double TranslateU(double u) => width * u;
        private double TranslateV(double v) => height * (1 - v);

        private void Update()
        {
            if (DataContext == null || width <= 0 || height <= 0)
            {
                return;
            }

            var gridPaths = MakeGridPaths();
            var curvePaths = MakeCurvePaths();

            // add all objects
            CanvasLeft.Children.Clear();
            CanvasLeft.Children.Add(gridPaths[0]);
            CanvasLeft.Children.Add(curvePaths[0]);
            CanvasRight.Children.Clear();
            CanvasRight.Children.Add(gridPaths[1]);
            CanvasRight.Children.Add(curvePaths[1]);

            if (DrawPoints)
            {
                var pointPaths = MakeDebugPoints();
                CanvasLeft.Children.Add(pointPaths[0]);
                CanvasRight.Children.Add(pointPaths[1]);
            }
        }

        private Path[] MakeDebugPoints()
        {
            var icm = DataContext as IntersectionCurveManager;
            List<PathFigure> MakeList(List<List<Vector2>> polygons)
            {
                var list = new List<PathFigure>();
                double uStep = 1.0 / UDivs, vStep = 1.0 / VDivs;
                for (int i = 0; i < UDivs + 1; i++)
                {
                    for (int j = 0; j < VDivs + 1; j++)
                    {
                        double u = i * uStep, v = j * vStep;
                        bool isInside = false;
                        foreach (var polygon in polygons)
                        {
                            if (MathHelper.IsPointInPolygon(new Vector2((float)u, (float)v), polygon))
                            {
                                isInside = !isInside;
                            }
                        }
                        if (isInside)
                        {
                            double x = TranslateU(u), y = TranslateV(v);
                            var fig1 = new PathFigure
                            {
                                StartPoint = new System.Windows.Point(x - 0.5, y - 0.5)
                            };
                            fig1.Segments.Add(new LineSegment(new System.Windows.Point(x + 0.5, y + 0.5), true));
                            var fig2 = new PathFigure
                            {
                                StartPoint = new System.Windows.Point(x - 0.5, y + 0.5)
                            };
                            fig2.Segments.Add(new LineSegment(new System.Windows.Point(x + 0.5, y - 0.5), true));
                            list.AddMany(fig1, fig2);
                        }
                    }
                }
                return list;
            }

            var paths = new Path[2];
            paths[0] = new Path
            {
                Stroke = Brushes.DarkRed,
                StrokeThickness = 4,
                Data = new PathGeometry(MakeList(icm.PolygonsP))
            };
            paths[1] = new Path
            {
                Stroke = Brushes.DarkRed,
                StrokeThickness = 4,
                Data = new PathGeometry(MakeList(icm.PolygonsQ))
            };
            return paths;
        }

        private Path[] MakeCurvePaths()
        {
            var icm = DataContext as IntersectionCurveManager;
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
                //Data = new PathGeometry(MakeList(icm.PolygonsP, true)) // debug
            };
            paths[1] = new Path
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Data = new PathGeometry(MakeList(icm.SegmentsQ, icm.IsClosedQ))
                //Data = new PathGeometry(MakeList(icm.PolygonsQ, true)) // debug
            };
            return paths;
        }

        private Path[] MakeGridPaths()
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

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Update();

        private void SliderDivChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Update();

        private void IntegerDivChanged(object sender, RoutedPropertyChangedEventArgs<object> e) => Update();

        private void CheckBoxPointsChanged(object sender, RoutedEventArgs e) => Update();
    }
}
