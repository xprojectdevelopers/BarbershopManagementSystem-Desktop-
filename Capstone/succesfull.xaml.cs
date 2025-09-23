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
using System.Windows.Shapes;

namespace Capstone
{
    /// <summary>
    /// Interaction logic for succesfull.xaml
    /// </summary>
    public partial class succesfull : Window
    {
        public succesfull()
        {
            InitializeComponent();
        }

        private void Successfully_Click(object sender, RoutedEventArgs e)
        {
            // Close the success popup window
            this.Close();
        }
    }

}
