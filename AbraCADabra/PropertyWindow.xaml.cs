using System.Windows;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for PropertyWindow.xaml
    /// </summary>
    public partial class PropertyWindow : Window
    {
        public PropertyWindow()
        {
            InitializeComponent();
        }

        public delegate void PropertyUpdatedEventHandler(MeshManager context);
        public event PropertyUpdatedEventHandler PropertyUpdated;

        private void SliderUpdate(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DataContext != null)
            {
                PropertyUpdated?.Invoke(DataContext as MeshManager);
            }
        }

        private void ContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // update layout
        }
    }
}
