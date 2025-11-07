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
    }
}