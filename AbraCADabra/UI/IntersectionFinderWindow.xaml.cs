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
        public static ISurface SelectedFirst { get; set; }
        public static ISurface SelectedSecond { get; set; }
        public static bool IsSingleSurface => SelectedFirst == SelectedSecond;
        public static bool UseCursorPosition { get; set; }

        public static int StartDims { get; set; } = 4;
        public static int StartMaxIterations { get; set; } = 30;
        public static float StartEps { get; set; } = 1e-6f;
        public static float StartPointEps { get; set; } = 1e-2f;
        public static float StartSelfDiff { get; set; } = 0.1f;
        public static float StartSelfDiffSquared => StartSelfDiff * StartSelfDiff;

        public static int CurveMaxPoints { get; set; } = 1000;
        public static int CurveMaxIterations { get; set; } = 30;
        public static float CurveStep { get; set; } = 0.1f;
        public static float CurveEps { get; set; } = 1e-6f;
        public static float CurveEndDist { get; set; } = 0.1f;

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
