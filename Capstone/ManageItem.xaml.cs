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
    /// Interaction logic for ManageItem.xaml
    /// </summary>
    public partial class ManageItem : Window
    {
        public ManageItem()
        {
            InitializeComponent();
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Inventory Inventory = new Inventory();
            Inventory.Show();
            this.Close();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            AddItem AddItem = new AddItem();
            AddItem.Show();
            this.Close();
        }
    }
}
