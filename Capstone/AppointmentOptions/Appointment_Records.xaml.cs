using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private List<AppointmentModel> allAppointments = new List<AppointmentModel>(); // Store all data for filtering

        public Appointment_Records()
        {
            InitializeComponent();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;

            // Bind DataGrid
            AppointmentDataGrid.ItemsSource = appointments;

            // Initialize Supabase and load data
            InitializeSupabase();
        }

        // Add this method to be called from your XAML DatePicker
        private void DateFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allAppointments.Count > 0)
            {
                ApplyFilters();
            }
        }

        // Add this method to be called from your XAML ComboBox
        private void BarberFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allAppointments.Count > 0)
            {
                ApplyFilters();
            }
        }

        // Add this method for the Sort button
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
            MessageBox.Show("Filters applied successfully!", "Sort Complete", MessageBoxButton.OK, MessageBoxImage.Information);
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
                PopulateBarberDropdown();
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
                    allAppointments = response.Models.ToList();
                    appointments.Clear();
                    foreach (var appt in allAppointments)
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

        // Populate barber dropdown with unique barber names
        private void PopulateBarberDropdown()
        {
            // Find the ComboBox in your XAML (you need to give it x:Name)
            var barberComboBox = FindName("BarberComboBox") as ComboBox;

            if (barberComboBox != null)
            {
                // Get unique barber names (sorted)
                var uniqueBarbers = allAppointments
                    .Where(a => !string.IsNullOrEmpty(a.BarberAssigned))
                    .Select(a => a.BarberAssigned)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();

                // Keep the first item as "All Barber"
                barberComboBox.Items.Clear();

                // Add default item
                var defaultItem = new ComboBoxItem
                {
                    Content = "All Barber",
                    IsEnabled = false,
                    IsSelected = true,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                barberComboBox.Items.Add(defaultItem);

                // Add static barber list (from your database)
                var staticBarbers = new List<string>
                {
                    "Barber - Zer",
                    "Barber - Aurbey",
                    "Barber - Klein Eagle",
                    "Barber - Aljames",
                    "Barber - arel",
                    "Barber - jay r",
                    "Barber - cube",
                    "Barber - Andrei"
                };

                // Combine static list with unique barbers from database (avoid duplicates)
                var allBarbers = staticBarbers
                    .Union(uniqueBarbers)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();

                // Add all barbers to dropdown
                foreach (var barber in allBarbers)
                {
                    barberComboBox.Items.Add(new ComboBoxItem { Content = barber });
                }

                barberComboBox.SelectedIndex = 0;
            }
        }

        // Filter appointments based on selected date and barber
        private void ApplyFilters()
        {
            var datePicker = FindName("DateFilterPicker") as DatePicker;
            var barberComboBox = FindName("BarberComboBox") as ComboBox;

            var filtered = allAppointments.AsEnumerable();

            // Filter by date
            if (datePicker != null && datePicker.SelectedDate.HasValue)
            {
                string selectedDate = datePicker.SelectedDate.Value.ToString("yyyy-MM-dd");
                filtered = filtered.Where(a => a.AppointmentDate == selectedDate);
            }

            // Filter by barber
            if (barberComboBox != null && barberComboBox.SelectedIndex > 0)
            {
                var selectedItem = barberComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string selectedBarber = selectedItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedBarber))
                    {
                        filtered = filtered.Where(a => a.BarberAssigned == selectedBarber);
                    }
                }
            }

            // Update the ObservableCollection
            appointments.Clear();
            foreach (var appt in filtered)
            {
                appointments.Add(appt);
            }

            // Show message if no results
            if (appointments.Count == 0)
            {
                MessageBox.Show("No appointments found for the selected filters.", "No Results", MessageBoxButton.OK, MessageBoxImage.Information);
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
