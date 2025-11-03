using Microsoft.IdentityModel.Tokens;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Capstone.AppointmentOptions
{
    /// <summary>
    /// Interaction logic for Book_Appointment.xaml
    /// </summary>
    public partial class Book_Appointment : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private ObservableCollection<AssignNewService> assignedServices;
        private Window currentModalWindow;

        public Book_Appointment()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
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

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();

            employees = new ObservableCollection<BarbershopManagementSystem>();
            assignedServices = new ObservableCollection<AssignNewService>();

            // Fetch appointment data
            var result = await supabase.From<BarbershopManagementSystem>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }

            // Fetch assigned services data
            var servicesResult = await supabase.From<AssignNewService>().Get();
            foreach (var service in servicesResult.Models)
            {
                assignedServices.Add(service);
            }

            // Populate Assigned Barber ComboBox with unique barber names
            var uniqueBarbers = assignedServices
                .Select(s => s.BarberNickname)
                .Distinct()
                .OrderBy(b => b);

            cmbAssignedBarber.Items.Clear();
            cmbAssignedBarber.Items.Add(new ComboBoxItem
            {
                Content = "Select Assigned Barber",
                IsEnabled = false,
                IsSelected = true,
                Foreground = Brushes.Gray
            });

            foreach (var barber in uniqueBarbers)
            {
                cmbAssignedBarber.Items.Add(new ComboBoxItem { Content = barber });
            }

            // Attach event handler for barber selection
            cmbAssignedBarber.SelectionChanged += CmbAssignedBarber_SelectionChanged;
            cmbService.SelectionChanged += CmbService_SelectionChanged;
        }

        private void CmbAssignedBarber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAssignedBarber.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content.ToString() != "Select Assigned Barber")
            {
                string selectedBarber = selectedItem.Content.ToString();

                // Get services for selected barber
                var barberServices = assignedServices
                    .Where(s => s.BarberNickname == selectedBarber)
                    .Select(s => new { s.Service, s.Price })
                    .ToList();

                // Populate Service ComboBox
                cmbService.Items.Clear();
                cmbService.Items.Add(new ComboBoxItem
                {
                    Content = "Select Service",
                    IsEnabled = false,
                    IsSelected = true,
                    Foreground = Brushes.Gray
                });

                foreach (var service in barberServices)
                {
                    var item = new ComboBoxItem { Content = service.Service, Tag = service.Price };
                    cmbService.Items.Add(item);
                }

                cmbService.IsEnabled = true;

                // Reset service and total
                cmbService.SelectedIndex = 0;
                txtTotal.Text = "";
            }
            else
            {
                // Reset if no valid barber selected
                cmbService.Items.Clear();
                cmbService.Items.Add(new ComboBoxItem
                {
                    Content = "Select Service",
                    IsEnabled = false,
                    IsSelected = true,
                    Foreground = Brushes.Gray
                });
                cmbService.IsEnabled = false;
                txtTotal.Text = "";
            }
        }

        private void CmbService_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbService.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content.ToString() != "Select Service" &&
                selectedItem.Tag != null)
            {
                decimal basePrice = Convert.ToDecimal(selectedItem.Tag);
                decimal appointmentFee = 50;
                decimal totalWithFee = basePrice + appointmentFee;

                // Display total without .00
                txtTotal.Text = totalWithFee.ToString("0");
            }
            else
            {
                txtTotal.Text = "";
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

        private void View_Click(object sender, RoutedEventArgs e)
        {
            // Get the values from the form
            string appointmentNumber = string.IsNullOrWhiteSpace(txtItemID.Text) ? null : txtItemID.Text.Trim();

            DateTime? appointmentDate = date.SelectedDate;

            TimeSpan? appointmentTime = null;
            if (cmbTime.SelectedIndex > 0)
            {
                string timeString = (cmbTime.SelectedItem as ComboBoxItem)?.Content.ToString();
                appointmentTime = ParseTimeString(timeString);
            }

            string paymentStatus = null;
            if (cmbPStatus.SelectedIndex > 0)
            {
                paymentStatus = (cmbPStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
            }

            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ViewReceipt(appointmentNumber, appointmentDate, appointmentTime, paymentStatus);
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void btnGeneratedNum_Click(object sender, RoutedEventArgs e)
        {
            string prefix = "MSB";
            int nextNumber = 1;

            var existingReceiptCodes = employees
                .Where(emp => !string.IsNullOrEmpty(emp.ReceiptCode) && emp.ReceiptCode.StartsWith(prefix))
                .Select(emp => emp.ReceiptCode)
                .ToList();

            if (existingReceiptCodes.Any())
            {
                var maxNumber = existingReceiptCodes
                    .Select(code =>
                    {
                        var parts = code.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int num))
                            return num;
                        return 0;
                    })
                    .Max();

                nextNumber = maxNumber + 1;
            }

            string newReceiptCode = $"{prefix}-{nextNumber:D4}";
            txtItemID.Text = newReceiptCode;
        }

        private async void BookNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reset all error messages
                txtAppNumberError.Visibility = Visibility.Collapsed;
                txtAppNumberSame.Visibility = Visibility.Collapsed;
                txtCustomerError.Visibility = Visibility.Collapsed;
                txtContactError.Visibility = Visibility.Collapsed;
                cmbAssignedBarberError.Visibility = Visibility.Collapsed;
                cmbServiceError.Visibility = Visibility.Collapsed;
                dateErorr.Visibility = Visibility.Collapsed;
                dateSame.Visibility = Visibility.Collapsed;
                cmbTimeErorr.Visibility = Visibility.Collapsed;
                cmbTimeSame.Visibility = Visibility.Collapsed;
                txtTotalErorr.Visibility = Visibility.Collapsed;
                cmbPMethodError.Visibility = Visibility.Collapsed;
                cmbPStatusErorr.Visibility = Visibility.Collapsed;

                bool hasError = false;

                // Validate Appointment Number
                if (string.IsNullOrWhiteSpace(txtItemID.Text))
                {
                    txtAppNumberError.Text = "Please generate an appointment number.";
                    txtAppNumberError.Visibility = Visibility.Visible;
                    hasError = true;
                }

                // Validate Customer Name
                if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
                {
                    txtCustomerError.Text = "Please enter customer name.";
                    txtCustomerError.Visibility = Visibility.Visible;
                    hasError = true;
                }

                // Validate Contact Number
                if (string.IsNullOrWhiteSpace(txtConatct.Text))
                {
                    txtContactError.Text = "Please enter contact number.";
                    txtContactError.Visibility = Visibility.Visible;
                    hasError = true;
                }
                else
                {
                    string contactNumber = txtConatct.Text.Trim();

                    // Check if contains only digits
                    if (!Regex.IsMatch(contactNumber, @"^\d+$"))
                    {
                        txtContactError.Text = "Contact number must contain only numbers.";
                        txtContactError.Visibility = Visibility.Visible;
                        hasError = true;
                    }
                    // Check if exactly 11 digits
                    else if (contactNumber.Length != 11)
                    {
                        txtContactError.Text = "Contact number must be exactly 11 digits.";
                        txtContactError.Visibility = Visibility.Visible;
                        hasError = true;
                    }
                }

                // Validate Assigned Barber
                if (cmbAssignedBarber.SelectedIndex <= 0)
                {
                    cmbAssignedBarberError.Text = "Please select an assigned barber.";
                    cmbAssignedBarberError.Visibility = Visibility.Visible;
                    hasError = true;
                }

                // Validate Service
                if (cmbService.SelectedIndex <= 0)
                {
                    cmbServiceError.Text = "Please select a service.";
                    cmbServiceError.Visibility = Visibility.Visible;
                    hasError = true;
                }

                // Validate Date
                if (!date.SelectedDate.HasValue)
                {
                    dateErorr.Text = "Please select a date.";
                    dateErorr.Visibility = Visibility.Visible;
                    hasError = true;
                }
                else
                {
                    // Check for past dates
                    DateTime selectedDate = date.SelectedDate.Value.Date;
                    DateTime today = DateTime.Today;

                    if (selectedDate < today)
                    {
                        dateSame.Text = "Cannot select a past date.";
                        dateSame.Visibility = Visibility.Visible;
                        hasError = true;
                    }
                }

                // Validate Time
                if (cmbTime.SelectedIndex <= 0)
                {
                    cmbTimeErorr.Text = "Please select a time.";
                    cmbTimeErorr.Visibility = Visibility.Visible;
                    hasError = true;
                }

                // Validate Payment Method
                if (cmbPMethod.SelectedIndex <= 0)
                {
                    cmbPMethodError.Text = "Please select a payment method.";
                    cmbPMethodError.Visibility = Visibility.Visible;
                    hasError = true;
                }

                // Validate Payment Status
                if (cmbPStatus.SelectedIndex <= 0)
                {
                    cmbPStatusErorr.Text = "Please select a payment status.";
                    cmbPStatusErorr.Visibility = Visibility.Visible;
                    hasError = true;
                }

                // If any basic validation failed, stop here
                if (hasError)
                {
                    return;
                }

                // Check for duplicate appointment number from the already loaded employees collection
                string receiptCode = txtItemID.Text.Trim();
                bool isDuplicateReceipt = employees.Any(x => x.ReceiptCode == receiptCode);

                if (isDuplicateReceipt)
                {
                    txtAppNumberSame.Text = "This appointment number already exists.";
                    txtAppNumberSame.Visibility = Visibility.Visible;
                    return;
                }

                // Additional validation: Check for duplicate date, time, and barber combination
                string selectedBarberNickname = (cmbAssignedBarber.SelectedItem as ComboBoxItem)?.Content.ToString();
                string timeString = (cmbTime.SelectedItem as ComboBoxItem)?.Content.ToString();
                TimeSpan scheduledTime = ParseTimeString(timeString);
                string selectedDateString = date.SelectedDate.Value.ToString("yyyy-MM-dd");

                // Get employee info for barber ID
                var employeeResult = await supabase.From<AddEmployee>()
                    .Get();

                var employee = employeeResult.Models.FirstOrDefault(x => x.EmployeeNickname == selectedBarberNickname);

                if (employee == null)
                {
                    MessageBox.Show("Barber not found in employee database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string barberId = $"{employee.FullName} - {employee.EmployeeRole}";

                // Check for conflicting appointments from already loaded employees collection
                var conflictingAppointment = employees.FirstOrDefault(x =>
                    x.Date == selectedDateString &&
                    x.Barber == barberId &&
                    x.Time == scheduledTime);

                if (conflictingAppointment != null)
                {
                    cmbTimeSame.Text = "This time slot is already booked for this barber";
                    cmbTimeSame.Visibility = Visibility.Visible;
                    return;
                }

                // Proceed with booking
                var selectedServiceItem = cmbService.SelectedItem as ComboBoxItem;
                string selectedService = selectedServiceItem?.Content.ToString();

                var serviceResult = assignedServices.FirstOrDefault(s =>
                    s.BarberNickname == selectedBarberNickname &&
                    s.Service == selectedService);

                if (serviceResult == null)
                {
                    MessageBox.Show("Service not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Calculate totals
                decimal subtotalVal = Convert.ToDecimal(serviceResult.Price);
                decimal appointmentFeeVal = 50;
                decimal totalVal = subtotalVal + appointmentFeeVal;

                string subtotal = subtotalVal.ToString("0");
                string appointmentFee = appointmentFeeVal.ToString("0");
                string total = totalVal.ToString("0");

                var newAppointment = new BarbershopManagementSystem
                {
                    CustomerBadge = null,
                    CustomerName = txtCustomerName.Text.Trim(),
                    ContactNumber = txtConatct.Text.Trim(),
                    Service = selectedService,
                    Barber = barberId,
                    Date = selectedDateString,
                    Time = scheduledTime,
                    Subtotal = subtotal,
                    AppointmentFee = appointmentFee,
                    Total = total,
                    PaymentMethod = (cmbPMethod.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    PaymentStatus = (cmbPStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    ReceiptCode = txtItemID.Text.Trim(),
                    Status = "On Going"
                };

                await supabase.From<BarbershopManagementSystem>().Insert(newAppointment);

                // Show the overlay FIRST
                ModalOverlay.Visibility = Visibility.Visible;

                // Open PurchaseOrders as a regular window
                currentModalWindow = new BookAppointmentSuccessful();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // Subscribe to Closed event
                currentModalWindow.Closed += ModalWindow_Closed;

                // Show as regular window
                currentModalWindow.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error booking appointment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TimeSpan ParseTimeString(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return TimeSpan.Zero;

            string cleanTime = timeString.Replace("am", "").Replace("pm", "").Trim();
            var parts = cleanTime.Split(':');
            int hour = int.Parse(parts[0]);
            int minute = parts.Length > 1 ? int.Parse(parts[1]) : 0;

            if (timeString.ToLower().Contains("pm") && hour != 12)
            {
                hour += 12;
            }
            else if (timeString.ToLower().Contains("am") && hour == 12)
            {
                hour = 0;
            }

            return new TimeSpan(hour, minute, 0);
        }

        [Table("appointment_sched")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("customer_badge")] public string CustomerBadge { get; set; } = null;
            [Column("customer_name")] public string CustomerName { get; set; } = string.Empty;
            [Column("contact_number")] public string ContactNumber { get; set; } = string.Empty;
            [Column("service_id")] public string Service { get; set; } = string.Empty;
            [Column("barber_id")] public string Barber { get; set; } = string.Empty;
            [Column("sched_date")] public string Date { get; set; } = string.Empty;
            [Column("sched_time")] public TimeSpan? Time { get; set; }
            [Column("subtotal")] public string Subtotal { get; set; }
            [Column("appointment_fee")] public string AppointmentFee { get; set; }
            [Column("total")] public string Total { get; set; }
            [Column("payment_method")] public string PaymentMethod { get; set; } = string.Empty;
            [Column("status")] public string Status { get; set; } = "On Going";
            [Column("payment_status")] public string PaymentStatus { get; set; } = string.Empty;
            [Column("receipt_code")] public string ReceiptCode { get; set; } = string.Empty;
        }

        [Table("AssignNew_Service")]
        public class AssignNewService : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }
            [Column("Barber_Nickname")] public string BarberNickname { get; set; } = string.Empty;
            [Column("Service")] public string Service { get; set; } = string.Empty;
            [Column("Price")] public string Price { get; set; }
        }

        [Table("Add_Employee")]
        public class AddEmployee : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }
            [Column("Full_Name")] public string FullName { get; set; } = string.Empty;
            [Column("Employee_Role")] public string EmployeeRole { get; set; } = string.Empty;
            [Column("Employee_Nickname")] public string EmployeeNickname { get; set; } = string.Empty;
        }
    }
}