using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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


    public partial class ManageItem : Window
    {
        private Window currentModalWindow;

        public ManageItem()
        {
            InitializeComponent();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Inventory Inventory = new Inventory();
            Inventory.Show();
            this.Close();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            // Open PurchaseOrders as a regular window
            currentModalWindow = new AddItem();
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
