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
    /// Interaction logic for Inventory.xaml
    /// </summary>
    public partial class Inventory : Window
    {
        public Inventory()
        {
            InitializeComponent();
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }

        private void ManageItem_Click(object sender, RoutedEventArgs e)
        {
            ManageItem ManageItem = new ManageItem();
            ManageItem.Show();
            this.Close();
        }

        private void Purchased_Click(object sender, RoutedEventArgs e)
        {
            PayrollHistory PayrollHistory = new PayrollHistory();
            PayrollHistory.Show();
            this.Close();
        }

        private void Sales_Click(object sender, RoutedEventArgs e)
        {
            PayrollHistory PayrollHistory = new PayrollHistory();
            PayrollHistory.Show();
            this.Close();
        }
    }
}
