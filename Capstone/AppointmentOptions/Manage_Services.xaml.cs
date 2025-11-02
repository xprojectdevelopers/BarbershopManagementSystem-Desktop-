using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Supabase.Postgrest.Constants;

namespace Capstone.AppointmentOptions
{
    // ✅ CONVERTERS MUST BE INSIDE THE NAMESPACE
    public class PesoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(str))
                return "";

            if (decimal.TryParse(str, out decimal number))
            {
                if (number == Math.Floor(number))
                    return $"₱ {number:N0}";
                else
                    return $"₱ {number:N2}";
            }

            return $"₱ {str}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            string str = value.ToString();
            return str.Replace("₱", "").Trim();
        }
    }

    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;
            return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class Manage_Services : Window
    {
        private Window currentModalWindow;
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> allEmployees = new ObservableCollection<BarbershopManagementSystem>();
        private ObservableCollection<BarbershopManagementSystem> employees = new ObservableCollection<BarbershopManagementSystem>();

        private int CurrentPage = 1;
        private int PageSize = 5;
        private int TotalPages = 1;

        public Manage_Services()
        {
            InitializeComponent();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await SyncCompletedAppointments(); // Sync completed appointments first
            await LoadEmployees();
        }

        private async Task InitializeSupabaseAsync()
        {
            string? supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string? supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                MessageBox.Show("Supabase configuration missing in App.config!");
                return;
            }

            supabase = new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        // ✅ NEW METHOD: Sync completed appointments to Manage Service
        private async Task SyncCompletedAppointments()
        {
            if (supabase == null) return;

            try
            {
                // Get all completed appointments
                var completedAppointments = await supabase
                    .From<AppointmentModel>()
                    .Where(x => x.AppointmentStatus == "Completed")
                    .Get();

                if (completedAppointments.Models == null || completedAppointments.Models.Count == 0)
                    return;

                // Get all employees to match barber name to Employee_ID
                var employees = await supabase
                    .From<Employee>()
                    .Get();

                // Get existing services to avoid duplicates
                var existingServices = await supabase
                    .From<BarbershopManagementSystem>()
                    .Get();

                foreach (var appointment in completedAppointments.Models)
                {
                    // Skip if barber not assigned
                    if (string.IsNullOrEmpty(appointment.BarberAssigned))
                        continue;

                    // Extract barber name from "MawPatalingjug - Barber" format
                    string barberName = appointment.BarberAssigned;

                    // Remove " - Barber" suffix if it exists
                    if (barberName.Contains(" - Barber"))
                    {
                        barberName = barberName.Replace(" - Barber", "").Trim();
                    }
                    else if (barberName.Contains("-"))
                    {
                        // Handle other formats like "MawPatalingjug-Barber"
                        barberName = barberName.Split('-')[0].Trim();
                    }

                    // Find employee by Full_Name matching the extracted barber name
                    var employee = employees.Models.FirstOrDefault(e =>
                        !string.IsNullOrEmpty(e.Fname) &&
                        e.Fname.Trim().Equals(barberName, StringComparison.OrdinalIgnoreCase));

                    if (employee == null)
                    {
                        // Log for debugging - could not find employee
                        System.Diagnostics.Debug.WriteLine($"Could not find employee with name: {barberName}");
                        continue;
                    }

                    // Check if this service already exists (avoid duplicates)
                    bool serviceExists = existingServices.Models.Any(s =>
                        s.EmiD == employee.EmID &&
                        s.Service == appointment.ServiceId &&
                        s.Price == appointment.Total);

                    if (serviceExists)
                        continue;

                    // Create new service entry
                    var newService = new BarbershopManagementSystem
                    {
                        EmiD = employee.EmID,              // Employee_ID (e.g., MSB-2025-0004)
                        BN = employee.EmployeeNickname,     // Employee_Nickname (e.g., Meowru)
                        Service = appointment.ServiceId ?? "N/A",  // Service (e.g., Haircut/Wash)
                        Price = appointment.Total ?? "0"    // Price (e.g., 300)
                    };

                    // Insert into database
                    await supabase
                        .From<BarbershopManagementSystem>()
                        .Insert(newService);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error syncing completed appointments: {ex.Message}");
            }
        }

        private async Task LoadEmployees()
        {
            if (supabase == null) return;

            var result = await supabase
                .From<BarbershopManagementSystem>()
                .Order(x => x.EmiD, Ordering.Ascending)
                .Get();

            allEmployees = new ObservableCollection<BarbershopManagementSystem>(result.Models);
            employees = new ObservableCollection<BarbershopManagementSystem>(allEmployees);

            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
            LoadPage(CurrentPage);
            GeneratePaginationButtons();

            PopulateComboBox();
        }

        private void LoadPage(int pageNumber)
        {
            CurrentPage = pageNumber;

            var pageData = employees
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            while (pageData.Count < PageSize)
            {
                pageData.Add(new BarbershopManagementSystem
                {
                    EmiD = "",
                    BN = "",
                    Service = "",
                    Price = "",
                });
            }

            EmployeeGrid.ItemsSource = pageData;
        }

        private void GeneratePaginationButtons()
        {
            PaginationPanel.Children.Clear();

            for (int i = 1; i <= TotalPages; i++)
            {
                Button btn = new Button
                {
                    Content = i.ToString(),
                    Margin = new Thickness(5, 0, 5, 0),
                    Padding = new Thickness(10, 5, 10, 5),
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontWeight = (i == CurrentPage) ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = 20,
                    Cursor = Cursors.Hand
                };

                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
                border.SetValue(Border.BorderThicknessProperty, new Thickness(0));

                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

                border.AppendChild(contentPresenter);
                template.VisualTree = border;

                btn.Template = template;

                btn.MouseEnter += (s, e) => btn.Foreground = System.Windows.Media.Brushes.Black;
                btn.MouseLeave += (s, e) => btn.Foreground = System.Windows.Media.Brushes.Gray;

                int pageNum = i;
                btn.Click += (s, e) =>
                {
                    LoadPage(pageNum);
                    GeneratePaginationButtons();
                };

                PaginationPanel.Children.Add(btn);
            }
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Appointments Appointments = new Appointments();
            Appointments.Show();
            this.Close();
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

        private void Service_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new AssignNew_Service();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
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

        private async void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                var service = btn.DataContext as BarbershopManagementSystem;

                if (service == null || string.IsNullOrWhiteSpace(service.EmiD))
                {
                    return;
                }

                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new delete();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                delete deleteDialog = (delete)currentModalWindow;

                currentModalWindow.Closed += ModalWindow_Closed;

                bool? result = deleteDialog.ShowDialog();

                if (result == true)
                {
                    if (supabase == null)
                    {
                        MessageBox.Show("Database connection not initialized.");
                        return;
                    }

                    await supabase
                        .From<BarbershopManagementSystem>()
                        .Where(x => x.Id == service.Id)
                        .Delete();

                    await LoadEmployees();

                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new MangeServiceDelete();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error deleting service: {ex.Message}");
            }
        }

        private void ServiceDes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                var service = btn.DataContext as BarbershopManagementSystem;

                if (service == null || string.IsNullOrWhiteSpace(service.EmiD))
                {
                    return;
                }

                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new Service_Description(
                    service.Id,
                    service.EmiD,
                    service.BN,
                    service.Service,
                    service.Price
                );
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error opening service details: {ex.Message}");
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string searchText = cmbItemID.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                employees = new ObservableCollection<BarbershopManagementSystem>(allEmployees);

                CurrentPage = 1;
                TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
                if (TotalPages == 0) TotalPages = 1;

                LoadPage(CurrentPage);
                GeneratePaginationButtons();
                return;
            }

            var searchResults = allEmployees.Where(emp =>
                !string.IsNullOrEmpty(emp.EmiD) &&
                emp.EmiD.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

            if (searchResults.Count == 0)
            {
                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new notfound();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();

                return;
            }

            employees = new ObservableCollection<BarbershopManagementSystem>(searchResults);

            CurrentPage = 1;
            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }

        private void cmbItemID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtEmployeeIDError.Visibility = Visibility.Collapsed;
        }

        private void ClearForm()
        {
            cmbItemID.Text = string.Empty;
            txtEmployeeIDError.Text = string.Empty;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await SyncCompletedAppointments(); // Sync again on refresh
            await LoadEmployees();
        }

        public async void RefreshGrid()
        {
            await SyncCompletedAppointments();
            await LoadEmployees();
        }

        private void PopulateComboBox()
        {
            cmbItemID.Items.Clear();

            var uniqueEmployeeIDs = allEmployees
                .Where(emp => !string.IsNullOrWhiteSpace(emp.EmiD))
                .Select(emp => emp.EmiD)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            foreach (var empId in uniqueEmployeeIDs)
            {
                cmbItemID.Items.Add(empId);
            }
        }

        [Table("AssignNew_Service")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Emp_ID")]
            public string EmiD { get; set; }

            [Column("Barber_Nickname")]
            public string BN { get; set; }

            [Column("Service")]
            public string Service { get; set; }

            [Column("Price")]
            public string Price { get; set; }
        }

        [Table("Add_Employee")]
        public class Employee : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Full_Name")]
            public string Fname { get; set; }

            [Column("Employee_Role")]
            public string Role { get; set; }

            [Column("Employee_ID")]
            public string EmID { get; set; }

            [Column("Employee_Nickname")]
            public string EmployeeNickname { get; set; }
        }

        // ✅ NEW: Appointment Model to read completed appointments
        [Table("appointment_sched")]
        public class AppointmentModel : BaseModel
        {
            [PrimaryKey("id", false)]
            public string Id { get; set; }

            [Column("status")]
            public string AppointmentStatus { get; set; }

            [Column("total")]
            public string Total { get; set; }

            [Column("service_id")]
            public string ServiceId { get; set; }

            [Column("barber_id")]
            public string BarberAssigned { get; set; }
        }
    }
}