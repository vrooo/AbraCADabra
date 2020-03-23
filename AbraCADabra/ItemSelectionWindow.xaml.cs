using System.Collections;
using System.Windows;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for ItemSelectionWindow.xaml
    /// </summary>
    public partial class ItemSelectionWindow : Window
    {
        public IList SelectedItems => ListItems.SelectedItems;
        public ItemSelectionWindow()
        {
            InitializeComponent();
        }

        private void ButtonAdd(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
