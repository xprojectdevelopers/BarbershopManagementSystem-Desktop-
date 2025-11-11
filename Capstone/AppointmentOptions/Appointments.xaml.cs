using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone.AppointmentOptions
{
    public partial class Appointments : Window
    {
        private Client? supabase;
        private readonly ObservableCollection<AppointmentModel> appointments = new();
        private int CurrentPage = 1;
        private int PageSize = 5;
        private int TotalPages = 1;
        private Window? currentModalWindow;
        private NotificationService? _notificationService;
        private AppointmentModel? _selectedAppointment;

        public Appointments()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;

            // Bind the ObservableCollection to the grid (if not bound in XAML)
            AppointmentsGrid.ItemsSource = appointments;
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            InitializeNotificationService();
            await LoadOnGoingAppointments();
        }

        private async Task InitializeSupabaseAsync()
        {
            string? supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string? supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];
            string? supabaseServiceKey = ConfigurationManager.AppSettings["SupabaseServiceKey"];

            // Use service key if available, otherwise fall back to anon key
            string effectiveKey = !string.IsNullOrEmpty(supabaseServiceKey) ? supabaseServiceKey! : supabaseKey!;

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(effectiveKey))
            {
                Console.WriteLine("⚠️ Supabase configuration missing in App.config (SupabaseUrl or SupabaseKey)");
                return;
            }

            try
            {
                supabase = new Client(supabaseUrl, effectiveKey, new Supabase.SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false
                });

                await supabase.InitializeAsync();
                Console.WriteLine($"✅ Supabase initialized successfully with {(string.IsNullOrEmpty(supabaseServiceKey) ? "ANON key" : "SERVICE ROLE key")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize Supabase: {ex.Message}");
                Console.WriteLine(ex);
                supabase = null;
            }
        }

        private void InitializeNotificationService()
        {
            try
            {
                _notificationService = new NotificationService(supabase);
                Console.WriteLine("✅ Notification service initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to initialize notification service: {ex.Message}");
                Console.WriteLine(ex);
                _notificationService = null;
            }
        }

        // ============ APPOINTMENT LOADING METHODS ============

        private async Task LoadOnGoingAppointments()
        {
            if (supabase == null)
            {
                Console.WriteLine("⚠️ Supabase client not initialized");
                return;
            }

            try
            {
                Console.WriteLine("📥 Loading 'On Going' appointments...");

                // NOTE: method names vary with Supabase C# client versions.
                // This example uses .From<T>().Filter(...) as in your original code.
                // If your client uses .From<T>().Where(...) or .Table<T>().Select(...).Eq(...), adapt accordingly.

                var result = await supabase
                    .From<AppointmentModel>()
                    .Filter("status", Operator.Equals, "On Going")
                    .Order("created_at", Ordering.Descending)
                    .Get();

                int count = result?.Models?.Count ?? 0;
                Console.WriteLine($"Found {count} 'On Going' appointments");

                // Update ObservableCollection on UI thread to avoid cross-thread issues
                await Dispatcher.InvokeAsync(() =>
                {
                    appointments.Clear();
                });

                if (result?.Models != null && result.Models.Count > 0)
                {
                    foreach (var model in result.Models)
                    {
                        Console.WriteLine($"📋 {model.ReceiptCode} | {model.CustomerName} | Status: {model.Status} | " +
                                          $"Token: {(string.IsNullOrEmpty(model.PushToken) ? "❌" : "✅")}");

                        // Add on UI thread
                        await Dispatcher.InvokeAsync(() => appointments.Add(model));
                    }
                }
                else
                {
                    Console.WriteLine("ℹ️ No 'On Going' appointments found");
                }

                // Pagination calculation
                await Dispatcher.InvokeAsync(() =>
                {
                    TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                    if (TotalPages < 1) TotalPages = 1;
                    if (CurrentPage > TotalPages) CurrentPage = TotalPages;

                    LoadPage(CurrentPage);
                    GeneratePaginationButtons();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading appointments: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private void LoadPage(int pageNumber)
        {
            if (appointments == null || appointments.Count == 0)
            {
                AppointmentsGrid.ItemsSource = null;
                Console.WriteLine("ℹ️ No appointments to display");
                return;
            }

            CurrentPage = Math.Max(1, Math.Min(pageNumber, TotalPages));

            var pageData = appointments
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            AppointmentsGrid.ItemsSource = pageData;
            AppointmentsGrid.Items.Refresh();

            Console.WriteLine($"📄 Displaying page {CurrentPage}/{TotalPages} with {pageData.Count} items");
        }

        private void GeneratePaginationButtons()
        {
            PaginationPanel.Children.Clear();

            for (int i = 1; i <= TotalPages; i++)
            {
                var btn = new Button
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
                btn.Click += async (s, e) =>
                {
                    LoadPage(pageNum);
                    GeneratePaginationButtons();
                    await Task.CompletedTask;
                };

                PaginationPanel.Children.Add(btn);
            }
        }

        // ============ NOTIFICATION METHODS ============

        private void AppointmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsGrid.SelectedItem is AppointmentModel selected)
            {
                _selectedAppointment = selected;
                Console.WriteLine($"📋 Selected: {selected.ReceiptCode} | Token: {selected.PushToken ?? "None"}");
            }
        }

        // ============ NAVIGATION METHODS ============

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

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            // Notification history feature - no alert
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🔄 Refreshing appointments...");
            await LoadOnGoingAppointments();
        }

        // ============ MODAL METHODS ============

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

        // ============ APPOINTMENT APPROVAL/REJECTION ============

        private async void ApproveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
            {
                // Directly approve without confirmation dialog
                await UpdateAppointmentStatus(selected, "Approved");
            }
        }

        private void RejectAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
            {
                // Show the modal overlay
                ModalOverlay.Visibility = Visibility.Visible;

                var declineModal = new DeclineAppointment
                {
                    SelectedAppointment = selected,
                    OnConfirmDecline = (appointment, reason) =>
                    {
                        // remove locally (DB update should happen inside DeclineAppointment)
                        Dispatcher.Invoke(() =>
                        {
                            appointments.Remove(appointment);

                            TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                            if (CurrentPage > TotalPages && TotalPages > 0)
                                CurrentPage = TotalPages;

                            LoadPage(CurrentPage);
                            GeneratePaginationButtons();
                        });
                    }
                };

                currentModalWindow = declineModal;
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();
            }
        }

        private async Task UpdateAppointmentStatus(AppointmentModel selected, string newStatus)
        {
            if (supabase == null || selected == null || selected.Id == Guid.Empty)
            {
                Console.WriteLine("Unable to update appointment. Invalid data.");
                return;
            }

            try
            {
                Console.WriteLine($"\n🔄 Updating appointment {selected.ReceiptCode} to '{newStatus}'");

                // Approach A: common "Set" -> Update pattern (your original)
                // If it works with your Supabase client, keep it. Otherwise use Approach B below.
                var updateAttempt = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Id == selected.Id)
                    .Set(x => x.Status, newStatus)
                    .Update();

                // Approach B (alternate) - construct partial object and call Update (works in some clients):
                // var partial = new AppointmentModel { Id = selected.Id, Status = newStatus };
                // var updateAttempt = await supabase.From<AppointmentModel>().Update(partial);

                if (updateAttempt?.Models != null && updateAttempt.Models.Count > 0)
                {
                    Console.WriteLine($"✅ Database updated successfully");

                    bool notificationSent = false;

                    if (!string.IsNullOrEmpty(selected.PushToken) && _notificationService != null)
                    {
                        Console.WriteLine($"📨 Sending {newStatus} notification...");
                        notificationSent = await _notificationService.SendAppointmentNotification(
                            selected.PushToken,
                            selected.Id.ToString(),
                            selected.CustomerName,
                            selected.ReceiptCode,
                            newStatus,
                            selected.CustomerId);

                        Console.WriteLine(notificationSent ? "✅ Notification sent successfully" : "⚠️ Notification failed");
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ No push token - skipping notification");
                    }

                    // Remove from in-memory list (UI thread)
                    await Dispatcher.InvokeAsync(() =>
                    {
                        appointments.Remove(selected);

                        TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                        if (TotalPages == 0) TotalPages = 1;
                        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

                        LoadPage(CurrentPage);
                        GeneratePaginationButtons();
                    });

                    // Only show success modal for "Approved"
                    if (newStatus == "Approved")
                    {
                        ModalOverlay.Visibility = Visibility.Visible;
                        var successModal = new AppointmentRequestApproved()
                        {
                            Owner = this,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        successModal.Closed += ModalWindow_Closed;
                        currentModalWindow = successModal;
                        successModal.Show();
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Failed to update appointment status - no models returned");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating appointment: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        // ============ NOTIFICATION SERVICE CLASS ============

        public class NotificationService
        {
            private static readonly HttpClient _httpClient = new HttpClient();
            private readonly string _edgeFunctionUrl;
            private readonly string _supabaseAnonKey;
            private static bool _isInitialized = false;
            private readonly Client? _supabase;

            public NotificationService(Client? supabase)
            {
                _supabase = supabase;
                _edgeFunctionUrl = ConfigurationManager.AppSettings["EdgeFunctionUrl"]
                    ?? "https://gycwoawekmmompvholqr.supabase.co/functions/v1/sendNotification";

                _supabaseAnonKey = ConfigurationManager.AppSettings["SupabaseKey"] ?? string.Empty;

                if (string.IsNullOrEmpty(_supabaseAnonKey))
                {
                    throw new InvalidOperationException("⚠️ SupabaseKey not found in App.config");
                }

                if (!_isInitialized)
                {
                    _httpClient.Timeout = TimeSpan.FromSeconds(30);
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _supabaseAnonKey);
                    _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseAnonKey);
                    _isInitialized = true;
                }
            }

            public async Task<bool> SendAppointmentNotification(string pushToken, string appointmentId, string customerName, string receiptCode, string status, string userId)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(pushToken))
                    {
                        Console.WriteLine("❌ Invalid push token for appointment notification");
                        return false;
                    }

                    Console.WriteLine($"📨 Sending {status} notification for appointment: {appointmentId}");
                    Console.WriteLine($"📱 Target token: {pushToken.Substring(0, Math.Min(30, pushToken.Length))}...");

                    var (title, message) = GetNotificationTemplate(status, customerName, receiptCode);

                    var payload = new
                    {
                        expoPushToken = pushToken,
                        title = title,
                        message = message,
                        data = new
                        {
                            type = $"appointment_{status.ToLower().Replace(" ", "_")}",
                            appointment_id = appointmentId,
                            status = status,
                            source = "csharp_desktop",
                            timestamp = DateTime.UtcNow.ToString("o")
                        },
                        sound = "default",
                        priority = "high",
                        channelId = "default"
                    };

                    var success = await SendNotificationRequest(payload);

                    if (_supabase != null)
                    {
                        await SafeInsertNotificationToLoader(title, message, receiptCode, userId);
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Appointment notification error: {ex.Message}");
                    Console.WriteLine(ex);
                    return false;
                }
            }

            private async Task SafeInsertNotificationToLoader(string title, string description, string receiptId, string userId)
            {
                try
                {
                    if (_supabase == null)
                    {
                        Console.WriteLine("⚠️ Supabase client not available for notification_loader insertion");
                        return;
                    }

                    if (!Guid.TryParse(userId, out Guid userGuid))
                    {
                        Console.WriteLine($"⚠️ Invalid user_id format: '{userId}' - must be a valid UUID");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(title)) title = "(No title)";

                    if (title.Length > 255) title = title.Substring(0, 255);
                    if (!string.IsNullOrWhiteSpace(description) && description.Length > 1000)
                        description = description.Substring(0, 1000);

                    if (string.IsNullOrWhiteSpace(receiptId)) receiptId = "N/A";

                    var notification = new NotificationLoaderModel
                    {
                        UserId = userGuid,
                        ReceiptId = receiptId,
                        Title = title,
                        Description = description,
                        CreatedAt = DateTime.UtcNow,
                        Read = false  // Explicitly set to false
                    };

                    var result = await _supabase
                        .From<NotificationLoaderModel>()
                        .Insert(notification);

                    if (result?.Models != null && result.Models.Count > 0)
                    {
                        var insertedNotification = result.Models[0];
                        Console.WriteLine($"✅ Notification inserted successfully into notification_loader (ID: {insertedNotification.Id}, Read: {insertedNotification.Read})");
                    }
                    else if (result?.ResponseMessage?.IsSuccessStatusCode == true)
                    {
                        Console.WriteLine($"✅ Notification inserted successfully (HTTP success)");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Failed to insert notification into notification_loader - no models returned");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error inserting into notification_loader: {ex.Message}");
                    Console.WriteLine(ex);
                }
            }

            private (string title, string message) GetNotificationTemplate(string status, string customerName, string receiptCode)
            {
                return status switch
                {
                    "Approved" => ("Molave Street Barbers", "Your appointment request has been approved! We'll see you soon at your selected time."),
                    "Declined" => ("Molave Street Barbers", "Your appointment request has been declined. Please contact us for more information."),
                    "Completed" => ("Molave Street Barbers", "Thank you for visiting Molave Street Barbers! Your appointment has been completed."),
                    "No Show" => ("Molave Street Barbers", "You missed your scheduled appointment. Please reschedule when convenient."),
                    _ => ("Molave Street Barbers", $"Your appointment status has been updated to {status}.")
                };
            }

            private async Task<bool> SendNotificationRequest(object payload)
            {
                string responseContent = string.Empty;

                try
                {
                    var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    Console.WriteLine($"📦 Payload:\n{json}");

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    Console.WriteLine($"📤 Sending to: {_edgeFunctionUrl}");

                    var response = await _httpClient.PostAsync(_edgeFunctionUrl, content);
                    responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"📥 Response Status: {(int)response.StatusCode} {response.StatusCode}");
                    Console.WriteLine($"📥 Response Body: {responseContent}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ HTTP Error: {response.StatusCode}");
                        try
                        {
                            var errorObj = JObject.Parse(responseContent);
                            var errorMsg = errorObj["error"]?.ToString() ?? errorObj["message"]?.ToString() ?? "No error message";
                            Console.WriteLine($"   Error details: {errorMsg}");
                        }
                        catch { }
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        Console.WriteLine("⚠️ Warning: Empty response from Edge Function");
                        return false;
                    }

                    var result = JObject.Parse(responseContent);
                    var success = result["success"]?.Value<bool>() ?? false;

                    if (success)
                    {
                        Console.WriteLine("✅ Notification sent successfully!");
                        return true;
                    }
                    else
                    {
                        var error = result["error"]?.ToString() ?? "Unknown error";
                        var details = result["details"]?.ToString() ?? "";
                        Console.WriteLine($"❌ Edge function returned success=false");
                        Console.WriteLine($"   Error: {error}");
                        if (!string.IsNullOrEmpty(details)) Console.WriteLine($"   Details: {details}");
                        return false;
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"❌ Network error: {ex.Message}");
                    return false;
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine($"❌ Request timeout: {ex.Message}");
                    return false;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"❌ JSON parsing error: {ex.Message}");
                    Console.WriteLine($"   Response content: {responseContent}");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Unexpected error: {ex.GetType().Name} - {ex.Message}");
                    Console.WriteLine(ex);
                    return false;
                }
            }

            public bool IsValidPushToken(string pushToken)
            {
                if (string.IsNullOrWhiteSpace(pushToken))
                    return false;

                return pushToken.StartsWith("ExponentPushToken[") &&
                       pushToken.EndsWith("]") &&
                       pushToken.Length > 20;
            }
        }

        // ============ MODEL DEFINITIONS ============

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

        [Table("notification_loader")]
        public class NotificationLoaderModel : BaseModel
        {
            [PrimaryKey("id", true)]
            public int Id { get; set; }

            [Column("user_id")]
            public Guid UserId { get; set; }

            [Column("receipt_id")]
            public string? ReceiptId { get; set; }

            [Column("title")]
            public string Title { get; set; } = string.Empty;

            [Column("description")]
            public string? Description { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("read")]
            public bool? Read { get; set; } = false;  // Default value set to false
        }
    }
}