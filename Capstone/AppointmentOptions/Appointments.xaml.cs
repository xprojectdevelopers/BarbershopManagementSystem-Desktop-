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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private NotificationService? _notificationService; // Notification service instance
        private AppointmentModel? _selectedAppointment;     // To store the currently selected appointment from the grid

        public Appointments()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
            AppointmentsGrid.SelectionChanged += AppointmentsGrid_SelectionChanged; // Hook up selection changed event
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            InitializeNotificationService(); // Initialize the notification service
            await LoadOnGoingAppointments();
        }

        private async Task InitializeSupabaseAsync()
        {
            string? supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string? supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                MessageBox.Show("⚠️ Supabase configuration missing in App.config!", "Configuration Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                supabase = new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false
                });

                await supabase.InitializeAsync();
                Console.WriteLine("✅ Supabase initialized successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to initialize Supabase: {ex.Message}", "Initialization Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Supabase init error: {ex}");
            }
        }

        private void InitializeNotificationService()
        {
            try
            {
                _notificationService = new NotificationService();
                Console.WriteLine("✅ Notification service initialized");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Failed to initialize notification service: {ex.Message}\n\nNotifications will not work.",
                              "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine($"Notification service init error: {ex}");
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
                        Console.WriteLine($"📋 {model.ReceiptCode} | {model.CustomerName} | Status: {model.Status} | " +
                                        $"Token: {(!string.IsNullOrEmpty(model.PushToken) ? "✅" : "❌")}");
                        appointments.Add(model);
                    }

                    TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                    if (TotalPages == 0) TotalPages = 1;

                    LoadPage(CurrentPage);
                    GeneratePaginationButtons();
                }
                else
                {
                    Console.WriteLine("ℹ️ No 'On Going' appointments found");
                    AppointmentsGrid.ItemsSource = null;
                    TotalPages = 1; // Ensure total pages is at least 1 even if empty
                    CurrentPage = 1; // Reset current page
                    GeneratePaginationButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading appointments: {ex.Message}", "Load Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Full error: {ex}");
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

            // Ensure pageNumber is within valid range
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > TotalPages) pageNumber = TotalPages;


            CurrentPage = pageNumber;

            var pageData = appointments
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            AppointmentsGrid.ItemsSource = null;
            AppointmentsGrid.ItemsSource = pageData;
            AppointmentsGrid.Items.Refresh();

            Console.WriteLine($"📄 Displaying page {pageNumber}/{TotalPages} with {pageData.Count} items");
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
                    GeneratePaginationButtons(); // Regenerate to update active button style
                };

                PaginationPanel.Children.Add(btn);
            }
        }

        // ============ NOTIFICATION METHODS (from second snippet) ============

        private async void TestNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationService == null)
            {
                MessageBox.Show("Notification service is not available.", "Service Unavailable",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ShowSimpleTestDialog();
        }

        private async void TestGridNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationService == null)
            {
                MessageBox.Show("Notification service is not available.", "Service Unavailable",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedAppointment == null)
            {
                MessageBox.Show("Please select an appointment first from the table.", "No Selection",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await SendTestNotificationForAppointment(_selectedAppointment);
        }

        private void AppointmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsGrid.SelectedItem is AppointmentModel selected)
            {
                _selectedAppointment = selected;
                Console.WriteLine($"📋 Selected: {selected.ReceiptCode} | Token: {selected.PushToken ?? "None"}");
            }
            else
            {
                _selectedAppointment = null; // Clear selection if nothing is selected
            }
        }

        private async Task ShowSimpleTestDialog()
        {
            var dialog = new Window()
            {
                Title = "Test Notification",
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var titleText = new TextBlock
            {
                Text = "🔔 Enter Push Token for Testing",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var tokenBox = new TextBox
            {
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = "ExponentPushToken[YOUR_TOKEN_HERE]",
                Margin = new Thickness(0, 0, 0, 10),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11
            };

            var instructionText = new TextBlock
            {
                Text = "Get this token from your React Native app console or database",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var sendButton = new Button
            {
                Content = "Send Test",
                Width = 100,
                Height = 35,
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };

            cancelButton.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };
            sendButton.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(sendButton);

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(tokenBox);
            stackPanel.Children.Add(instructionText);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            if (dialog.ShowDialog() == true)
            {
                var pushToken = tokenBox.Text.Trim();

                if (string.IsNullOrEmpty(pushToken) || !_notificationService!.IsValidPushToken(pushToken))
                {
                    MessageBox.Show("Please enter a valid Expo push token.\n\nFormat: ExponentPushToken[...]",
                                  "Invalid Token", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await SendTestNotification(pushToken);
            }
        }

        private async Task SendTestNotification(string pushToken)
        {
            try
            {
                Console.WriteLine("\n========================================");
                Console.WriteLine("🧪 SENDING TEST NOTIFICATION");
                Console.WriteLine("========================================");

                var result = await _notificationService!.SendTestNotification(pushToken);

                Console.WriteLine("========================================\n");

                if (result)
                {
                    MessageBox.Show("✅ Test notification sent successfully!\n\nCheck your mobile device.",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("❌ Failed to send test notification.\n\nCheck console output for details.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"💥 Error: {ex.Message}\n\nCheck console for details.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Exception: {ex}");
            }
        }

        private async Task SendTestNotificationForAppointment(AppointmentModel appointment)
        {
            try
            {
                if (string.IsNullOrEmpty(appointment.PushToken))
                {
                    MessageBox.Show($"No push token found for appointment {appointment.ReceiptCode}.\n\n" +
                                  "The customer needs to log in to the mobile app to register their device.",
                                  "No Token", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_notificationService!.IsValidPushToken(appointment.PushToken))
                {
                    MessageBox.Show($"Invalid push token format for appointment {appointment.ReceiptCode}.",
                                  "Invalid Token", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Console.WriteLine("\n========================================");
                Console.WriteLine($"🧪 TESTING NOTIFICATION FOR: {appointment.ReceiptCode}");
                Console.WriteLine("========================================");

                var result = await _notificationService.SendAppointmentNotification(
                    appointment.PushToken,
                    appointment.Id.ToString(),
                    appointment.CustomerName,
                    appointment.ReceiptCode,
                    "Test"); // Using "Test" status for the notification content here

                Console.WriteLine("========================================\n");

                if (result)
                {
                    MessageBox.Show($"✅ Test notification sent for appointment {appointment.ReceiptCode}!\n\n" +
                                  $"Customer: {appointment.CustomerName}\nCheck their mobile device.",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ Failed to send notification for {appointment.ReceiptCode}\n\n" +
                                  "Check console output for details.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"💥 Error: {ex.Message}\n\nCheck console for details.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Exception: {ex}");
            }
        }


        // ============ NAVIGATION METHODS ============
        // These methods are identical in both, so we keep one set.
        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            new Menu().Show(); // Assuming 'Menu' is a defined Window
            Close();
        }

        private void Book_Click(object sender, RoutedEventArgs e)
        {
            new Book_Appointment().Show(); // Assuming 'Book_Appointment' is a defined Window
            Close();
        }

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            new Appointment_Records().Show(); // Assuming 'Appointment_Records' is a defined Window
            Close();
        }

        private void Service_Click(object sender, RoutedEventArgs e)
        {
            new Manage_Services().Show(); // Assuming 'Manage_Services' is a defined Window
            Close();
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Notification history feature coming soon!", "Notifications",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🔄 Refreshing appointments...");
            await LoadOnGoingAppointments();
        }

        // ============ MODAL METHODS ============
        // These methods are identical in both, so we keep one set.
        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ShowModal(new ModalsSetting(), offsetX: 70); // Assuming 'ModalsSetting' is a defined Window
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
        // Using the more detailed confirmation and notification logic from the second snippet.

        private async void ApproveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
            {
                var result = MessageBox.Show(
                    $"Approve appointment for {selected.CustomerName}?\n\n" +
                    $"Receipt: {selected.ReceiptCode}\n" +
                    $"Date: {selected.Date:MMM dd, yyyy}\n" +
                    $"Time: {selected.Time}",
                    "Confirm Approval",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await UpdateAppointmentStatus(selected, "Approved");
                }
            }
        }

        private async void RejectAppointment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AppointmentModel selected)
            {
                var result = MessageBox.Show(
                    $"Decline appointment for {selected.CustomerName}?\n\n" +
                    $"Receipt: {selected.ReceiptCode}\n" +
                    $"Date: {selected.Date:MMM dd, yyyy}\n" +
                    $"Time: {selected.Time}",
                    "Confirm Decline",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // The second snippet used a specific DeclineAppointment modal,
                    // but the direct UpdateAppointmentStatus approach from the second snippet
                    // handles notifications and UI refresh directly. I'll stick to that
                    // for simplicity unless you specifically want the DeclineAppointment modal re-integrated.
                    await UpdateAppointmentStatus(selected, "Declined");
                }
            }
        }

        private async Task UpdateAppointmentStatus(AppointmentModel selected, string newStatus)
        {
            if (supabase == null || selected.Id == Guid.Empty)
            {
                MessageBox.Show("Unable to update appointment. Invalid data.", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Console.WriteLine($"\n🔄 Updating appointment {selected.ReceiptCode} to '{newStatus}'");

                var updated = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Id == selected.Id)
                    .Set(x => x.Status, newStatus)
                    .Update();

                if (updated.Models != null && updated.Models.Count > 0)
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
                            newStatus); // Pass the actual new status for template selection

                        if (notificationSent)
                        {
                            Console.WriteLine($"✅ Notification sent successfully");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Notification failed (check logs above)");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ No push token - skipping notification");
                    }

                    // UI update
                    appointments.Remove(selected); // Remove from the observable collection

                    TotalPages = (int)Math.Ceiling(appointments.Count / (double)PageSize);
                    if (TotalPages == 0 && appointments.Count > 0) TotalPages = 1; // Ensure 1 page if items exist
                    if (TotalPages == 0) TotalPages = 1; // Always at least 1 page

                    if (CurrentPage > TotalPages && TotalPages > 0)
                        CurrentPage = TotalPages; // Go to last page if current page is now out of bounds
                    if (TotalPages == 0) CurrentPage = 1; // Reset current page if no pages

                    LoadPage(CurrentPage);
                    GeneratePaginationButtons();

                    var message = $"✅ Appointment {newStatus}!\n\n" +
                                $"Receipt: {selected.ReceiptCode}\n" +
                                $"Customer: {selected.CustomerName}";

                    if (!string.IsNullOrEmpty(selected.PushToken))
                    {
                        message += notificationSent
                            ? "\n\n📱 Push notification sent to customer"
                            : "\n\n⚠️ Push notification failed (check console)";
                    }
                    else
                    {
                        message += "\n\nℹ️ No push token - notification not sent";
                    }

                    MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("⚠️ Failed to update appointment status. No rows were affected.", "Update Failed",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error updating appointment:\n\n{ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Full error: {ex}");
            }
        }


        // ============ NOTIFICATION SERVICE CLASS ============
        // This entire class is copied from the second snippet.
        public class NotificationService
        {
            private static readonly HttpClient _httpClient = new HttpClient();
            private readonly string _edgeFunctionUrl;
            private readonly string _supabaseAnonKey;
            private static bool _isInitialized = false;

            public NotificationService()
            {
                _edgeFunctionUrl = ConfigurationManager.AppSettings["EdgeFunctionUrl"]
                    ?? "https://gycwoawekmmompvholqr.supabase.co/functions/v1/sendNotification"; // Default if not in App.config

                _supabaseAnonKey = ConfigurationManager.AppSettings["SupabaseKey"];

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

            public async Task<bool> SendTestNotification(string pushToken)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(pushToken))
                    {
                        Console.WriteLine("❌ Invalid push token: Token is null or empty");
                        return false;
                    }

                    Console.WriteLine($"📱 Target token: {pushToken.Substring(0, Math.Min(30, pushToken.Length))}...");

                    var payload = new
                    {
                        expoPushToken = pushToken,
                        title = "Test from C# Desktop 🚀",
                        message = "Hello from Molave Street Barbers desktop application!",
                        data = new
                        {
                            type = "test_notification",
                            source = "csharp_desktop",
                            timestamp = DateTime.UtcNow.ToString("o")
                        },
                        sound = "default",
                        priority = "high",
                        channelId = "default"
                    };

                    return await SendNotificationRequest(payload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Test notification error: {ex.Message}");
                    return false;
                }
            }

            public async Task<bool> SendAppointmentNotification(string pushToken, string appointmentId, string customerName, string receiptCode, string status)
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

                    // Get notification template based on status
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

                    return await SendNotificationRequest(payload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Appointment notification error: {ex.Message}");
                    return false;
                }
            }

            private (string title, string message) GetNotificationTemplate(string status, string customerName, string receiptCode)
            {
                // Hardcoded templates based on your specification
                // The receiptCode and customerName are not currently used in the messages,
                // but are available if you want to customize them further.
                return status switch
                {
                    "Approved" => (
                        "Molave Street Barbers",
                        "Your appointment request has been approved! We'll see you soon at your selected time."
                    ),
                    "Declined" => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. Please contact us for more information."
                    ),
                    "Completed" => (
                        "Molave Street Barbers",
                        "Thank you for visiting Molave Street Barbers! Your appointment has been completed."
                    ),
                    "No Show" => (
                        "Molave Street Barbers",
                        "You missed your scheduled appointment. Please reschedule when convenient."
                    ),
                    "Test" => (
                        "Test Notification 🔔",
                        "This is a test notification from Molave Street Barbers."
                    ),
                    _ => (
                        "Molave Street Barbers",
                        $"Your appointment status has been updated to {status}."
                    )
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
                    Console.WriteLine($"🔑 Authorization: Bearer {_supabaseAnonKey.Substring(0, Math.Min(20, _supabaseAnonKey.Length))}..."); // Show only part of key

                    var response = await _httpClient.PostAsync(_edgeFunctionUrl, content);
                    responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"📥 Response Status: {(int)response.StatusCode} {response.StatusCode}");
                    Console.WriteLine($"📥 Response Body: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            Console.WriteLine("⚠️ Warning: Empty response from Edge Function");
                            // Depending on your Edge Function, this might still be a success or an error.
                            // Assuming success requires a specific JSON response.
                            return false;
                        }

                        var result = JObject.Parse(responseContent);
                        var success = result["success"]?.Value<bool>() ?? false;

                        if (success)
                        {
                            Console.WriteLine("✅ Notification sent successfully to Expo!"); // Corrected message
                            var dataField = result["data"] as JArray; // Access data as an array
                            if (dataField != null && dataField.Count > 0)
                            {
                                var ticketId = dataField[0]?["id"]?.ToString(); // Get the ID from the first object
                                if (!string.IsNullOrEmpty(ticketId))
                                {
                                    Console.WriteLine($"🎫 Expo ticket ID: {ticketId}");
                                }
                            }
                            return true;
                        }
                        else
                        {
                            // Improved error reporting from Edge Function response
                            var errors = result["errors"] as JArray;
                            if (errors != null && errors.Count > 0)
                            {
                                Console.WriteLine("❌ Edge function returned errors:");
                                foreach (var errorEntry in errors)
                                {
                                    Console.WriteLine($"   Error: {errorEntry["message"]}");
                                    Console.WriteLine($"   Details: {errorEntry["details"]}");
                                }
                            }
                            else
                            {
                                var error = result["error"]?.ToString() ?? "Unknown error";
                                var details = result["details"]?.ToString() ?? "";
                                Console.WriteLine($"❌ Edge function returned success=false");
                                Console.WriteLine($"   Error: {error}");
                                if (!string.IsNullOrEmpty(details))
                                {
                                    Console.WriteLine($"   Details: {details}");
                                }
                            }
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ HTTP Error: {response.StatusCode}");
                        Console.WriteLine($"   Response: {responseContent}");

                        try
                        {
                            var errorObj = JObject.Parse(responseContent);
                            var errorMsg = errorObj["error"]?.ToString()
                                ?? errorObj["message"]?.ToString()
                                ?? "No error message";
                            Console.WriteLine($"   Error details: {errorMsg}");
                        }
                        catch (JsonException)
                        {
                            // Response is not JSON, raw content already logged above
                            Console.WriteLine("   (Response was not valid JSON)");
                        }

                        return false;
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"❌ Network error: {ex.Message}");
                    Console.WriteLine($"   Check if Edge Function URL is correct: {_edgeFunctionUrl}");
                    return false;
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"❌ Request was cancelled before completion: {ex.Message}");
                    return false;
                }
                catch (TaskCanceledException ex) // Generic timeout exception
                {
                    Console.WriteLine($"❌ Request timeout: {ex.Message}");
                    Console.WriteLine($"   The request took longer than {_httpClient.Timeout.TotalSeconds} seconds");
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
                    Console.WriteLine($"💥 Unexpected error in SendNotificationRequest: {ex.GetType().Name} - {ex.Message}");
                    return false;
                }
            }

            public bool IsValidPushToken(string pushToken)
            {
                if (string.IsNullOrWhiteSpace(pushToken))
                    return false;

                return pushToken.StartsWith("ExponentPushToken[") &&
                       pushToken.EndsWith("]") &&
                       pushToken.Length > 20; // Minimum length check for a valid token structure
            }
        }

        // ============ MODEL DEFINITIONS ============
        // This is identical in both, so we keep one set.
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