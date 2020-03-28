using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

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
                GroupBezier3C0
            };
            typeMap = new Dictionary<Type, DisplayParams>
            {
                { typeof(TorusManager), new DisplayParams(GroupTorus, true, true, true) },
                { typeof(PointManager), new DisplayParams(null, true, false, false) },
                { typeof(Bezier3C0Manager), new DisplayParams(GroupBezier3C0, false, false, false) }
            };
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

        private void ButtonBezier3C0Add(object sender, RoutedEventArgs e)
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
                    (DataContext as Bezier3C0Manager).AddPoint(item as PointManager);
                }
                PropertyUpdated?.Invoke(DataContext as TransformManager);
            }
        }

        private void ButtonBezier3C0Remove(object sender, RoutedEventArgs e)
        {
            var selected = ListBezier3C0.SelectedItems;
            if (selected.Count > 0)
            {
                for (int i = selected.Count - 1; i >= 0; i--)
                {
                    (DataContext as Bezier3C0Manager).RemovePoint(selected[i] as PointManager);
                }
                PropertyUpdated?.Invoke(DataContext as TransformManager);
            }
        }

        private void ButtonBezier3C0MoveUp(object sender, RoutedEventArgs e)
        {
            int index = ListBezier3C0.SelectedIndex;
            if (ListBezier3C0.SelectedItems.Count == 1 && index > 0)
            {
                (DataContext as Bezier3C0Manager).MovePoint(index, index - 1);
            }
            PropertyUpdated?.Invoke(DataContext as TransformManager);
        }

        private void ButtonBezier3C0MoveDown(object sender, RoutedEventArgs e)
        {
            int index = ListBezier3C0.SelectedIndex;
            if (ListBezier3C0.SelectedItems.Count == 1 && index < ListBezier3C0.Items.Count - 1)
            {
                (DataContext as Bezier3C0Manager).MovePoint(index, index + 1);
            }
            PropertyUpdated?.Invoke(DataContext as TransformManager);
        }
    }
}
