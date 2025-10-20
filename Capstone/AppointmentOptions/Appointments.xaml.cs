using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    public partial class Appointments : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<AppointmentModel> appointments = new ObservableCollection<AppointmentModel>();
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly HttpClient _edgeClient = new HttpClient();

        public Appointments()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        // ✅ Custom Debug Logger
        private void Log(string type, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug.WriteLine($"[{timestamp}] [{type}] {message}");
        }

        private async Task InitializeData()
        {
            string? supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string? supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                MessageBox.Show("❌ Supabase configuration is missing in App.config!", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log("ERROR", "Supabase URL or Key is missing in App.config.");
                return;
            }

            try
            {
                Log("INFO", "Initializing Supabase client...");
                supabase = new Supabase.Client(supabaseUrl, supabaseKey);
                await supabase.InitializeAsync();
                Log("SUCCESS", "Connected to Supabase successfully.");

                // Test database access
                await ValidateTableAccess();

                await LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to connect to database:\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log("ERROR", $"Supabase initialization failed: {ex}");
            }
        }

        // ✅ TEST TABLE ACCESS
        private async Task<bool> ValidateTableAccess()
        {
            try
            {
                Log("VALIDATION", "Testing database table access...");

                // Test appointments table
                var appointmentsTest = await supabase
                    .From<AppointmentModel>()
                    .Select("id")
                    .Limit(1)
                    .Get();
                Log("VALIDATION", $"Appointments table: {(appointmentsTest.Models.Any() ? "✅ ACCESSIBLE" : "⚠️ EMPTY")}");

                // Test customer_profiles table
                try
                {
                    var customersTest = await supabase
                        .From<CustomerProfile>()
                        .Select("id")
                        .Limit(1)
                        .Get();
                    Log("VALIDATION", $"Customer profiles table: {(customersTest.Models.Any() ? "✅ ACCESSIBLE" : "⚠️ EMPTY")}");
                }
                catch (Exception custEx)
                {
                    Log("VALIDATION", $"❌ Customer profiles table error: {custEx.Message}");
                    MessageBox.Show($"Customer profiles table access failed. This may affect push notifications.\n\nError: {custEx.Message}",
                                  "Table Access Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Test notifications table
                try
                {
                    var notificationsTest = await supabase
                        .From<Notification>()
                        .Select("id")
                        .Limit(1)
                        .Get();
                    Log("VALIDATION", $"Notifications table: {(notificationsTest.Models.Any() ? "✅ ACCESSIBLE" : "⚠️ EMPTY")}");
                }
                catch (Exception notifEx)
                {
                    Log("VALIDATION", $"❌ Notifications table error: {notifEx.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Table validation failed: {ex.Message}");
                return false;
            }
        }

        private async Task LoadAppointments()
        {
            if (supabase == null)
            {
                Log("ERROR", "Supabase client is null — cannot load appointments.");
                MessageBox.Show("Database connection not established.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Log("INFO", "Fetching appointments with 'On Going' status from database...");

                var response = await supabase
                    .From<AppointmentModel>()
                    .Filter("status", Operator.Equals, "On Going")
                    .Order("created_at", Ordering.Descending)
                    .Get();

                appointments.Clear();
                foreach (var appointment in response.Models)
                {
                    appointments.Add(appointment);
                    Log("APPOINTMENT", $"Loaded: {appointment.ReceiptCode} - {appointment.CustomerName} - PushToken: {(string.IsNullOrEmpty(appointment.PushToken) ? "❌ NULL" : "✅ " + appointment.PushToken.Substring(0, 20) + "...")}");
                }

                if (AppointmentsGrid.ItemsSource == null)
                {
                    AppointmentsGrid.ItemsSource = appointments;
                }

                Log("SUCCESS", $"Loaded {appointments.Count} 'On Going' appointment(s) from database.");

                if (appointments.Count == 0)
                {
                    Log("INFO", "No 'On Going' appointments found.");
                    MessageBox.Show("No pending appointments found.\n\nOnly appointments with 'On Going' status are displayed.",
                                  "No Pending Appointments", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Failed to load appointments: {ex.Message}");
                MessageBox.Show($"Failed to load appointments:\n\n{ex.Message}\n\nCheck Output window for details.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ APPROVE BUTTON - UPDATED WITH MODAL
        private async void ApproveAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AppointmentModel appointment)
            {
                Log("DEBUG", $"Approve button clicked - Appointment ID: {appointment.Id}, Customer: {appointment.CustomerName}");

                if (appointment.Status == "Approved" || appointment.Status == "Completed")
                {
                    MessageBox.Show("This appointment is already approved or completed.", "Cannot Approve", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show the modal instead of MessageBox
                ShowApproveModal(appointment);
            }
            else
            {
                Log("ERROR", "Button DataContext is not AppointmentModel or is null");
            }
        }

        // ✅ SHOW APPROVAL MODAL
        private void ShowApproveModal(AppointmentModel appointment)
        {
            try
            {
                // Create and configure the modal
                var approveModal = new Appointment_Approve(appointment)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                // Handle the modal result
                approveModal.Closed += async (s, args) =>
                {
                    if (approveModal.DialogResult == true)
                    {
                        // User confirmed approval
                        await ProcessAppointmentApproval(appointment, approveModal.AdditionalNotes);
                    }
                    else
                    {
                        Log("INFO", $"Approval cancelled in modal for appointment ID: {appointment.Id}");
                    }
                };

                approveModal.ShowDialog();
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Error showing approval modal: {ex.Message}");
                MessageBox.Show($"Failed to open approval dialog:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ PROCESS APPOINTMENT APPROVAL (separated from UI logic)
        private async Task ProcessAppointmentApproval(AppointmentModel appointment, string? additionalNotes = null)
        {
            try
            {
                if (supabase == null)
                {
                    MessageBox.Show("Database connection not established.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Log("INFO", $"Approving appointment ID: {appointment.Id}...");

                // Update appointment status to "Approved" and add notes if provided
                var updateData = new Dictionary<string, object>
                {
                    { "status", "Approved" }
                };

                if (!string.IsNullOrEmpty(additionalNotes))
                {
                    updateData.Add("admin_notes", additionalNotes);
                    Log("INFO", $"Added admin notes: {additionalNotes}");
                }

                await supabase
                    .From<AppointmentModel>()
                    .Where(a => a.Id == appointment.Id)
                    .Set(updateData)
                    .Update();

                Log("SUCCESS", $"Appointment {appointment.Id} approved in database.");

                // ✅ CREATE NOTIFICATION WITH type: "approve_notif"
                await CreateAppointmentNotification(
                    appointment.CustomerId,
                    "Appointment Approved 🎉",
                    $"Your {appointment.Service} appointment on {appointment.Date?.ToString("MMM dd, yyyy")} at {FormatTimeSpan(appointment.Time)} has been approved!",
                    "Approved",
                    "approve_notif",
                    appointment
                );

                // Remove from UI
                var itemToRemove = appointments.FirstOrDefault(a => a.Id == appointment.Id);
                if (itemToRemove != null)
                {
                    appointments.Remove(itemToRemove);
                    Log("SUCCESS", $"Appointment {appointment.Id} removed from UI. Remaining: {appointments.Count}");

                    // Show success message

                }
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Error approving appointment: {ex.Message}");
                MessageBox.Show($"Failed to approve appointment:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ REJECT BUTTON
        private async void RejectAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AppointmentModel appointment)
            {
                Log("DEBUG", $"Reject button clicked - Appointment ID: {appointment.Id}, Customer: {appointment.CustomerName}");

                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    MessageBox.Show("This appointment is already completed or cancelled.", "Cannot Reject", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to REJECT this appointment?\n\n" +
                    $"Customer: {appointment.CustomerName}\n" +
                    $"Service: {appointment.Service}\n" +
                    $"Date: {appointment.Date?.ToString("MMMM dd, yyyy")}\n" +
                    $"Time: {FormatTimeSpan(appointment.Time)}\n" +
                    $"Current Status: {appointment.Status}",
                    "Confirm Rejection",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result != MessageBoxResult.Yes)
                {
                    Log("INFO", $"Rejection cancelled for appointment ID: {appointment.Id}");
                    return;
                }

                try
                {
                    if (supabase == null)
                    {
                        MessageBox.Show("Database connection not established.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    Log("INFO", $"Rejecting appointment ID: {appointment.Id}...");

                    // Update appointment status to "Cancelled"
                    await supabase
                        .From<AppointmentModel>()
                        .Where(a => a.Id == appointment.Id)
                        .Set(a => a.Status, "Cancelled")
                        .Update();

                    Log("SUCCESS", $"Appointment {appointment.Id} cancelled in database.");

                    // ✅ CREATE NOTIFICATION WITH type: "decline_notif"
                    await CreateAppointmentNotification(
                        appointment.CustomerId,
                        "Appointment Cancelled ❌",
                        $"Your {appointment.Service} appointment on {appointment.Date?.ToString("MMM dd, yyyy")} has been cancelled.",
                        "Cancelled",
                        "decline_notif",
                        appointment
                    );

                    // Remove from UI
                    var itemToRemove = appointments.FirstOrDefault(a => a.Id == appointment.Id);
                    if (itemToRemove != null)
                    {
                        appointments.Remove(itemToRemove);
                        Log("SUCCESS", $"Appointment {appointment.Id} removed from UI. Remaining: {appointments.Count}");
                        MessageBox.Show("❌ Appointment cancelled! Notification sent to customer.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    Log("ERROR", $"Error rejecting appointment: {ex.Message}");
                    MessageBox.Show($"Failed to reject appointment:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Log("ERROR", "Button DataContext is not AppointmentModel or is null");
            }
        }

        // 🧪 TEST NOTIFICATION BUTTON - ENHANCED DEBUGGING
        private async void TestNotification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (supabase == null)
                {
                    MessageBox.Show("Supabase client not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Log("TEST", "=== STARTING PUSH NOTIFICATION TEST ===");

                // 1. Check appointments with push tokens
                Log("TEST", "Checking appointments with push tokens...");
                var appointmentsWithTokens = await GetAppointmentsWithPushTokens();

                // 2. Check customer profiles with push tokens
                Log("TEST", "Checking customer profiles with push tokens...");
                var customersWithTokens = await GetCustomersWithPushTokens();

                // 3. Show debug information
                var debugInfo = $"🔍 DEBUG INFORMATION:\n\n" +
                               $"Appointments with push tokens: {appointmentsWithTokens.Count}\n" +
                               $"Customers with push tokens: {customersWithTokens.Count}\n\n" +
                               $"Recent appointments loaded: {this.appointments.Count}";

                Log("DEBUG", debugInfo);

                if (appointmentsWithTokens.Count == 0 && customersWithTokens.Count == 0)
                {
                    MessageBox.Show(
                        "❌ No push tokens found anywhere!\n\n" +
                        "Please ensure:\n" +
                        "1. React Native app is running on physical device\n" +
                        "2. Push notifications are enabled\n" +
                        "3. App has registered a push token\n" +
                        "4. Push token is saved to customer_profiles\n" +
                        "5. New appointments are created through the app\n\n" +
                        debugInfo,
                        "No Push Tokens Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Show selection window with all available options
                var selectionWindow = new DebugSelectionWindow(appointmentsWithTokens, customersWithTokens);
                if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedOption != null)
                {
                    var selected = selectionWindow.SelectedOption;

                    Log("TEST", $"Selected: {selected.Type} - {selected.Name}");
                    Log("TEST", $"Push Token: {selected.PushToken}");

                    // Create test appointment
                    var testAppointment = new AppointmentModel
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = selected.CustomerId,
                        CustomerName = selected.Name,
                        Service = "Test Service",
                        Date = DateTime.Now.AddDays(1),
                        Time = TimeSpan.FromHours(10),
                        ReceiptCode = "TEST001",
                        Status = "On Going",
                        PushToken = selected.PushToken
                    };

                    // Send test notification via Edge Function first, then fallback to direct Expo
                    bool success = await SendPushNotification(
                        selected.PushToken,
                        "🔔 Test Notification from WPF",
                        $"Hello {selected.Name}! This is a test notification from your barbershop admin app.",
                        testAppointment,
                        "test_notif"
                    );

                    // Create notification in database
                    await CreateNotificationInDatabase(
                        selected.CustomerId,
                        "🔔 Test Notification from WPF",
                        $"Hello {selected.Name}! This is a test notification from your barbershop admin app.",
                        "Test",
                        "test_notif",
                        testAppointment.Id
                    );

                    if (success)
                    {
                        MessageBox.Show($"✅ Test notification sent successfully!\n\n" +
                                      $"To: {selected.Name}\n" +
                                      $"Type: {selected.Type}\n" +
                                      $"Push Token: {selected.PushToken?.Substring(0, 20)}...\n\n" +
                                      $"Check your physical device!",
                                      "Test Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"⚠️ Test notification may not have been delivered.\n\n" +
                                      $"Check the debug logs for details.\n" +
                                      $"To: {selected.Name}\n" +
                                      $"Token: {selected.PushToken?.Substring(0, 20)}...",
                                      "Test Completed with Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Test notification failed: {ex.Message}");
                MessageBox.Show($"Test failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ GET APPOINTMENTS WITH PUSH TOKENS - FIXED WITH CLIENT-SIDE FILTERING
        private async Task<List<PushTokenOption>> GetAppointmentsWithPushTokens()
        {
            var options = new List<PushTokenOption>();

            try
            {
                // Get all appointments and filter client-side
                var response = await supabase
                    .From<AppointmentModel>()
                    .Select("id, customer_id, customer_name, push_token, receipt_code")
                    .Limit(50)  // Get more since we're filtering client-side
                    .Get();

                // Filter out null/empty push tokens in C#
                foreach (var appointment in response.Models)
                {
                    if (!string.IsNullOrEmpty(appointment.PushToken))
                    {
                        options.Add(new PushTokenOption
                        {
                            Type = "Appointment",
                            CustomerId = appointment.CustomerId,
                            Name = $"{appointment.CustomerName} ({appointment.ReceiptCode})",
                            PushToken = appointment.PushToken
                        });
                    }
                }

                Log("DEBUG", $"Found {options.Count} appointments with push tokens");
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Error getting appointments with push tokens: {ex.Message}");
            }

            return options;
        }

        // ✅ GET CUSTOMERS WITH PUSH TOKENS - UPDATED FOR YOUR TABLE STRUCTURE
        private async Task<List<PushTokenOption>> GetCustomersWithPushTokens()
        {
            var options = new List<PushTokenOption>();

            try
            {
                Log("DEBUG", "Fetching customer profiles with push tokens...");

                var response = await supabase
                    .From<CustomerProfile>()
                    .Select("id, display_name, username, push_token")
                    .Limit(50)
                    .Get();

                Log("DEBUG", $"Retrieved {response.Models.Count} customer profiles from database");

                foreach (var profile in response.Models)
                {
                    if (!string.IsNullOrEmpty(profile.PushToken))
                    {
                        // Use display_name if available, otherwise fall back to username
                        string displayName = !string.IsNullOrEmpty(profile.DisplayName)
                            ? profile.DisplayName
                            : (!string.IsNullOrEmpty(profile.Username)
                                ? profile.Username
                                : "Unknown Customer");

                        options.Add(new PushTokenOption
                        {
                            Type = "Customer Profile",
                            CustomerId = profile.Id,
                            Name = displayName,
                            PushToken = profile.PushToken
                        });
                    }
                }

                Log("SUCCESS", $"Found {options.Count} customers with push tokens in profiles");
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Error getting customers with push tokens: {ex.Message}");
                Log("ERROR", $"Stack trace: {ex.StackTrace}");

                // Show more detailed error information
                MessageBox.Show($"Failed to load customer profiles:\n\n{ex.Message}\n\nThis might be due to:\n1. Missing push_token column in table\n2. Table permissions\n3. RLS policies\n\nPlease run the SQL script to add the missing columns.",
                               "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return options;
        }

        // ✅ CREATE NOTIFICATION IN DATABASE - IMPROVED VERSION
        private async Task CreateAppointmentNotification(string customerId, string header, string description, string status, string type, AppointmentModel appointment)
        {
            try
            {
                Log("NOTIFICATION", $"Creating notification for customer: {customerId}");

                string? pushToken = null;
                string pushTokenSource = "None";

                // 1. First try to get push token from the appointment itself
                if (!string.IsNullOrEmpty(appointment.PushToken))
                {
                    pushToken = appointment.PushToken;
                    pushTokenSource = "Appointment";
                    Log("NOTIFICATION", $"✅ Using push token from appointment: {pushToken?.Substring(0, 20)}...");
                }
                else
                {
                    // 2. Fallback: Get push token from customer_profiles
                    pushToken = await GetPushTokenFromCustomerProfile(customerId);
                    if (!string.IsNullOrEmpty(pushToken))
                    {
                        pushTokenSource = "Customer Profile";
                        Log("NOTIFICATION", $"✅ Using push token from customer profile: {pushToken?.Substring(0, 20)}...");
                    }
                    else
                    {
                        Log("NOTIFICATION", $"❌ No push token available for customer {customerId}");
                    }
                }

                // 3. Create notification in the notifications table
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = customerId,
                    Header = header,
                    Description = description,
                    Status = status,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };

                await supabase.From<Notification>().Insert(notification);
                Log("SUCCESS", $"📝 Notification created in database for user {customerId}");

                // 4. Send push notification if push token is available
                if (!string.IsNullOrEmpty(pushToken))
                {
                    Log("PUSH", $"🚀 Sending push notification (Source: {pushTokenSource})");
                    await SendPushNotification(pushToken, header, description, appointment, type);
                }
                else
                {
                    Log("INFO", "📝 Notification saved to database only (no push token)");
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", $"❌ Failed to create notification: {ex.Message}");
            }
        }

        // ✅ GET PUSH TOKEN FROM CUSTOMER PROFILE - UPDATED
        private async Task<string?> GetPushTokenFromCustomerProfile(string customerId)
        {
            try
            {
                var response = await supabase
                    .From<CustomerProfile>()
                    .Select("push_token")
                    .Filter("id", Operator.Equals, customerId)
                    .Single();

                return response?.PushToken;
            }
            catch (Exception ex)
            {
                Log("DEBUG", $"No push token found in customer profile for {customerId}: {ex.Message}");
                return null;
            }
        }

        // ✅ SEND PUSH NOTIFICATION - UPDATED WITH EDGE FUNCTIONS
        private async Task<bool> SendPushNotification(string pushToken, string title, string body, AppointmentModel appointment, string type)
        {
            try
            {
                // Validate push token
                if (string.IsNullOrEmpty(pushToken) || !pushToken.StartsWith("ExponentPushToken"))
                {
                    Log("ERROR", $"❌ Invalid push token format: {pushToken}");
                    return false;
                }

                Log("PUSH", $"🚀 Starting push notification process...");
                Log("PUSH", $"📱 Token: {pushToken.Substring(0, 20)}...");
                Log("PUSH", $"📝 Title: {title}");
                Log("PUSH", $"📝 Body: {body}");
                Log("PUSH", $"🎯 Type: {type}");

                // Try Edge Function first
                bool edgeFunctionSuccess = await SendViaEdgeFunction(pushToken, title, body, appointment, type);

                if (edgeFunctionSuccess)
                {
                    Log("SUCCESS", "✅ Push notification sent successfully via Edge Function!");
                    return true;
                }

                // Fallback to direct Expo API
                Log("PUSH", "🔄 Falling back to direct Expo API...");
                bool expoSuccess = await SendViaExpoDirect(pushToken, title, body, appointment, type);

                return expoSuccess;
            }
            catch (Exception ex)
            {
                Log("ERROR", $"❌ Error in push notification process: {ex.Message}");
                return false;
            }
        }

        // ✅ SEND VIA EDGE FUNCTION - IMPROVED
        private async Task<bool> SendViaEdgeFunction(string pushToken, string title, string body, AppointmentModel appointment, string type)
        {
            try
            {
                Log("EDGE", "📤 Attempting to send via Supabase Edge Function...");

                // Prepare the data for Edge Function
                var requestData = new
                {
                    pushToken = pushToken,
                    title = title,
                    body = body,
                    data = new
                    {
                        type = "appointment_update",
                        notification_type = type,
                        appointment_id = appointment.Id.ToString(),
                        receipt_code = appointment.ReceiptCode,
                        status = appointment.Status,
                        screen = "Appointments",
                        customer_name = appointment.CustomerName,
                        service = appointment.Service,
                        date = appointment.Date?.ToString("MMM dd, yyyy"),
                        time = FormatTimeSpan(appointment.Time)
                    }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Get Supabase URL and Key from configuration
                string? supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
                string? supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                {
                    Log("ERROR", "❌ Supabase configuration missing for Edge Function");
                    return false;
                }

                // Call Supabase Edge Function
                var edgeFunctionUrl = $"{supabaseUrl}/functions/v1/send-push-notification";

                Log("EDGE", $"🔗 Calling Edge Function: {edgeFunctionUrl}");

                // Create request with proper headers
                var request = new HttpRequestMessage(HttpMethod.Post, edgeFunctionUrl)
                {
                    Content = content
                };
                request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

                var response = await _edgeClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Log("SUCCESS", $"✅ Edge Function call successful!");
                    Log("SUCCESS", $"📨 Edge Function response: {responseContent}");
                    return true;
                }
                else
                {
                    Log("ERROR", $"❌ Edge Function failed: {response.StatusCode} - {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", $"❌ Edge Function error: {ex.Message}");
                return false;
            }
        }

        // ✅ FALLBACK: DIRECT EXPO API
        private async Task<bool> SendViaExpoDirect(string pushToken, string title, string body, AppointmentModel appointment, string type)
        {
            try
            {
                Log("EXPO", "🔄 Trying direct Expo API as fallback...");

                var notificationData = new
                {
                    to = pushToken,
                    title = title,
                    body = body,
                    sound = "default",
                    data = new
                    {
                        type = "appointment_update",
                        notification_type = type,
                        appointment_id = appointment.Id.ToString(),
                        receipt_code = appointment.ReceiptCode,
                        status = appointment.Status,
                        screen = "Appointments"
                    }
                };

                var json = JsonSerializer.Serialize(notificationData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://exp.host/--/api/v2/push/send", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Log("SUCCESS", $"✅ Direct Expo API successful!");

                    // Parse Expo response to check for errors
                    try
                    {
                        var expoResponse = JsonSerializer.Deserialize<ExpoResponse>(responseContent);
                        if (expoResponse?.Data != null)
                        {
                            foreach (var receipt in expoResponse.Data)
                            {
                                if (receipt.Status == "error")
                                {
                                    Log("ERROR", $"❌ Expo delivery error: {receipt.Message}");
                                    return false;
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // If we can't parse the response, assume success if status code is 200
                        Log("INFO", "📨 Expo response received (unable to parse details)");
                    }

                    return true;
                }
                else
                {
                    Log("ERROR", $"❌ Direct Expo API failed: {response.StatusCode} - {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", $"❌ Direct Expo API error: {ex.Message}");
                return false;
            }
        }

        // ✅ CREATE NOTIFICATION IN DATABASE (Separate method for test)
        private async Task CreateNotificationInDatabase(string customerId, string header, string description, string status, string type, Guid appointmentId)
        {
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = customerId,
                    Header = header,
                    Description = description,
                    Status = status,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };

                await supabase.From<Notification>().Insert(notification);
                Log("SUCCESS", $"📝 Test notification created in database for user {customerId}");
            }
            catch (Exception ex)
            {
                Log("ERROR", $"❌ Failed to create test notification in database: {ex.Message}");
            }
        }

        // 🔄 REFRESH BUTTON
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Log("INFO", "Manual refresh triggered - Loading 'On Going' appointments...");
            await LoadAppointments();
        }

        // 🏠 HOME NAVIGATION
        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Log("INFO", "Navigating to Menu window.");
            try
            {
                var menu = new Menu();
                menu.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Navigation error: {ex.Message}");
                MessageBox.Show("Failed to navigate to menu.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🛎️ NOTIFICATION CLICK
        private void Notification_Click(object sender, MouseButtonEventArgs e)
        {
            Log("INFO", "Notification icon clicked");
            MessageBox.Show("Notifications feature would open here.", "Notifications", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ⚙️ SETTINGS CLICK
        private void Settings_Click(object sender, MouseButtonEventArgs e)
        {
            Log("INFO", "Settings icon clicked");
            MessageBox.Show("Settings feature would open here.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 📅 BOOK APPOINTMENT CLICK
        private void BookAppointment_Click(object sender, RoutedEventArgs e)
        {
            Log("INFO", "Book Appointment button clicked");
            MessageBox.Show("Book Appointment feature would open here.", "Book Appointment", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 📋 APPOINTMENT RECORDS CLICK
        private void AppointmentRecords_Click(object sender, RoutedEventArgs e)
        {
            Log("INFO", "Appointment Records button clicked");
            MessageBox.Show("Appointment Records feature would open here.", "Appointment Records", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ✂️ MANAGE SERVICE CLICK
        private void ManageService_Click(object sender, RoutedEventArgs e)
        {
            Log("INFO", "Manage Service button clicked");
            MessageBox.Show("Manage Service feature would open here.", "Manage Service", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ◀️ PREVIOUS PAGE CLICK
        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            Log("INFO", "Previous Page button clicked");
            MessageBox.Show("Previous page functionality would go here.", "Previous Page", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ▶️ NEXT PAGE CLICK
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            Log("INFO", "Next Page button clicked");
            MessageBox.Show("Next page functionality would go here.", "Next Page", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 🆕 HELPER METHOD: Format TimeSpan to readable time
        private string FormatTimeSpan(TimeSpan? timeSpan)
        {
            if (timeSpan == null) return "N/A";
            DateTime time = DateTime.Today.Add(timeSpan.Value);
            return time.ToString("h:mm tt");
        }

        // ✅ SUPABASE MODELS - UPDATED WITH ADMIN_NOTES

        [Table("appointment_sched")]
        public class AppointmentModel : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("customer_id")]
            public string CustomerId { get; set; } = string.Empty;

            [Column("customer_name")]
            public string CustomerName { get; set; } = string.Empty;

            [Column("contact_number")]
            public string? ContactNumber { get; set; }

            [Column("customer_badge")]
            public string? CustomerBadge { get; set; }

            [Column("service_id")]
            public string Service { get; set; } = string.Empty;

            [Column("barber_id")]
            public string Barber { get; set; } = string.Empty;

            [Column("sched_date")]
            public DateTime? Date { get; set; }

            [Column("sched_time")]
            public TimeSpan? Time { get; set; }

            [Column("subtotal")]
            public decimal? Subtotal { get; set; }

            [Column("appointment_fee")]
            public decimal? AppointmentFee { get; set; }

            [Column("total")]
            public decimal? Total { get; set; }

            [Column("payment_method")]
            public string PaymentMethod { get; set; } = string.Empty;

            [Column("receipt_code")]
            public string ReceiptCode { get; set; } = string.Empty;

            [Column("status")]
            public string Status { get; set; } = "On Going";

            [Column("push_token")]
            public string? PushToken { get; set; }

            [Column("admin_notes")]
            public string? AdminNotes { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }
        }

        [Table("customer_profiles")]
        public class CustomerProfile : BaseModel
        {
            [PrimaryKey("id", false)]
            public string Id { get; set; } = string.Empty;

            [Column("display_name")]
            public string? DisplayName { get; set; }

            [Column("username")]
            public string? Username { get; set; }

            [Column("contact_number")]
            public string? ContactNumber { get; set; }

            [Column("push_token")]
            public string? PushToken { get; set; }

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }

        [Table("notifications")]
        public class Notification : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("user_id")]
            public string UserId { get; set; } = string.Empty;

            [Column("header")]
            public string Header { get; set; } = string.Empty;

            [Column("description")]
            public string Description { get; set; } = string.Empty;

            [Column("status")]
            public string Status { get; set; } = string.Empty;

            [Column("type")]
            public string Type { get; set; } = string.Empty;

            [Column("created_at")]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }

    // 🆕 PUSH TOKEN OPTION CLASS
    public class PushTokenOption
    {
        public string Type { get; set; } = string.Empty; // "Appointment" or "Customer Profile"
        public string CustomerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PushToken { get; set; }
    }

    // 🆕 EXPO RESPONSE MODELS
    public class ExpoResponse
    {
        [JsonPropertyName("data")]
        public List<ExpoReceipt> Data { get; set; } = new List<ExpoReceipt>();
    }

    public class ExpoReceipt
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public object? Details { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    // 🆕 DEBUG SELECTION WINDOW
    public class DebugSelectionWindow : Window
    {
        public PushTokenOption? SelectedOption { get; private set; }
        private List<PushTokenOption> _options;

        public DebugSelectionWindow(List<PushTokenOption> appointments, List<PushTokenOption> customers)
        {
            _options = new List<PushTokenOption>();
            _options.AddRange(appointments);
            _options.AddRange(customers);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Title = "Select Push Token Source for Testing";
            this.Width = 700;
            this.Height = 500;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var stackPanel = new StackPanel();

            var titleText = new TextBlock
            {
                Text = "🔍 Select a push token source for testing:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(titleText);

            if (_options.Count == 0)
            {
                var noTokensText = new TextBlock
                {
                    Text = "❌ No push tokens found in database!",
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(noTokensText);
            }

            foreach (var option in _options)
            {
                var button = new Button
                {
                    Content = $"📱 {option.Type}: {option.Name}\n" +
                             $"🔑 Token: {option.PushToken?.Substring(0, 30)}...",
                    Tag = option,
                    Margin = new Thickness(10, 5, 10, 5),
                    Padding = new Thickness(10),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = option.Type == "Appointment" ? Brushes.LightBlue : Brushes.LightGreen
                };
                button.Click += (s, e) =>
                {
                    SelectedOption = (PushTokenOption)((Button)s).Tag;
                    this.DialogResult = true;
                    this.Close();
                };
                stackPanel.Children.Add(button);
            }

            var cancelButton = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(10),
                Padding = new Thickness(20, 10, 20, 10)
            };
            cancelButton.Click += (s, e) =>
            {
                this.DialogResult = false;
                this.Close();
            };
            stackPanel.Children.Add(cancelButton);

            this.Content = new ScrollViewer
            {
                Content = stackPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }
    }
}