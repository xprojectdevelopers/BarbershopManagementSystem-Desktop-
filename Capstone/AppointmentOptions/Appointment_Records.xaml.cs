using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Capstone.AppointmentOptions
{
    [Table("appointment_sched")]
    public class AppointmentModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("sched_date")]
        public string AppointmentDate { get; set; }

        [Column("sched_time")]
        public string AppointmentTime { get; set; }

        [Column("customer_name")]
        public string CustomerName { get; set; }

        [Column("contact_number")]
        public string ContactNumber { get; set; }

        [Column("barber_id")]
        public string BarberAssigned { get; set; }

        [Column("status")]
        public string AppointmentStatus { get; set; }
    }

    public partial class Appointment_Records : Window
    {
        private Window currentModalWindow;
        private Supabase.Client client;
        private ObservableCollection<AppointmentModel> appointments = new ObservableCollection<AppointmentModel>();

        public Appointment_Records()
        {
            InitializeComponent();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;

            // Bind DataGrid
            AppointmentDataGrid.ItemsSource = appointments;

            // Initialize Supabase and load data
            InitializeSupabase();
        }

        private async void InitializeSupabase()
        {
            try
            {
                // Read from app.config
                string supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
                string supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

                client = new Supabase.Client(supabaseUrl, supabaseKey);
                await client.InitializeAsync();

                await LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Supabase: {ex.Message}");
            }
        }

        private async Task LoadAppointments()
        {
            try
            {
                var response = await client.From<AppointmentModel>().Get();

                if (response.Models != null && response.Models.Count > 0)
                {
                    appointments.Clear();
                    foreach (var appt in response.Models)
                    {
                        appointments.Add(appt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments: {ex.Message}");
            }
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Appointments Appointments = new Appointments();
            Appointments.Show();
            this.Close();
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;
            currentModalWindow = new ModalsNotification();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 95;
            currentModalWindow.Top = this.Top + 110;
            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;
            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 95;
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
            if (currentModalWindow != null)
            {
                currentModalWindow.Close();
            }
            e.Handled = true;
        }
    }
}
