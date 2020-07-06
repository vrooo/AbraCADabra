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

            var xs = new List<Vector4>(icm.Xs);
            if (icm.IsLoop && xs.Count > 0)
            {
                xs.Add(xs[0]);
            }

            var listLeft = new List<PathFigure>();
            var listRight = new List<PathFigure>();
            bool first = true;
            var prevX = new Vector4();
            foreach (var tmpx in xs)
            {
                var pointP = icm.P.ClampUV(tmpx.X, tmpx.Y);
                var pointQ = icm.Q.ClampUV(tmpx.Z, tmpx.W);

                var pointLeft = new System.Windows.Point(TranslateU(pointP.X / icm.P.UScale),
                                                         TranslateV(pointP.Y / icm.P.VScale));
                var pointRight = new System.Windows.Point(TranslateU(pointQ.X / icm.Q.UScale),
                                                          TranslateV(pointQ.Y / icm.Q.VScale));
                var x = new Vector4(tmpx.X / icm.P.UScale, tmpx.Y / icm.P.VScale, tmpx.Z / icm.Q.UScale, tmpx.W / icm.Q.VScale);
                if (first)
                {
                    first = false;
                    listLeft.Add(new PathFigure { StartPoint = pointLeft });
                    listRight.Add(new PathFigure { StartPoint = pointRight });
                }
                else
                {
                    if ((int)Math.Floor(x.X) != (int)Math.Floor(prevX.X))
                    {
                        double t = (Math.Max(Math.Floor(x.X), Math.Floor(prevX.X)) - x.X) / (prevX.X - x.X);
                        var midVec = icm.P.ClampUV(0, (float)(x.Y + t * (prevX.Y - x.Y)));
                        var midPointZero = new System.Windows.Point(TranslateU(0), TranslateV(midVec.Y));
                        var midPointOne = new System.Windows.Point(TranslateU(1), TranslateV(midVec.Y));
                        if (x.X > prevX.X)
                        {
                            listLeft[listLeft.Count - 1].Segments.Add(new LineSegment(midPointOne, true));
                            listLeft.Add(new PathFigure { StartPoint = midPointZero });
                        }
                        else
                        {
                            listLeft[listLeft.Count - 1].Segments.Add(new LineSegment(midPointZero, true));
                            listLeft.Add(new PathFigure { StartPoint = midPointOne });
                        }
                    }
                    if ((int)Math.Floor(x.Y) != (int)Math.Floor(prevX.Y))
                    {
                        double t = (Math.Max(Math.Floor(x.Y), Math.Floor(prevX.Y)) - x.Y) / (prevX.Y - x.Y);
                        var midVec = icm.P.ClampUV((float)(x.X + t * (prevX.X - x.X)), 0);
                        var midPointZero = new System.Windows.Point(TranslateU(midVec.X), TranslateV(0));
                        var midPointOne = new System.Windows.Point(TranslateU(midVec.X), TranslateV(1));
                        if (x.Y > prevX.Y)
                        {
                            listLeft[listLeft.Count - 1].Segments.Add(new LineSegment(midPointOne, true));
                            listLeft.Add(new PathFigure { StartPoint = midPointZero });
                        }
                        else
                        {
                            listLeft[listLeft.Count - 1].Segments.Add(new LineSegment(midPointZero, true));
                            listLeft.Add(new PathFigure { StartPoint = midPointOne });
                        }
                    }
                    if ((int)Math.Floor(x.Z) != (int)Math.Floor(prevX.Z))
                    {
                        double t = (Math.Max(Math.Floor(x.Z), Math.Floor(prevX.Z)) - x.Z) / (prevX.Z - x.Z);
                        var midVec = icm.P.ClampUV(0, (float)(x.W + t * (prevX.W - x.W)));
                        var midPointZero = new System.Windows.Point(TranslateU(0), TranslateV(midVec.Y));
                        var midPointOne = new System.Windows.Point(TranslateU(1), TranslateV(midVec.Y));
                        if (x.Z > prevX.Z)
                        {
                            listRight[listRight.Count - 1].Segments.Add(new LineSegment(midPointOne, true));
                            listRight.Add(new PathFigure { StartPoint = midPointZero });
                        }
                        else
                        {
                            listRight[listRight.Count - 1].Segments.Add(new LineSegment(midPointZero, true));
                            listRight.Add(new PathFigure { StartPoint = midPointOne });
                        }
                    }
                    if ((int)Math.Floor(x.W) != (int)Math.Floor(prevX.W))
                    {
                        double t = (Math.Max(Math.Floor(x.W), Math.Floor(prevX.W)) - x.W) / (prevX.W - x.W);
                        var midVec = icm.P.ClampUV((float)(x.Z + t * (prevX.Z - x.Z)), 0);
                        var midPointZero = new System.Windows.Point(TranslateU(midVec.X), TranslateV(0));
                        var midPointOne = new System.Windows.Point(TranslateU(midVec.X), TranslateV(1));
                        if (x.W > prevX.W)
                        {
                            listRight[listRight.Count - 1].Segments.Add(new LineSegment(midPointOne, true));
                            listRight.Add(new PathFigure { StartPoint = midPointZero });
                        }
                        else
                        {
                            listRight[listRight.Count - 1].Segments.Add(new LineSegment(midPointZero, true));
                            listRight.Add(new PathFigure { StartPoint = midPointOne });
                        }
                    }
                    listLeft[listLeft.Count - 1].Segments.Add(new LineSegment(pointLeft, true));
                    listRight[listRight.Count - 1].Segments.Add(new LineSegment(pointRight, true));
                }
                prevX = x;
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
