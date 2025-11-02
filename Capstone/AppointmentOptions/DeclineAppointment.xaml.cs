using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Supabase.Postgrest.Constants;

namespace Capstone.AppointmentOptions
{
    public partial class DeclineAppointment : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<AppointmentModel> appointments = new();
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
                // Update both Status and Reason_Decline in database
                var updated = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.Id == SelectedAppointment.Id)
                    .Set(x => x.Status, "Declined")
                    .Set(x => x.ReasonDecline, selectedReason)
                    .Update();

                if (updated.Models != null && updated.Models.Count > 0)
                {
                    // Call the decline action to remove from table
                    if (OnConfirmDecline != null)
                    {
                        OnConfirmDecline(SelectedAppointment, selectedReason);
                    }

                    // Show success modal on the main Appointments window
                    if (this.Owner != null)
                    {
                        var successModal = new DeclineAppointmentSuccess();
                        successModal.Owner = this.Owner; // Use Appointments window as owner
                        successModal.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        successModal.Show();
                    }
                }
                else
                {
                    MessageBox.Show("⚠️ No rows were updated.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error declining appointment: {ex.Message}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Just close the modal without doing anything
            this.Close();
        }

        [Table("appointment_sched")]
        public class AppointmentModel : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("status")]
            public string Status { get; set; } = string.Empty;

            [Column("Reason_Decline")]
            public string ReasonDecline { get; set; } = string.Empty;
        }
    }
}