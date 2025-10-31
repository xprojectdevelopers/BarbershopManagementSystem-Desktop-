using System;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
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

namespace Capstone.AppointmentOptions
{
    /// <summary>
    /// Interaction logic for AppointmentTracker.xaml
    /// </summary>
    public partial class AppointmentTracker : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private BarbershopManagementSystem currentAppointment;
        private Window currentModalWindow;

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
            supabase = new Client(supabaseUrl, supabaseKey, new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });
            await supabase.InitializeAsync();
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appointmentNumber = txtAppNumber.Text.Trim();

                // Fix: Check the actual input TextBox, not the error TextBlock
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

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Fix: Check the actual input TextBox
                if (string.IsNullOrWhiteSpace(txtAppNumber.Text))
                {
                    ShowError(txtAppNumberError, "Appointment Number is required");
                    return;
                }

                // Clear error message
                txtAppNumberError.Visibility = Visibility.Collapsed;
                txtAppNumberError.Text = string.Empty;

                // Check if an appointment has been loaded
                if (currentAppointment == null)
                {
                    MessageBox.Show("Please search for an appointment first.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get selected values from ComboBoxes
                string appointmentStatus = GetComboBoxSelectedValue(cmbAppStatus);
                string paymentStatus = GetComboBoxSelectedValue(cmbPayStatus);

                // Validate that both statuses are selected
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

                // Store original date and time to preserve them
                string originalDate = currentAppointment.Date;
                TimeSpan? originalTime = currentAppointment.Time;

                // Update only the status fields
                currentAppointment.Status = appointmentStatus;
                currentAppointment.PaymentStatus = paymentStatus;

                // Ensure date and time remain unchanged
                currentAppointment.Date = originalDate;
                currentAppointment.Time = originalTime;

                // Update in database using the model instance
                await supabase
                    .From<BarbershopManagementSystem>()
                    .Update(currentAppointment);

                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new ChangesSuccessfullyAppNumber();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating appointment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        [Table("appointment_sched")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }
            [Column("customer_name")] public string CustomerName { get; set; } = string.Empty;
            [Column("service_id")] public string Service { get; set; } = string.Empty;
            [Column("barber_id")] public string Barber { get; set; } = string.Empty;
            [Column("sched_date")] public string Date { get; set; } = string.Empty;
            [Column("sched_time")] public TimeSpan? Time { get; set; }
            [Column("total")] public decimal? Total { get; set; }
            [Column("payment_method")] public string PaymentMethod { get; set; } = string.Empty;
            [Column("status")] public string Status { get; set; } = "On Going";
            [Column("payment_status")] public string PaymentStatus { get; set; } = string.Empty;
            [Column("receipt_code")] public string ReceiptCode { get; set; } = string.Empty;
        }
    }
}