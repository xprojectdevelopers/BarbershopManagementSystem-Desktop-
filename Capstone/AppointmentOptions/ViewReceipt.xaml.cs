using System.Windows;

namespace Capstone.AppointmentOptions
{
    public partial class ViewReceipt : Window
    {
        public ViewReceipt()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}