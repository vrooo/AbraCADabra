using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for PatchWindow.xaml
    /// </summary>
    public partial class PatchWindow : Window
    {
        public int PatchCountX { get; set; } = 1;
        public int PatchCountZ { get; set; } = 1;
        public float DimX { get; set; } = 3.0f;
        public float DimZ { get; set; } = 3.0f;
        public PatchType PatchType { get; set; } = PatchType.Simple;

        public PatchWindow()
        {
            InitializeComponent();
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

    [ValueConversion(typeof(PatchType), typeof(bool))]
    public class SimplePatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PatchType val = (PatchType)value;
            return val == PatchType.Simple;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = (bool)value;
            return val ? PatchType.Simple : PatchType.Cylinder;
        }
    }

    [ValueConversion(typeof(PatchType), typeof(bool))]
    public class CylinderPatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PatchType val = (PatchType)value;
            return val == PatchType.Cylinder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = (bool)value;
            return val ? PatchType.Cylinder : PatchType.Simple;
        }
    }
}
