using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Capstone.AppointmentOptions
{
    public class NotificationService
    {
        // Static HttpClient to prevent socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _edgeFunctionUrl;
        private readonly string _supabaseAnonKey;
        private static bool _isInitialized = false;

        public NotificationService()
        {
            // Load configuration from App.config
            _edgeFunctionUrl = ConfigurationManager.AppSettings["EdgeFunctionUrl"]
                ?? "https://gycwoawekmmompvholqr.supabase.co/functions/v1/sendNotification";

            _supabaseAnonKey = ConfigurationManager.AppSettings["SupabaseKey"];

            if (string.IsNullOrEmpty(_supabaseAnonKey))
            {
                throw new InvalidOperationException("⚠️ SupabaseKey not found in App.config");
            }

            // Initialize HttpClient settings only once
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

        /// <summary>
        /// Send a simple test notification to verify the push token works
        /// </summary>
        public async Task<bool> SendTestNotification(string pushToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pushToken))
                {
                    Console.WriteLine("❌ Invalid push token: Token is null or empty");
                    return false;
                }

                if (!pushToken.StartsWith("ExponentPushToken["))
                {
                    Console.WriteLine("⚠️ Warning: Push token doesn't start with 'ExponentPushToken['");
                }

                Console.WriteLine("🚀 Sending test notification...");
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
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Send appointment status notification (Approved, Declined, Completed, etc.)
        /// </summary>
        public async Task<bool> SendAppointmentNotification(string pushToken, string appointmentId, string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pushToken))
                {
                    Console.WriteLine("❌ Invalid push token for appointment notification");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(appointmentId))
                {
                    Console.WriteLine("❌ Invalid appointment ID");
                    return false;
                }

                Console.WriteLine($"📨 Sending {status} notification for appointment: {appointmentId}");
                Console.WriteLine($"📱 Target token: {pushToken.Substring(0, Math.Min(30, pushToken.Length))}...");

                var (title, message) = GetNotificationContent(status);

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
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Core method to send notification request to Edge Function
        /// </summary>
        private async Task<bool> SendNotificationRequest(object payload)
        {
            HttpResponseMessage response = null;
            string responseContent = string.Empty;

            try
            {
                var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                Console.WriteLine($"📦 Payload:\n{json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"📤 Sending to: {_edgeFunctionUrl}");
                Console.WriteLine($"🔑 Authorization: Bearer {_supabaseAnonKey.Substring(0, 20)}...");

                response = await _httpClient.PostAsync(_edgeFunctionUrl, content);
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

                    // Parse response safely using JObject
                    var result = JObject.Parse(responseContent);
                    var success = result["success"]?.Value<bool>() ?? false;

                    if (success)
                    {
                        Console.WriteLine("✅ Notification sent successfully!");

                        // Log ticket info if available
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

                    // Try to parse error details
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
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Get notification title and message based on appointment status
        /// </summary>
        private (string title, string message) GetNotificationContent(string status)
        {
            return status switch
            {
                "Approved" => (
                    "Appointment Approved ✅",
                    "Your appointment has been approved and confirmed! See you soon."
                ),
                "Declined" => (
                    "Appointment Declined 🚫",
                    "Sorry, your appointment request has been declined. Please contact us for more information."
                ),
                "Completed" => (
                    "Appointment Completed ✅",
                    "Thank you for visiting! Your appointment has been completed."
                ),
                "No Show" => (
                    "Appointment Missed ❌",
                    "You missed your scheduled appointment. Please reschedule when convenient."
                ),
                "Test" => (
                    "Test Notification 🔔",
                    "This is a test notification from Molave Street Barbers."
                ),
                _ => (
                    $"Appointment Update: {status}",
                    $"Your appointment status has been updated to {status}."
                )
            };
        }

        /// <summary>
        /// Validate if a push token has the correct format
        /// </summary>
        public bool IsValidPushToken(string pushToken)
        {
            if (string.IsNullOrWhiteSpace(pushToken))
                return false;

            return pushToken.StartsWith("ExponentPushToken[") &&
                   pushToken.EndsWith("]") &&
                   pushToken.Length > 20;
        }
    }
}