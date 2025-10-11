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
    public partial class Inventory : Window
    {
        private Window currentModalWindow;

        public Inventory()
        {
            InitializeComponent();
            // Add handler directly to the overlay
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
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
            // Show the overlay FIRST
            ModalOverlay.Visibility = Visibility.Visible;

            // Open PurchaseOrders as a regular window
            currentModalWindow = new PurchaseOrders();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Subscribe to Closed event
            currentModalWindow.Closed += ModalWindow_Closed;

            // Show as regular window
            currentModalWindow.Show();
        }

        private void Sales_Click(object sender, RoutedEventArgs e)
        {
            // Show the overlay FIRST
            ModalOverlay.Visibility = Visibility.Visible;

            // Open SaleItem as a regular window
            currentModalWindow = new SaleItem();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Subscribe to Closed event
            currentModalWindow.Closed += ModalWindow_Closed;

            // Show as regular window
            currentModalWindow.Show();
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private void ModalOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            // Close the modal window when clicking on the overlay
            if (currentModalWindow != null)
            {
                currentModalWindow.Close();
            }

            // Mark event as handled
            e.Handled = true;
        }
    }
}