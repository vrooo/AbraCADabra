using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for PropertyWindow.xaml
    /// </summary>
    public partial class PropertyWindow : Window
    {
        private struct DisplayParams
        {
            public GroupBox GroupBox;
            public bool ShowPosition;
            public bool ShowRotation;
            public bool ShowScale;

            public DisplayParams(GroupBox gb, bool showPos, bool showRot, bool showSca)
            {
                GroupBox = gb;
                ShowPosition = showPos;
                ShowRotation = showRot;
                ShowScale = showSca;
            }
        }
        List<GroupBox> groupBoxes;
        Dictionary<Type, DisplayParams> typeMap;
        public PropertyWindow()
        {
            InitializeComponent();
            groupBoxes = new List<GroupBox>
            {
                GroupTorus,
                GroupBezier3C0,
                GroupBezier3C2,
                GroupBezier3Inter,
                GroupPatchC0,
                GroupPatchC2,
                GroupGregoryPatch,
                GroupIntersectionCurve
            };
            typeMap = new Dictionary<Type, DisplayParams>
            {
                { typeof(TorusManager), new DisplayParams(GroupTorus, true, true, true) },
                { typeof(PointManager), new DisplayParams(null, true, false, false) },
                { typeof(Bezier3C0Manager), new DisplayParams(GroupBezier3C0, false, false, false) },
                { typeof(Bezier3C2Manager), new DisplayParams(GroupBezier3C2, false, false, false) },
                { typeof(Bezier3InterManager), new DisplayParams(GroupBezier3Inter, false, false, false) },
                { typeof(PatchC0Manager), new DisplayParams(GroupPatchC0, false, false, false) },
                { typeof(PatchC2Manager), new DisplayParams(GroupPatchC2, false, false, false) },
                { typeof(GregoryPatchManager), new DisplayParams(GroupGregoryPatch, false, false, false) },
                { typeof(IntersectionCurveManager), new DisplayParams(GroupIntersectionCurve, false, false, false) }
            };

            ToggleIntersectionFirstA.Checked += (sender, e) => ToggleIntersectionTrim(ToggleIntersectionFirstA, ToggleIntersectionFirstB);
            ToggleIntersectionFirstB.Checked += (sender, e) => ToggleIntersectionTrim(ToggleIntersectionFirstB, ToggleIntersectionFirstA);
            ToggleIntersectionSecondA.Checked += (sender, e) => ToggleIntersectionTrim(ToggleIntersectionSecondA, ToggleIntersectionSecondB);
            ToggleIntersectionSecondB.Checked += (sender, e) => ToggleIntersectionTrim(ToggleIntersectionSecondB, ToggleIntersectionSecondA);

            ToggleIntersectionFirstA.Checked += (sender, e) => UpdateSurface(true);
            ToggleIntersectionFirstA.Unchecked += (sender, e) => UpdateSurface(true);
            ToggleIntersectionFirstB.Checked += (sender, e) => UpdateSurface(true);
            ToggleIntersectionFirstB.Unchecked += (sender, e) => UpdateSurface(true);

            ToggleIntersectionSecondA.Checked += (sender, e) => UpdateSurface(false);
            ToggleIntersectionSecondA.Unchecked += (sender, e) => UpdateSurface(false);
            ToggleIntersectionSecondB.Checked += (sender, e) => UpdateSurface(false);
            ToggleIntersectionSecondB.Unchecked += (sender, e) => UpdateSurface(false);
        }

        public delegate void PropertyUpdatedEventHandler(TransformManager context);
        public event PropertyUpdatedEventHandler PropertyUpdated;

        private void ContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var group in groupBoxes)
            {
                group.Visibility = Visibility.Collapsed;
            }

            if (DataContext != null)
            {
                GroupObject.IsEnabled = true;
                GroupTransform.IsEnabled = true;

                Type type = DataContext.GetType();
                if (typeMap.TryGetValue(type, out DisplayParams dp))
                {
                    if (dp.GroupBox != null)
                    {
                        dp.GroupBox.Visibility = Visibility.Visible;
                    }
                    GridPosition.IsEnabled = dp.ShowPosition;
                    GridRotation.IsEnabled = dp.ShowRotation;
                    GridScale.IsEnabled = dp.ShowScale;
                }

                if (DataContext is IntersectionCurveManager icm)
                {
                    if (icm.TrimModeP == TrimMode.SideA)
                    {
                        ToggleIntersectionFirstA.IsChecked = true;
                    }
                    else if (icm.TrimModeP == TrimMode.SideB)
                    {
                        ToggleIntersectionFirstB.IsChecked = true;
                    }
                    else
                    {
                        ToggleIntersectionFirstA.IsChecked = false;
                        ToggleIntersectionFirstB.IsChecked = false;
                    }

                    if (icm.TrimModeQ == TrimMode.SideA)
                    {
                        ToggleIntersectionSecondA.IsChecked = true;
                    }
                    else if (icm.TrimModeQ == TrimMode.SideB)
                    {
                        ToggleIntersectionSecondB.IsChecked = true;
                    }
                    else
                    {
                        ToggleIntersectionSecondA.IsChecked = false;
                        ToggleIntersectionSecondB.IsChecked = false;
                    }
                }
            }
            else
            {
                GroupObject.IsEnabled = false;
                GroupTransform.IsEnabled = false;
            }
        }

        private void SliderUpdate(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DataContext != null)
            {
                PropertyUpdated?.Invoke(DataContext as TransformManager);
            }
        }

        private void DecimalUpdate(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext != null)
            {
                PropertyUpdated?.Invoke(DataContext as TransformManager);
            }
        }

        private void CheckBoxUpdate(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                PropertyUpdated?.Invoke(DataContext as TransformManager);
            }
        }

        private void ButtonBezier3Add(object sender, RoutedEventArgs e)
        {
            var points = (Owner as MainWindow).GetObjectsOfType(typeof(PointManager));
            var isw = new ItemSelectionWindow
            {
                DataContext = points
            };
            bool? result = isw.ShowDialog();
            if (result.HasValue && result.Value)
            {
                foreach (var item in isw.SelectedItems)
                {
                    (DataContext as Bezier3Manager).AddPoint(item as PointManager);
                }
                PropertyUpdated?.Invoke(DataContext as TransformManager);
            }
        }

        private void ButtonBezier3Remove(object sender, RoutedEventArgs e)
        {
            var ListBezier3 = (sender as Button).Tag as ListBox;
            var selected = ListBezier3.SelectedItems;
            if (selected.Count > 0)
            {
                for (int i = selected.Count - 1; i >= 0; i--)
                {
                    (DataContext as Bezier3Manager).RemovePoint(selected[i] as PointManager);
                }
                PropertyUpdated?.Invoke(DataContext as TransformManager);
            }
        }

        private void ButtonBezier3MoveUp(object sender, RoutedEventArgs e)
        {
            var ListBezier3 = (sender as Button).Tag as ListBox;
            int index = ListBezier3.SelectedIndex;
            if (ListBezier3.SelectedItems.Count == 1 && index > 0)
            {
                (DataContext as Bezier3Manager).MovePoint(index, index - 1);
            }
            PropertyUpdated?.Invoke(DataContext as TransformManager);
        }

        private void ButtonBezier3MoveDown(object sender, RoutedEventArgs e)
        {
            var ListBezier3 = (sender as Button).Tag as ListBox;
            int index = ListBezier3.SelectedIndex;
            if (ListBezier3.SelectedItems.Count == 1 && index < ListBezier3.Items.Count - 1)
            {
                (DataContext as Bezier3Manager).MovePoint(index, index + 1);
            }
            PropertyUpdated?.Invoke(DataContext as TransformManager);
        }

        private void ButtonIntersectionShowParameter(object sender, RoutedEventArgs e)
        {
            ParameterSpaceViewer psv = new ParameterSpaceViewer
            {
                DataContext = DataContext
            };
            psv.Show();
        }

        private void ButtonIntersectionGenCurve(object sender, RoutedEventArgs e)
        {
            var mw = Owner as MainWindow;
            var (points, curve) = (DataContext as IntersectionCurveManager).ToBezierInter();
            foreach (var point in points)
            {
                mw.AddManager(point);
            }
            mw.AddManager(curve);
        }

        private void ToggleIntersectionTrim(ToggleButton sender, ToggleButton twin)
        {
            if (sender.IsChecked == true)
            {
                twin.IsChecked = false;
            }
        }

        private void UpdateSurface(bool useP)
        {
            var icm = DataContext as IntersectionCurveManager;
            ISurface surface;
            if (useP)
            {
                surface = icm.P;
                SetTrimModeP();
            }
            else
            {
                surface = icm.Q;
                SetTrimModeQ();
            }
            surface.UpdateMesh();
            PropertyUpdated?.Invoke(DataContext as TransformManager);
        }

        private void SetTrimModeP()
        {
            var icm = DataContext as IntersectionCurveManager;
            if (ToggleIntersectionFirstA.IsChecked == true)
            {
                icm.TrimModeP = TrimMode.SideA;
            }
            else if (ToggleIntersectionFirstB.IsChecked == true)
            {
                icm.TrimModeP = TrimMode.SideB;
            }
            else
            {
                icm.TrimModeP = TrimMode.None;
            }
        }

        private void SetTrimModeQ()
        {
            var icm = DataContext as IntersectionCurveManager;
            if (ToggleIntersectionSecondA.IsChecked == true)
            {
                icm.TrimModeQ = TrimMode.SideA;
            }
            else if (ToggleIntersectionSecondB.IsChecked == true)
            {
                icm.TrimModeQ = TrimMode.SideB;
            }
            else
            {
                icm.TrimModeQ = TrimMode.None;
            }
        }
    }
}
