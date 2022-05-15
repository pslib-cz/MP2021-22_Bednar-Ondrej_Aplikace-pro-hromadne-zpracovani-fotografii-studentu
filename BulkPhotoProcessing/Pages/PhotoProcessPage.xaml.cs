using BulkPhotoProcessing.Viewmodels;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BulkPhotoProcessing.Pages
{
    /// <summary>
    /// Interaction logic for PhotoProcessPage.xaml
    /// </summary>
    public partial class PhotoProcessPage : Page
    {
        public PhotoProcessPage()
        {
            InitializeComponent();
        }

        private void PersonSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((PhotoProcessingViewmodel)DataContext).ListSelectionChanged();
        }

        private void NameSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((PhotoProcessingViewmodel)DataContext).NameSelectionChanged();
        }
    }
}
