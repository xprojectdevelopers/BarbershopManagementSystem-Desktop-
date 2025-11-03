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

namespace Capstone.AppointmentOptions
{
    public partial class DeclineAppointment : Window
    {
        private Supabase.Client? supabase;
        public Appointments.AppointmentModel? SelectedAppointment { get; set; }
        public Action<Appointments.AppointmentModel, string>? OnConfirmDecline { get; set; }
        private NotificationService? _notificationService;

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

        private void InitializeNotificationService()
        {
            try
            {
                _notificationService = new NotificationService();
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
                MessageBox.Show("⚠️ Unable to update appointment. Missing data.");
                return;
            }

            try
            {
                Console.WriteLine($"\n🔄 Declining appointment {SelectedAppointment.ReceiptCode} with reason: {selectedReason}");

                // Update both Status and Reason_Decline in database using the correct model
                var updated = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Id == SelectedAppointment.Id)
                    .Set(x => x.Status, "Declined")
                    .Set(x => x.ReasonDecline, selectedReason)
                    .Update();

                if (updated.Models != null && updated.Models.Count > 0)
                {
                    Console.WriteLine($"✅ Database updated successfully");

                    // Send push notification if token exists
                    bool notificationSent = false;

                    if (!string.IsNullOrEmpty(SelectedAppointment.PushToken) && _notificationService != null)
                    {
                        Console.WriteLine($"📨 Sending decline notification for reason: {selectedReason}");
                        Console.WriteLine($"📱 Push Token: {SelectedAppointment.PushToken}");

                        notificationSent = await _notificationService.SendDeclineNotification(
                            SelectedAppointment.PushToken,
                            SelectedAppointment.Id.ToString(),
                            SelectedAppointment.CustomerName,
                            SelectedAppointment.ReceiptCode,
                            selectedReason);

                        if (notificationSent)
                        {
                            Console.WriteLine($"✅ Decline notification sent successfully");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Decline notification failed");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ No push token - skipping notification");
                        if (SelectedAppointment.PushToken == null)
                            Console.WriteLine($"ℹ️ PushToken is null");
                        else
                            Console.WriteLine($"ℹ️ PushToken is empty");
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
                    MessageBox.Show("⚠️ No rows were updated.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error declining appointment: {ex.Message}");
                Console.WriteLine($"Full error: {ex}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Just close the modal without doing anything
            this.Close();
        }

        // ============ NOTIFICATION SERVICE CLASS ============

        public class NotificationService
        {
            private static readonly HttpClient _httpClient = new HttpClient();
            private readonly string _edgeFunctionUrl;
            private readonly string _supabaseAnonKey;
            private static bool _isInitialized = false;

            public NotificationService()
            {
                _edgeFunctionUrl = ConfigurationManager.AppSettings["EdgeFunctionUrl"]
                    ?? "https://gycwoawekmmompvholqr.supabase.co/functions/v1/sendNotification";

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

            public async Task<bool> SendDeclineNotification(string pushToken, string appointmentId, string customerName, string receiptCode, string declineReason)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(pushToken))
                    {
                        Console.WriteLine("❌ Invalid push token for decline notification");
                        return false;
                    }

                    Console.WriteLine($"\n========================================");
                    Console.WriteLine($"📨 SENDING DECLINE NOTIFICATION");
                    Console.WriteLine($"========================================");
                    Console.WriteLine($"Appointment: {receiptCode}");
                    Console.WriteLine($"Customer: {customerName}");
                    Console.WriteLine($"Reason: {declineReason}");
                    Console.WriteLine($"Target token: {pushToken.Substring(0, Math.Min(30, pushToken.Length))}...");

                    // Get notification template based on decline reason
                    var (title, message) = GetDeclineNotificationTemplate(declineReason);

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

                    var result = await SendNotificationRequest(payload);

                    Console.WriteLine($"========================================\n");
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Decline notification error: {ex.Message}");
                    return false;
                }
            }

            private (string title, string message) GetDeclineNotificationTemplate(string declineReason)
            {
                // Notification templates based on your specification
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
                    Console.WriteLine($"📦 Payload:\n{json}");

                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    Console.WriteLine($"📤 Sending to: {_edgeFunctionUrl}");
                    Console.WriteLine($"🔑 Authorization: Bearer {_supabaseAnonKey.Substring(0, 20)}...");

                    var response = await _httpClient.PostAsync(_edgeFunctionUrl, content);
                    responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"📥 Response Status: {(int)response.StatusCode} {response.StatusCode}");
                    Console.WriteLine($"📥 Response Body: {responseContent}");

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

                            var ticket = result["ticket"]?.ToString();
                            if (!string.IsNullOrEmpty(ticket))
                            {
                                Console.WriteLine($"🎫 Expo ticket: {ticket}");
                            }

                            return true;
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
                        catch
                        {
                            // Response is not JSON, already logged above
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
                catch (TaskCanceledException ex)
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
                    Console.WriteLine($"💥 Unexpected error: {ex.GetType().Name} - {ex.Message}");
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

        // Use the same model as the main Appointments window
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
        }
    }
}