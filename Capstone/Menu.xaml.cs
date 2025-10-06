using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Capstone
{
    public partial class Menu : Window
    {
        public Menu()
        {
            InitializeComponent();
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
    }
}
