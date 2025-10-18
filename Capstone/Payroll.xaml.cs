using Supabase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
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
    public partial class Payroll : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private Window currentModalWindow;
        public Payroll()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeSupabaseAsync()
        {
            string supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            supabase = new Client(supabaseUrl, supabaseKey, new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();

            employees = new ObservableCollection<BarbershopManagementSystem>();

            var result = await supabase.From<BarbershopManagementSystem>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            string employeeId = txtEmployeeID.Text.Trim();

            if (string.IsNullOrEmpty(employeeId))
            {
                ShowError(txtEmployeeIDError, "Employee ID is required");
                return;
            }

            // Clear Employee ID validation error when search is performed with valid ID
            txtEmployeeIDError.Visibility = Visibility.Collapsed;

            try
            {
                var result = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.Eid == employeeId)
                    .Get();

                if (result.Models.Count > 0)
                {
                    var employee = result.Models.First();
                    PopulateForm(employee);
                    HideAllErrorMessages();
                }
                else
                {
                    // Hide all validation errors before showing not found dialog
                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    // Open PurchaseOrders as a regular window
                    currentModalWindow = new notfound();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Subscribe to Closed event
                    currentModalWindow.Closed += ModalWindow_Closed;

                    // Show as regular window
                    currentModalWindow.Show();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching for employee: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }
    }
}
