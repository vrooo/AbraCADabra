using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for IntersectionFinderWindow.xaml
    /// </summary>
    public partial class IntersectionFinderWindow : Window
    {
        public ISurface SelectedFirst { get; set; }
        public ISurface SelectedSecond { get; set; }
        public bool IsSingleSurface => SelectedFirst == SelectedSecond;
        public bool UseCursorPosition { get; set; }
        public float CurveStep { get; set; } = 1.0f;
        public float Eps { get; set; } = 1e-6f;
        public float PointEps { get; set; } = 1e-2f;
        public int MaxIterations { get; set; } = 30;
        public int StartDims { get; set; } = 4;

        public IntersectionFinderWindow(List<ISurface> managers, List<ISurface> selected = null)
        {
            InitializeComponent();
            ComboSurface1.ItemsSource = managers;
            ComboSurface2.ItemsSource = managers;
            if (selected == null || !selected.Any())
            {
                ComboSurface1.SelectedItem = managers[0];
                ComboSurface2.SelectedItem = managers[1 % managers.Count];
            }
            else
            {
                ComboSurface1.SelectedItem = selected[0];
                ComboSurface2.SelectedItem = selected[1 % selected.Count];
            }
        }

        private void ButtonOKClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
