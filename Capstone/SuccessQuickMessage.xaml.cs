using System.Windows;

namespace Capstone
{
    public partial class SuccessQuickMessage : Window
    {
        public SuccessQuickMessage()
        {
            InitializeComponent();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Close the popup when "Continue" is clicked
        }
    }
}
