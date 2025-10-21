using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone.AppointmentOptions
{
    public partial class Appointments : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<AppointmentModel> appointments = new();
        private int CurrentPage = 1;
        private int PageSize = 5;
        private int TotalPages = 1;
        private Window? currentModalWindow;

        public Appointments()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadAppointments();
        }

        private async Task InitializeSupabaseAsync()
        {
            string? supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string? supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                MessageBox.Show("⚠️ Supabase configuration missing in App.config!");
                return;
            }

            supabase = new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        private async Task LoadAppointments()
        {
            if (supabase == null) return;

            try
            {
                var result = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Status == "On Going")
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                appointments = new ObservableCollection<AppointmentModel>(result.Models);

                TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                LoadPage(1);
                GeneratePaginationButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading appointments: {ex.Message}");
            }
        }

        private void LoadPage(int pageNumber)
        {
            CurrentPage = pageNumber;

            var pageData = appointments
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            AppointmentsGrid.ItemsSource = pageData;
        }

        private void GeneratePaginationButtons()
        {
            PaginationPanel.Children.Clear();

            for (int i = 1; i <= TotalPages; i++)
            {
                Button btn = new Button
                {
                    Content = i.ToString(),
                    Margin = new Thickness(5),
                    Padding = new Thickness(10, 5, 10, 5),
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontWeight = (i == CurrentPage) ? FontWeights.Bold : FontWeights.Normal,
                    Cursor = Cursors.Hand
                };

                int pageNum = i;
                btn.Click += (s, e) =>
                {
                    LoadPage(pageNum);
                    GeneratePaginationButtons();
                };

                PaginationPanel.Children.Add(btn);
            }
        }

        // ✅ Navigation handlers
        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }

        private void Book_Click(object sender, RoutedEventArgs e)
        {
            Book_Appointment bookWindow = new Book_Appointment();
            bookWindow.Show();
            this.Close();
        }

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            Appointment_Records recordWindow = new Appointment_Records();
            recordWindow.Show();
            this.Close();
        }

        private void Service_Click(object sender, RoutedEventArgs e)
        {
            Manage_Services serviceWindow = new Manage_Services();
            serviceWindow.Show();
            this.Close();
        }

        // Refresh button click
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAppointments();
            MessageBox.Show("🔄 Appointments refreshed!");
        }

        // Test notification button click
        private void TestNotificationButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🔔 Test notification sent!");
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
                currentModalWindow.Close();

            e.Handled = true;
        }

        // ✅ Approve / Reject appointment handlers
        private async void ApproveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
            {
                await UpdateAppointmentStatus(selected, "Approved");
            }
        }

        private async void RejectAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
            {
                await UpdateAppointmentStatus(selected, "Declined");
            }
        }

        private async Task UpdateAppointmentStatus(AppointmentModel selected, string newStatus)
        {
            if (supabase == null || selected.Id == Guid.Empty) return;

            try
            {
                // Create a new instance with updated status
                var updatedAppointment = new AppointmentModel
                {
                    Id = selected.Id,
                    CustomerId = selected.CustomerId,
                    CustomerName = selected.CustomerName,
                    ContactNumber = selected.ContactNumber,
                    CustomerBadge = selected.CustomerBadge,
                    Service = selected.Service,
                    Barber = selected.Barber,
                    Date = selected.Date,
                    Time = selected.Time,
                    Subtotal = selected.Subtotal,
                    AppointmentFee = selected.AppointmentFee,
                    Total = selected.Total,
                    PaymentMethod = selected.PaymentMethod,
                    ReceiptCode = selected.ReceiptCode,
                    Status = newStatus,  // ✅ Updated status
                    PushToken = selected.PushToken,
                    CreatedAt = selected.CreatedAt
                };

                var updated = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Id == selected.Id)
                    .Update(updatedAppointment);

                if (updated.Models != null && updated.Models.Count > 0)
                {
                    // Remove from the current list and refresh
                    appointments.Remove(selected);

                    // Recalculate pagination
                    TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);

                    // Adjust current page if needed
                    if (CurrentPage > TotalPages && TotalPages > 0)
                    {
                        CurrentPage = TotalPages;
                    }

                    LoadPage(CurrentPage);
                    GeneratePaginationButtons();

                    MessageBox.Show($"✅ Appointment {newStatus}!\nID: {selected.Id}");
                }
                else
                {
                    MessageBox.Show("⚠️ Failed to update appointment. No rows affected.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error updating appointment: {ex.Message}\n\nDetails: {ex.InnerException?.Message}");
            }
        }

        // 🔹 Supabase Appointment Model
        [Table("appointment_sched")]
        public class AppointmentModel : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("customer_id")] public string CustomerId { get; set; } = string.Empty;
            [Column("customer_name")] public string CustomerName { get; set; } = string.Empty;
            [Column("contact_number")] public string? ContactNumber { get; set; }
            [Column("customer_badge")] public string? CustomerBadge { get; set; }
            [Column("service_id")] public string Service { get; set; } = string.Empty;
            [Column("barber_id")] public string Barber { get; set; } = string.Empty;
            [Column("sched_date")] public DateTime? Date { get; set; }
            [Column("sched_time")] public TimeSpan? Time { get; set; }
            [Column("subtotal")] public decimal? Subtotal { get; set; }
            [Column("appointment_fee")] public decimal? AppointmentFee { get; set; }
            [Column("total")] public decimal? Total { get; set; }
            [Column("payment_method")] public string PaymentMethod { get; set; } = string.Empty;
            [Column("receipt_code")] public string ReceiptCode { get; set; } = string.Empty;
            [Column("status")] public string Status { get; set; } = "On Going";
            [Column("push_token")] public string? PushToken { get; set; }
            [Column("created_at")] public DateTime? CreatedAt { get; set; }
        }
    }
}
