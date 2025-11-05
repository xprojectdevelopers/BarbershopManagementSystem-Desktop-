using System;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
using static Supabase.Postgrest.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Capstone.AppointmentOptions
{
    /// <summary>
    /// Interaction logic for AppointmentTracker.xaml
    /// </summary>
    public partial class AppointmentTracker : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> items = new ObservableCollection<BarbershopManagementSystem>();
        private ObservableCollection<BarbershopManagementSystem> employees;
        private BarbershopManagementSystem currentAppointment;
        private Window currentModalWindow;
        private NotificationService _notificationService;

        public AppointmentTracker()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeData()
        {
            try
            {
                await InitializeSupabaseAsync();
                InitializeNotificationService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitializeSupabaseAsync()
        {
            string supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];
            string supabaseServiceKey = ConfigurationManager.AppSettings["SupabaseServiceKey"];

            // Use service key if available, otherwise fall back to anon key
            string effectiveKey = !string.IsNullOrEmpty(supabaseServiceKey) ? supabaseServiceKey : supabaseKey;

            supabase = new Client(supabaseUrl, effectiveKey, new SupabaseOptions
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
                _notificationService = new NotificationService(supabase);
                Console.WriteLine("✅ Notification service initialized for AppointmentTracker");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to initialize notification service: {ex.Message}");
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appointmentNumber = txtAppNumber.Text.Trim();

                if (string.IsNullOrWhiteSpace(appointmentNumber))
                {
                    ShowError(txtAppNumberError, "Appointment Number is required");
                    return;
                }

                // Clear any previous error message
                txtAppNumberError.Visibility = Visibility.Collapsed;
                txtAppNumberError.Text = string.Empty;

                // Search for appointment by receipt_code
                var response = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.ReceiptCode == appointmentNumber)
                    .Get();

                if (response.Models.Count == 0)
                {
                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new NotfoundAppNumber();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    Clear();
                    return;
                }

                // Get the first matching appointment
                currentAppointment = response.Models.First();

                // Log push token information
                Console.WriteLine($"\n🔍 Found appointment: {currentAppointment.ReceiptCode}");
                Console.WriteLine($"👤 Customer: {currentAppointment.CustomerName}");
                Console.WriteLine($"📱 Push Token: {(string.IsNullOrEmpty(currentAppointment.PushToken) ? "❌ NOT FOUND" : "✅ FOUND")}");

                // Auto-fill the form
                PopulateForm(currentAppointment);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching appointment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateForm(BarbershopManagementSystem appointment)
        {
            try
            {
                // Fill customer name
                txtCustomerName.Text = appointment.CustomerName ?? string.Empty;

                // Fill service - directly from service_id
                txtService.Text = appointment.Service ?? string.Empty;

                // Fill barber - directly from barber_id
                txtAssigne.Text = appointment.Barber ?? string.Empty;

                // Fill date - directly as string
                txtDate.Text = appointment.Date ?? string.Empty;

                // Fill time - Format as time only
                txtTime.Text = appointment.Time.HasValue
                    ? appointment.Time.Value.ToString(@"hh\:mm")
                    : string.Empty;

                // Fill payment method
                txtPayMethod.Text = appointment.PaymentMethod ?? string.Empty;

                // Fill total
                Total.Text = appointment.Total.HasValue
                    ? appointment.Total.Value.ToString("0.00")
                    : string.Empty;

                // Fill appointment status
                SetComboBoxByContent(cmbAppStatus, appointment.Status);

                // Fill payment status
                SetComboBoxByContent(cmbPayStatus, appointment.PaymentStatus);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating form: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetComboBoxByContent(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                comboBox.SelectedIndex = 0;
                return;
            }

            for (int i = 1; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item)
                {
                    if (item.Content.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private void ClearForm()
        {
            txtAppNumber.Clear();
            txtCustomerName.Clear();
            txtService.Clear();
            txtAssigne.Clear();
            txtDate.Clear();
            txtTime.Clear();
            txtPayMethod.Clear();
            Total.Clear();
            cmbAppStatus.SelectedIndex = 0;
            cmbPayStatus.SelectedIndex = 0;
            currentAppointment = null;

            // Clear error message when clearing form
            txtAppNumberError.Visibility = Visibility.Collapsed;
            txtAppNumberError.Text = string.Empty;
        }

        private void Clear()
        {
            txtCustomerName.Clear();
            txtService.Clear();
            txtAssigne.Clear();
            txtDate.Clear();
            txtTime.Clear();
            txtPayMethod.Clear();
            Total.Clear();
            cmbAppStatus.SelectedIndex = 0;
            cmbPayStatus.SelectedIndex = 0;
            currentAppointment = null;

            // Clear error message when clearing form
            txtAppNumberError.Visibility = Visibility.Collapsed;
            txtAppNumberError.Text = string.Empty;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appointmentNumber = txtAppNumber.Text.Trim();

                if (string.IsNullOrWhiteSpace(appointmentNumber))
                {
                    ShowError(txtAppNumberError, "Appointment Number is required");
                    return;
                }

                // Show delete confirmation
                ModalOverlay.Visibility = Visibility.Visible;

                // Open delete confirmation as a regular window
                currentModalWindow = new DeleteAppointment();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // Store reference for dialog result
                DeleteAppointment deleteDialog = (DeleteAppointment)currentModalWindow;

                // Subscribe to Closed event
                currentModalWindow.Closed += ModalWindow_Closed;

                // Show the dialog
                currentModalWindow.ShowDialog();

                if (deleteDialog.DialogResult != true)
                {
                    // User clicked No or closed the dialog
                    ModalOverlay.Visibility = Visibility.Collapsed;
                    return;
                }

                // User clicked Yes - proceed with deletion
                // Delete from database
                await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.Id == currentAppointment.Id)
                    .Delete();

                // Update local collection if it exists
                if (employees != null)
                {
                    var localEmployee = employees.FirstOrDefault(e => e.Id == currentAppointment.Id);
                    if (localEmployee != null)
                    {
                        employees.Remove(localEmployee);
                    }
                }

                // Show success message with modal
                ModalOverlay.Visibility = Visibility.Visible;

                // Open success window
                currentModalWindow = new AppointmentDeleteSuccessful();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // Subscribe to Closed event
                currentModalWindow.Closed += ModalWindow_Closed;

                // Show as regular window
                currentModalWindow.Show();

                // Clear the form
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting appointment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ModalOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private string GetComboBoxSelectedValue(ComboBox comboBox)
        {
            if (comboBox.SelectedIndex <= 0)
                return string.Empty;

            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content.ToString();
            }

            return string.Empty;
        }

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            if (errorTextBlock != null)
            {
                errorTextBlock.Text = message;
                errorTextBlock.Visibility = Visibility.Visible;
            }
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

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appointmentNumber = txtAppNumber.Text.Trim();

                if (string.IsNullOrWhiteSpace(appointmentNumber))
                {
                    ShowError(txtAppNumberError, "Appointment Number is required");
                    return;
                }

                // Validate that status fields are selected
                string appointmentStatus = GetComboBoxSelectedValue(cmbAppStatus);
                string paymentStatus = GetComboBoxSelectedValue(cmbPayStatus);

                if (string.IsNullOrEmpty(appointmentStatus))
                {
                    MessageBox.Show("Please select an Appointment Status.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(paymentStatus))
                {
                    MessageBox.Show("Please select a Payment Status.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Store the old status to check if we're changing to "Completed" or "No Show"
                string oldStatus = currentAppointment.Status;

                // Update the current appointment object
                currentAppointment.Status = appointmentStatus;
                currentAppointment.PaymentStatus = paymentStatus;

                // Save to database
                var updateResponse = await supabase
                    .From<BarbershopManagementSystem>()
                    .Update(currentAppointment);

                if (updateResponse != null)
                {
                    // Check if status was changed to "Completed" or "No Show" or "Approved"
                    bool wasChangedToCompleted = (oldStatus?.ToLower() != "completed" &&
                                                appointmentStatus.ToLower() == "completed");

                    bool wasChangedToNoShow = (oldStatus?.ToLower() != "no show" &&
                                             appointmentStatus.ToLower() == "no show");

                    bool wasChangedToApproved = (oldStatus?.ToLower() != "approved" &&
                                               appointmentStatus.ToLower() == "approved");

                    if (wasChangedToCompleted || wasChangedToNoShow || wasChangedToApproved)
                    {
                        Console.WriteLine($"\n🔄 Status changed to {appointmentStatus}: {currentAppointment.ReceiptCode}");

                        // Send notification if notification service is available
                        if (_notificationService != null)
                        {
                            try
                            {
                                await SendStatusNotification(currentAppointment, appointmentStatus);
                            }
                            catch (Exception notifEx)
                            {
                                Console.WriteLine($"💥 Notification error: {notifEx.Message}");
                                // Still show success for appointment update, but warn about notification
                                MessageBox.Show($"Appointment updated but notification failed: {notifEx.Message}",
                                    "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Notification service is not available");
                        }

                        // Optional: Verify the badge was updated if customer ID exists
                        if (wasChangedToCompleted && currentAppointment.CustomerId != Guid.Empty)
                        {
                            await CheckBadgeUpdate(currentAppointment.CustomerId);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ Status changed to: {appointmentStatus}, but not sending notification");
                    }

                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new ChangesSuccessfullyAppNumber();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    ClearForm();
                    return;
                }
                else
                {
                    MessageBox.Show("Failed to update appointment.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating appointment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendStatusNotification(BarbershopManagementSystem appointment, string status)
        {
            try
            {
                Console.WriteLine($"\n🔍 Checking push token for: {appointment.ReceiptCode}");
                Console.WriteLine($"📱 Token exists: {!string.IsNullOrEmpty(appointment.PushToken)}");

                if (!string.IsNullOrEmpty(appointment.PushToken))
                {
                    // Check if token is in valid format
                    bool isValidToken = _notificationService.IsValidPushToken(appointment.PushToken);
                    Console.WriteLine($"📱 Token valid format: {isValidToken}");

                    if (isValidToken)
                    {
                        Console.WriteLine($"📨 Sending {status} notification for: {appointment.ReceiptCode}");

                        var notificationSent = await _notificationService.SendAppointmentNotification(
                            appointment.PushToken,
                            appointment.Id.ToString(),
                            appointment.CustomerName,
                            appointment.ReceiptCode,
                            status,
                            appointment.CustomerId.ToString());

                        if (notificationSent)
                        {
                            Console.WriteLine($"✅ {status} notification sent successfully");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Failed to send {status} notification");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Invalid push token format for: {appointment.CustomerName}");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ No push token found for: {appointment.CustomerName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error sending {status} notification: {ex.Message}");
                throw; // Re-throw to be handled by caller
            }
        }

        // Method to check badge update (optional)
        private async Task CheckBadgeUpdate(Guid customerId)
        {
            try
            {
                var badgeResponse = await supabase
                    .From<BadgeTracker>()
                    .Where(x => x.CustomerId == customerId)
                    .Get();

                if (badgeResponse.Models.Count > 0)
                {
                    var badge = badgeResponse.Models.First();
                    Console.WriteLine($"🏅 Current badge: {badge.BadgeName}, Completed count: {badge.CompletedCount}");
                }
                else
                {
                    Console.WriteLine("❌ No badge record found for customer");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking badge: {ex.Message}");
            }
        }

        // Method to get badge progress for a customer
        public async Task<BadgeProgress> GetBadgeProgressAsync(Guid customerId)
        {
            try
            {
                // Query the badge_tracker table using CustomerId
                var response = await supabase
                    .From<BadgeTracker>()
                    .Where(x => x.CustomerId == customerId)
                    .Get();

                if (response.Models.Count > 0)
                {
                    var badge = response.Models.First();
                    return CalculateBadgeProgress(badge);
                }
                else
                {
                    // Return default progress if no badge found
                    return new BadgeProgress
                    {
                        CurrentBadge = "None",
                        CompletedCount = 0,
                        NextBadge = "Rookie",
                        ProgressToNext = 0,
                        NeededForNext = 1,
                        ProgressPercentage = 0
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting badge progress: {ex.Message}");
                return null;
            }
        }

        // Helper method to calculate badge progress
        private BadgeProgress CalculateBadgeProgress(BadgeTracker badge)
        {
            var progress = new BadgeProgress
            {
                CurrentBadge = badge.BadgeName,
                CompletedCount = badge.CompletedCount
            };

            // Calculate next badge and progress
            if (badge.CompletedCount < 1)
            {
                progress.NextBadge = "Rookie";
                progress.ProgressToNext = badge.CompletedCount;
                progress.NeededForNext = 1;
                progress.ProgressPercentage = (int)((badge.CompletedCount / 1.0) * 100);
            }
            else if (badge.CompletedCount < 5)
            {
                progress.NextBadge = "Loyal Customer";
                progress.ProgressToNext = badge.CompletedCount - 1;
                progress.NeededForNext = 5;
                progress.ProgressPercentage = (int)(((badge.CompletedCount - 1) / 4.0) * 100);
            }
            else if (badge.CompletedCount < 10)
            {
                progress.NextBadge = "Molave Street Legend";
                progress.ProgressToNext = badge.CompletedCount - 5;
                progress.NeededForNext = 10;
                progress.ProgressPercentage = (int)(((badge.CompletedCount - 5) / 5.0) * 100);
            }
            else
            {
                progress.NextBadge = "Max Level";
                progress.ProgressToNext = 0;
                progress.NeededForNext = 0;
                progress.ProgressPercentage = 100;
            }

            return progress;
        }

        // ============ NOTIFICATION SERVICE CLASS ============

        public class NotificationService
        {
            private static readonly HttpClient _httpClient = new HttpClient();
            private readonly string _edgeFunctionUrl;
            private readonly string _supabaseAnonKey;
            private static bool _isInitialized = false;
            private readonly Supabase.Client? _supabase;

            public NotificationService(Supabase.Client? supabase)
            {
                _supabase = supabase;
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

            public async Task<bool> SendAppointmentNotification(string pushToken, string appointmentId, string customerName, string receiptCode, string status, string userId)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(pushToken))
                    {
                        Console.WriteLine("❌ Push token is null or empty");
                        return false;
                    }

                    if (!IsValidPushToken(pushToken))
                    {
                        Console.WriteLine("❌ Push token format is invalid");
                        return false;
                    }

                    Console.WriteLine($"📨 Sending {status} notification for: {receiptCode}");

                    var (title, message) = GetNotificationTemplate(status);

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
                            source = "appointment_tracker",
                            timestamp = DateTime.UtcNow.ToString("o")
                        },
                        sound = "default",
                        priority = "high",
                        channelId = "default"
                    };

                    var success = await SendNotificationRequest(payload);

                    // Save to notification_loader for important statuses
                    if (_supabase != null && ShouldSaveToNotificationLoader(status))
                    {
                        bool loaderSuccess = false;

                        try
                        {
                            loaderSuccess = await InsertNotificationToLoader(title, message, receiptCode, userId);
                        }
                        catch (Exception reloadEx) when (reloadEx.Message.Contains("deleted method") || reloadEx.Message.Contains("HotReload"))
                        {
                            Console.WriteLine($"🔥 Hot reload issue detected - continuing without database save: {reloadEx.Message}");
                            loaderSuccess = true; // Continue anyway to not block notifications
                        }

                        // Silent operation - no alert boxes shown to user
                        if (!loaderSuccess)
                        {
                            Console.WriteLine($"⚠️ Notification sent but failed to save to database for: {receiptCode}");
                        }
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Error in SendAppointmentNotification: {ex.Message}");
                    return false;
                }
            }

            private bool ShouldSaveToNotificationLoader(string status)
            {
                return status.ToLower() switch
                {
                    "completed" => true,
                    "no show" => true,
                    "approved" => true,
                    _ => false
                };
            }

            private async Task<bool> InsertNotificationToLoader(string title, string description, string receiptId, string userId)
            {
                try
                {
                    // Debug mode detection
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        Console.WriteLine("⚠️ Debugger attached - application may be in debug mode");
                    }

                    if (_supabase == null)
                    {
                        Console.WriteLine("⚠️ Supabase client not available");
                        return false;
                    }

                    if (!Guid.TryParse(userId, out Guid userGuid))
                    {
                        Console.WriteLine($"⚠️ Invalid user_id format: '{userId}'");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        Console.WriteLine($"⚠️ Title cannot be empty");
                        return false;
                    }

                    // Ensure required fields have values
                    if (string.IsNullOrWhiteSpace(description))
                        description = "No description";
                    if (string.IsNullOrWhiteSpace(receiptId))
                        receiptId = "N/A";

                    // Trim fields if too long
                    if (title.Length > 255) title = title.Substring(0, 255);
                    if (description.Length > 1000) description = description.Substring(0, 1000);

                    Console.WriteLine($"📝 Inserting into notification_loader for: {receiptId}");

                    var notification = new NotificationLoaderModel
                    {
                        UserId = userGuid,
                        ReceiptId = receiptId,
                        Title = title,
                        Description = description,
                        CreatedAt = DateTime.UtcNow,
                        Read = false  // Explicitly set to false
                    };

                    // Use a robust approach to handle insertion
                    var result = await _supabase
                        .From<NotificationLoaderModel>()
                        .Insert(notification);

                    // Check for successful insertion using multiple methods
                    if (result != null)
                    {
                        // Check if we have models (normal case)
                        if (result.Models != null && result.Models.Count > 0)
                        {
                            var insertedNotification = result.Models[0];
                            Console.WriteLine($"✅ Notification saved to loader. ID: {insertedNotification.Id}, Read: {insertedNotification.Read}");
                            return true;
                        }
                        // Also check if response was successful even if Models is null
                        else if (result.ResponseMessage?.IsSuccessStatusCode == true)
                        {
                            Console.WriteLine($"✅ Notification saved successfully (alternative check)");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Insertion may have failed - no models returned");
                            // Try to get more info
                            if (result.ResponseMessage != null)
                            {
                                Console.WriteLine($"⚠️ HTTP Status: {result.ResponseMessage.StatusCode}");
                                var responseContent = await result.ResponseMessage.Content.ReadAsStringAsync();
                                if (!string.IsNullOrEmpty(responseContent))
                                {
                                    Console.WriteLine($"⚠️ Response: {responseContent}");
                                }
                            }
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Insert returned null result");
                        return false;
                    }
                }
                catch (Supabase.Postgrest.Exceptions.PostgrestException pgEx)
                {
                    Console.WriteLine($"❌ PostgreSQL error: {pgEx.Message}");
                    Console.WriteLine($"❌ PostgreSQL details: {pgEx.Reason}");
                    return false;
                }
                catch (Exception ex) when (ex.Message.Contains("deleted method") || ex.Message.Contains("HotReload"))
                {
                    Console.WriteLine($"🔥 Hot reload issue detected - please restart application: {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error inserting into loader: {ex.Message}");
                    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                    return false;
                }
            }

            private (string title, string message) GetNotificationTemplate(string status)
            {
                return status switch
                {
                    "Completed" => (
                        "Molave Street Barbers",
                        "Your appointment has been marked as completed. Thank you for visiting Molave Street Barbers!"
                    ),
                    "Approved" => (
                        "Molave Street Barbers",
                        "Your appointment request has been approved! We'll see you soon at your selected time."
                    ),
                    "Declined" => (
                        "Molave Street Barbers",
                        "Your appointment request has been declined. Please contact us for more information."
                    ),
                    "No Show" => (
                        "Molave Street Barbers",
                        "You didn't attend your scheduled appointment. Repeated no-shows can lead to booking restrictions. Please schedule responsibly."
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
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(_edgeFunctionUrl, content);
                    responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            Console.WriteLine("⚠️ Empty response from Edge Function");
                            return false;
                        }

                        var result = JObject.Parse(responseContent);
                        var success = result["success"]?.Value<bool>() ?? false;

                        if (success)
                        {
                            Console.WriteLine("✅ Notification sent successfully");
                            return true;
                        }
                        else
                        {
                            var error = result["error"]?.ToString() ?? "Unknown error";
                            Console.WriteLine($"❌ Edge function error: {error}");
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
                    Console.WriteLine($"💥 Request error: {ex.Message}");
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
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("customer_name")]
            public string CustomerName { get; set; } = string.Empty;

            [Column("service_id")]
            public string Service { get; set; } = string.Empty;

            [Column("barber_id")]
            public string Barber { get; set; } = string.Empty;

            [Column("sched_date")]
            public string Date { get; set; } = string.Empty;

            [Column("sched_time")]
            public TimeSpan? Time { get; set; }

            [Column("total")]
            public decimal? Total { get; set; }

            [Column("payment_method")]
            public string PaymentMethod { get; set; } = string.Empty;

            [Column("status")]
            public string Status { get; set; } = "On Going";

            [Column("payment_status")]
            public string PaymentStatus { get; set; } = string.Empty;

            [Column("receipt_code")]
            public string ReceiptCode { get; set; } = string.Empty;

            [Column("customer_id")]
            public Guid CustomerId { get; set; }

            [Column("push_token")]
            public string? PushToken { get; set; }
        }

        [Table("badge_tracker")]
        public class BadgeTracker : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("customer_id")]
            public Guid CustomerId { get; set; }

            [Column("completed_count")]
            public int CompletedCount { get; set; }

            [Column("badge_name")]
            public string BadgeName { get; set; } = "None";

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }
        }

        [Table("notification_loader")]
        public class NotificationLoaderModel : BaseModel
        {
            [PrimaryKey("id", false)] // FIXED: Changed from true to false - let database handle auto-increment
            public int Id { get; set; }

            [Column("user_id")]
            public Guid UserId { get; set; }

            [Column("receipt_id")]
            public string ReceiptId { get; set; } = string.Empty; // FIXED: Don't make nullable

            [Column("title")]
            public string Title { get; set; } = string.Empty;

            [Column("description")]
            public string Description { get; set; } = string.Empty; // FIXED: Don't make nullable

            [Column("created_at")]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // FIXED: Set default value

            [Column("read")]
            public bool Read { get; set; } = false; // FIXED: Set default value to false
        }

        // Class to represent badge progress
        public class BadgeProgress
        {
            public string CurrentBadge { get; set; } = "None";
            public int CompletedCount { get; set; }
            public string NextBadge { get; set; } = "Rookie";
            public int ProgressToNext { get; set; }
            public int NeededForNext { get; set; }
            public int ProgressPercentage { get; set; }
        }
    }
}