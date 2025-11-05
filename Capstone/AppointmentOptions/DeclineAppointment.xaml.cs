using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Supabase.Postgrest.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase;

namespace Capstone.AppointmentOptions
{
    public partial class DeclineAppointment : Window
    {
        private Client? supabase;
        public Appointments.AppointmentModel? SelectedAppointment { get; set; }
        public Action<Appointments.AppointmentModel, string>? OnConfirmDecline { get; set; }
        private DeclineNotificationService? _notificationService;

        public DeclineAppointment()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            InitializeNotificationService();
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
                MessageBox.Show("⚠️ Supabase configuration missing in App.config!", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            supabase = new Client(supabaseUrl, effectiveKey, new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        private void InitializeNotificationService()
        {
            try
            {
                _notificationService = new DeclineNotificationService(supabase);
                Console.WriteLine("✅ Notification service initialized in DeclineAppointment");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Failed to initialize notification service: {ex.Message}\n\nNotifications will not work.",
                              "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine($"Notification service init error: {ex}");
            }
        }

        private async void ConfirmDecline_Click(object sender, RoutedEventArgs e)
        {
            // Validate that a reason is selected
            if (cmbItemID.SelectedIndex <= 0)
            {
                cmbItemIDError.Text = "Please select a reason for decline";
                cmbItemIDError.Visibility = Visibility.Visible;
                return;
            }

            // Hide error message if validation passes
            cmbItemIDError.Visibility = Visibility.Collapsed;

            // Get selected reason
            string selectedReason = ((ComboBoxItem)cmbItemID.SelectedItem).Content.ToString() ?? "";

            if (supabase == null || SelectedAppointment == null || SelectedAppointment.Id == Guid.Empty)
            {
                MessageBox.Show("⚠️ Unable to update appointment. Missing data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Console.WriteLine($"\n🔄 Declining appointment {SelectedAppointment.ReceiptCode} with reason: {selectedReason}");

                // Update both Status and Reason_Decline in database
                var updated = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Id == SelectedAppointment.Id)
                    .Set(x => x.Status, "Declined")
                    .Set(x => x.ReasonDecline, selectedReason)
                    .Update();

                if (updated.Models != null && updated.Models.Count > 0)
                {
                    Console.WriteLine($"✅ Database updated successfully");

                    // Save notification to notification_loader table
                    bool notificationSaved = false;
                    string notificationSaveMessage = "";

                    if (_notificationService != null && !string.IsNullOrEmpty(SelectedAppointment.CustomerId))
                    {
                        Console.WriteLine($"💾 Attempting to save notification for customer: {SelectedAppointment.CustomerId}");

                        // ✅ FIXED: Call the correct method name
                        notificationSaved = await _notificationService.SaveDeclineNotificationToLoader(
                            SelectedAppointment.CustomerName,
                            SelectedAppointment.ReceiptCode,  // This becomes receipt_id in database
                            selectedReason,
                            SelectedAppointment.CustomerId
                        );

                        if (notificationSaved)
                        {
                            notificationSaveMessage = "✅ Notification saved to history!";
                            Console.WriteLine($"✅ Notification successfully saved to database");
                        }
                        else
                        {
                            notificationSaveMessage = "⚠️ Notification failed to save to history.";
                            Console.WriteLine($"❌ Notification failed to save to database");
                        }
                    }
                    else
                    {
                        notificationSaveMessage = "⚠️ Cannot save notification - missing customer information.";
                        Console.WriteLine($"❌ Cannot save notification - missing service or customer ID");
                    }

                    // Send push notification if token exists
                    bool notificationSent = false;
                    string pushNotificationMessage = "";

                    if (!string.IsNullOrEmpty(SelectedAppointment.PushToken) && _notificationService != null)
                    {
                        Console.WriteLine($"📨 Sending decline push notification...");
                        Console.WriteLine($"📱 Push Token: {SelectedAppointment.PushToken}");

                        notificationSent = await _notificationService.SendDeclineNotification(
                            SelectedAppointment.PushToken,
                            SelectedAppointment.Id.ToString(),
                            SelectedAppointment.CustomerName,
                            SelectedAppointment.ReceiptCode,
                            selectedReason,
                            SelectedAppointment.CustomerId);

                        if (notificationSent)
                        {
                            Console.WriteLine($"✅ Push notification sent successfully");
                            pushNotificationMessage = "📨 Push notification sent to customer.";
                        }
                        else
                        {
                            Console.WriteLine($"❌ Push notification failed");
                            pushNotificationMessage = "⚠️ Push notification failed to send.";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ No push token available - skipping push notification");
                        pushNotificationMessage = "ℹ️ No push token available for customer.";
                    }

                    // Show accurate result message
                    string resultMessage = "✅ Appointment declined successfully!\n\n";
                    resultMessage += $"{notificationSaveMessage}\n{pushNotificationMessage}";

                    if (notificationSaved && notificationSent)
                    {
                        MessageBox.Show(resultMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(resultMessage, "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    // Call the decline action to remove from table
                    OnConfirmDecline?.Invoke(SelectedAppointment, selectedReason);

                    // Show success modal on the main Appointments window
                    if (this.Owner != null)
                    {
                        var successModal = new DeclineAppointmentSuccess();
                        successModal.Owner = this.Owner;
                        successModal.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        successModal.Show();
                    }

                    // Close this modal
                    this.Close();
                }
                else
                {
                    MessageBox.Show("⚠️ No rows were updated. Appointment may have already been modified.",
                                  "Update Failed",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error declining appointment: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Console.WriteLine($"Full error: {ex}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ============ NOTIFICATION SERVICE CLASS ============

        public class DeclineNotificationService
        {
            private static readonly HttpClient _httpClient = new HttpClient();
            private readonly string _edgeFunctionUrl;
            private readonly string _supabaseAnonKey;
            private static bool _isInitialized = false;
            private readonly Client? _supabase;

            public DeclineNotificationService(Client? supabase)
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

            public async Task<bool> SaveDeclineNotificationToLoader(string customerName, string receiptCode, string declineReason, string userId)
            {
                try
                {
                    Console.WriteLine($"\n========================================");
                    Console.WriteLine($"💾 [DECLINE] Starting SaveDeclineNotificationToLoader");
                    Console.WriteLine($"   Customer: '{customerName}'");
                    Console.WriteLine($"   ReceiptCode: '{receiptCode}'");
                    Console.WriteLine($"   Reason: '{declineReason}'");
                    Console.WriteLine($"   UserId: '{userId}'");
                    Console.WriteLine($"========================================\n");

                    if (_supabase == null)
                    {
                        Console.WriteLine("❌ CRITICAL: Supabase client is NULL");
                        return false;
                    }

                    if (string.IsNullOrEmpty(userId))
                    {
                        Console.WriteLine($"❌ CRITICAL: userId is null or empty");
                        return false;
                    }

                    if (!Guid.TryParse(userId, out Guid userGuid))
                    {
                        Console.WriteLine($"❌ CRITICAL: Invalid user_id format: '{userId}'");
                        return false;
                    }

                    Console.WriteLine($"✅ UserId parsed successfully: {userGuid}");

                    // Get the decline-specific message
                    var (title, description) = GetDeclineNotificationTemplate(declineReason);

                    // Validate and truncate
                    if (string.IsNullOrWhiteSpace(title)) title = "Molave Street Barbers";
                    if (title.Length > 255) title = title.Substring(0, 255);

                    if (!string.IsNullOrWhiteSpace(description) && description.Length > 1000)
                        description = description.Substring(0, 1000);

                    if (string.IsNullOrWhiteSpace(receiptCode)) receiptCode = "N/A";

                    var notification = new NotificationLoaderModel
                    {
                        UserId = userGuid,
                        ReceiptId = receiptCode,
                        Title = title,
                        Description = description,
                        CreatedAt = DateTime.UtcNow,
                        Read = false
                    };

                    Console.WriteLine($"💾 Inserting notification into notification_loader...");
                    Console.WriteLine($"   UserId: {notification.UserId}");
                    Console.WriteLine($"   ReceiptId: {notification.ReceiptId}");
                    Console.WriteLine($"   Title: {notification.Title}");
                    Console.WriteLine($"   Description: {notification.Description?.Substring(0, Math.Min(50, description?.Length ?? 0))}...");

                    var result = await _supabase
                        .From<NotificationLoaderModel>()
                        .Insert(notification);

                    if (result?.Models != null && result.Models.Count > 0)
                    {
                        var inserted = result.Models[0];
                        Console.WriteLine($"✅ Notification saved successfully to notification_loader!");
                        Console.WriteLine($"   Database ID: {inserted.Id}");
                        Console.WriteLine($"   Receipt ID: {inserted.ReceiptId}");
                        return true;
                    }
                    else if (result?.ResponseMessage?.IsSuccessStatusCode == true)
                    {
                        Console.WriteLine($"✅ Notification saved successfully (HTTP 200)");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to save notification - no models returned");

                        if (result?.ResponseMessage != null)
                        {
                            Console.WriteLine($"   HTTP Status: {result.ResponseMessage.StatusCode}");
                            try
                            {
                                var responseContent = await result.ResponseMessage.Content.ReadAsStringAsync();
                                Console.WriteLine($"   Response: {responseContent}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"   Could not read response: {ex.Message}");
                            }
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ERROR saving decline notification: {ex.Message}");
                    Console.WriteLine($"   Stack: {ex.StackTrace}");
                    return false;
                }
            }

            public async Task<bool> SendDeclineNotification(string pushToken, string appointmentId, string customerName, string receiptCode, string declineReason, string userId)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(pushToken))
                    {
                        Console.WriteLine("❌ Invalid push token for decline notification");
                        return false;
                    }

                    Console.WriteLine($"📨 Sending decline notification for appointment: {appointmentId}");

                    var (title, message) = GetDeclineNotificationTemplate(declineReason);

                    // SEND PUSH NOTIFICATION ONLY (notification already saved in ConfirmDecline_Click)
                    var payload = new
                    {
                        expoPushToken = pushToken,
                        title = title,
                        message = message,
                        data = new
                        {
                            type = "appointment_declined",
                            appointment_id = appointmentId,
                            status = "Declined",
                            decline_reason = declineReason,
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
                    Console.WriteLine($"💥 Decline notification error: {ex.Message}");
                    return false;
                }
            }

            private (string title, string message) GetDeclineNotificationTemplate(string declineReason)
            {
                return declineReason.ToLower() switch
                {
                    "time slot unavailable" => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. The selected time slot is no longer available. Please choose another time."
                    ),
                    "short notice request" => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. We can't accept short-notice bookings. Please schedule earlier next time."
                    ),
                    "barber unvailable" or "barber unavailable" => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. The chosen barber isn't available at that time. Please select another barber or reschedule."
                    ),
                    "service unvailable" or "service unavailable" => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. The service you selected is currently unavailable. Please pick a different service."
                    ),
                    "shop closed/holiday" => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. The shop is closed on your selected date. Please choose another available day."
                    ),
                    _ => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. Please contact us for more information."
                    )
                };
            }

            private async Task<bool> SendNotificationRequest(object payload)
            {
                string responseContent = string.Empty;

                try
                {
                    var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(_edgeFunctionUrl, content);
                    responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"📥 Response Status: {(int)response.StatusCode} {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
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
                            Console.WriteLine($"❌ Edge function returned success=false: {error}");
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ HTTP Error: {response.StatusCode}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Notification request error: {ex.Message}");
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

            [Column("status")]
            public string Status { get; set; } = string.Empty;

            [Column("Reason_Decline")]
            public string ReasonDecline { get; set; } = string.Empty;

            [Column("push_token")]
            public string? PushToken { get; set; }

            [Column("customer_name")]
            public string CustomerName { get; set; } = string.Empty;

            [Column("receipt_code")]
            public string ReceiptCode { get; set; } = string.Empty;

            [Column("customer_id")]
            public string CustomerId { get; set; } = string.Empty;
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
            public bool? Read { get; set; }
        }
    }
}