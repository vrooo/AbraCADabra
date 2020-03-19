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
        List<GroupBox> groupBoxes;
        Dictionary<Type, GroupBox> typeMap;
        public PropertyWindow()
        {
            InitializeComponent();
            groupBoxes = new List<GroupBox>
            {
                GroupTorus
            };
            typeMap = new Dictionary<Type, GroupBox>
            {
                { typeof(TorusManager), GroupTorus }
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
                if (typeMap.TryGetValue(type, out GroupBox gb))
                {
                    gb.Visibility = Visibility.Visible;
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
    }
}
