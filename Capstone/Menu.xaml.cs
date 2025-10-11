using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Capstone
{
    public partial class Menu : Window
    {
        private Window currentModalWindow;
        public Menu()
        {
            InitializeComponent();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        public class ComparisonConverter : IValueConverter
        {
            private static ComparisonConverter _instance;
            public static ComparisonConverter Instance => _instance ??= new ComparisonConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double actualValue && parameter is string parameterString)
                {
                    if (double.TryParse(parameterString, out double threshold))
                    {
                        return actualValue < threshold;
                    }
                }
                return false;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsNotification();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            // Position sa top-right corner, below the notification area
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 190;
            currentModalWindow.Top = this.Top + 135;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            // Position sa top-right corner, below the notification area
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 190;
            currentModalWindow.Top = this.Top + 135;

            currentModalWindow.Closed += ModalWindow_Closed;
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

        private void Customers_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Customers Customers = new Customers();
            Customers.Show();
            this.Hide();
        }

        private void Employees_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EMenu EMenu = new EMenu();
            EMenu.Show();
            this.Hide();
        }

        private void Payroll_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Payroll Payroll = new Payroll();
            Payroll.Show();
            this.Hide();
        }

        private void Inventory_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Inventory Inventory = new Inventory();
            Inventory.Show();
            this.Hide();
        }
        private void Appointments_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Appointments appointments = new Appointments();
            appointments.Show();
            this.Hide();
        }
    }
}
