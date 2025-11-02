using Microsoft.IdentityModel.Tokens;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Capstone.AppointmentOptions
{
    /// <summary>
    /// Interaction logic for Book_Appointment.xaml
    /// </summary>
    public partial class Book_Appointment : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private Window currentModalWindow;

        public Book_Appointment()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData(); // Initialize when window is loaded
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
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

            // Fetch data from Supabase
            var result = await supabase.From<BarbershopManagementSystem>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Appointments Appointments = new Appointments();
            Appointments.Show();
            this.Close();
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsNotification();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 95;
            currentModalWindow.Top = this.Top + 110;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 95;
            currentModalWindow.Top = this.Top + 110;

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
            if (currentModalWindow != null)
            {
                currentModalWindow.Close();
            }
            e.Handled = true;
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ViewReceipt();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void btnGeneratedNum_Click(object sender, RoutedEventArgs e)
        {
            string prefix = "MSB";
            int nextNumber = 1;

            // Find all items with receipt codes starting with "MSB-"
            var existingReceiptCodes = employees
                .Where(emp => !string.IsNullOrEmpty(emp.ReceiptCode) && emp.ReceiptCode.StartsWith(prefix))
                .Select(emp => emp.ReceiptCode)
                .ToList();

            if (existingReceiptCodes.Any())
            {
                // Extract the last numeric part from each receipt code
                var maxNumber = existingReceiptCodes
                    .Select(code =>
                    {
                        var parts = code.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int num))
                            return num;
                        return 0;
                    })
                    .Max();

                nextNumber = maxNumber + 1;
            }

            // Format as MSB-0001, MSB-0002, etc.
            string newReceiptCode = $"{prefix}-{nextNumber:D4}";

            // Display the new receipt code in your TextBox
            txtItemID.Text = newReceiptCode;
        }

        [Table("appointment_sched")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }
            [Column("customer_name")] public string CustomerName { get; set; } = string.Empty;
            [Column("service_id")] public string Service { get; set; } = string.Empty;
            [Column("barber_id")] public string Barber { get; set; } = string.Empty;
            [Column("sched_date")] public string Date { get; set; } = string.Empty;
            [Column("sched_time")] public TimeSpan? Time { get; set; }
            [Column("total")] public decimal? Total { get; set; }
            [Column("payment_method")] public string PaymentMethod { get; set; } = string.Empty;
            [Column("status")] public string Status { get; set; } = "On Going";
            [Column("payment_status")] public string PaymentStatus { get; set; } = string.Empty;
            [Column("receipt_code")] public string ReceiptCode { get; set; } = string.Empty;
        }
    }
}
