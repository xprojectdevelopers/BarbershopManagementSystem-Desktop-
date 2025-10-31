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
        private int PageSize = 10;
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
            await LoadOnGoingAppointments();
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

        private async Task LoadOnGoingAppointments()
        {
            if (supabase == null) return;

            try
            {
                // Get ONLY "On Going" appointments
                var result = await supabase
                    .From<AppointmentModel>()
                    .Filter("status", Operator.Equals, "On Going")
                    .Order("created_at", Ordering.Descending)
                    .Get();

                Console.WriteLine($"Found {result?.Models?.Count ?? 0} 'On Going' appointments");

                appointments.Clear();

                if (result?.Models != null && result.Models.Count > 0)
                {
                    foreach (var model in result.Models)
                    {
                        Console.WriteLine($"📋 Adding: {model.ReceiptCode} | {model.CustomerName} | Status: {model.Status}");
                        appointments.Add(model);
                    }

                    TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                    if (TotalPages == 0) TotalPages = 1;

                    LoadPage(CurrentPage);
                    GeneratePaginationButtons();
                }
                else
                {
                    AppointmentsGrid.ItemsSource = null;
                    // No message box - just show empty table
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading appointments: {ex.Message}");
                Console.WriteLine($"Full error: {ex}");
            }
        }

        private void LoadPage(int pageNumber)
        {
            if (appointments == null || appointments.Count == 0)
            {
                AppointmentsGrid.ItemsSource = null;
                return;
            }

            CurrentPage = pageNumber;

            var pageData = appointments
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            AppointmentsGrid.ItemsSource = null;
            AppointmentsGrid.ItemsSource = pageData;
            AppointmentsGrid.Items.Refresh();

            Console.WriteLine($"Displaying page {pageNumber} with {pageData.Count} items");
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
                    Background = i == CurrentPage ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Transparent,
                    Foreground = i == CurrentPage ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(1),
                    BorderBrush = System.Windows.Media.Brushes.Black
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

        // Navigation Methods
        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            new Menu().Show();
            Close();
        }

        private void Book_Click(object sender, RoutedEventArgs e)
        {
            new Book_Appointment().Show();
            Close();
        }

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            new Appointment_Records().Show();
            Close();
        }

        private void Service_Click(object sender, RoutedEventArgs e)
        {
            new Manage_Services().Show();
            Close();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadOnGoingAppointments();
        }

        // Modal Methods
        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ShowModal(new ModalsSetting(), offsetX: 70);
        }

        private void ShowModal(Window modal, int offsetX)
        {
            ModalOverlay.Visibility = Visibility.Visible;
            currentModalWindow = modal;
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - offsetX;
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
            currentModalWindow?.Close();
            e.Handled = true;
        }

        // Appointment Approval/Rejection
        private async void ApproveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
                await UpdateAppointmentStatus(selected, "Approved");
        }

        private async void RejectAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
                await UpdateAppointmentStatus(selected, "Declined");
        }

        private async Task UpdateAppointmentStatus(AppointmentModel selected, string newStatus)
        {
            if (supabase == null || selected.Id == Guid.Empty) return;

            try
            {
                // Update status in database to "Approved" or "Declined"
                var updated = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Id == selected.Id)
                    .Set(x => x.Status, newStatus)
                    .Update();

                if (updated.Models != null && updated.Models.Count > 0)
                {
                    // Remove from current view (but it stays in database with new status)
                    appointments.Remove(selected);

                    TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                    if (CurrentPage > TotalPages && TotalPages > 0)
                        CurrentPage = TotalPages;

                    LoadPage(CurrentPage);
                    GeneratePaginationButtons();

                    MessageBox.Show($"✅ Appointment {newStatus}!");
                }
                else
                {
                    MessageBox.Show("⚠️ No rows were updated.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error updating appointment: {ex.Message}");
            }
        }

        // Model definition
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