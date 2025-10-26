using Capstone.AppointmentOptions;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    public partial class Menu : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> employees = new ObservableCollection<BarbershopManagementSystem>();
        private Window currentModalWindow;
        public static string CurrentUserRole { get; set; }
        public static string CurrentUserName { get; set; }
        public static string CurrentUserPhoto { get; set; }

        public Menu()
        {
            InitializeComponent();

            ApplyRoleBasedAccess();
            DisplayCurrentUserProfile();

            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        private void DisplayCurrentUserProfile()
        {
            if (!string.IsNullOrEmpty(CurrentUserName))
            {
                NameText.Text = CurrentUserName;
            }

            if (!string.IsNullOrEmpty(CurrentUserRole))
            {
                RoleText.Text = CurrentUserRole;
            }

            if (!string.IsNullOrEmpty(CurrentUserPhoto))
            {
                LoadProfilePicture(CurrentUserPhoto);
            }
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await RefreshUserProfile();
            await LoadEmployeeCount();
        }

        private void MyProfile_Click(object sender, MouseButtonEventArgs e)
        {
            // Check user role and open appropriate profile window
            if (CurrentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                ProfileAdmin profileAdmin = new ProfileAdmin();
                profileAdmin.Show();
                this.Close();
            }
            else if (CurrentUserRole.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
            {
                ProfileCashier profileCashier = new ProfileCashier();
                profileCashier.Show();
                this.Close();
            }
        }

        private void ApplyRoleBasedAccess()
        {
            if (string.IsNullOrEmpty(CurrentUserRole))
                return;

            bool isAdmin = CurrentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            bool isCashier = CurrentUserRole.Equals("Cashier", StringComparison.OrdinalIgnoreCase);

            if (isCashier)
            {
                AppointmentsCard.IsEnabled = false;
                AppointmentsCard.Opacity = 0.4;
                AppointmentsCard.Cursor = Cursors.No;

                CustomersCard.IsEnabled = false;
                CustomersCard.Opacity = 0.4;
                CustomersCard.Cursor = Cursors.No;

                EmployeesCard.IsEnabled = true;
                EmployeesCard.Opacity = 1.0;
                EmployeesCard.Cursor = Cursors.Hand;

                PayrollCard.IsEnabled = true;
                PayrollCard.Opacity = 1.0;
                PayrollCard.Cursor = Cursors.Hand;

                InventoryCard.IsEnabled = true;
                InventoryCard.Opacity = 1.0;
                InventoryCard.Cursor = Cursors.Hand;
            }
            else if (isAdmin)
            {
                AppointmentsCard.IsEnabled = true;
                AppointmentsCard.Opacity = 1.0;
                AppointmentsCard.Cursor = Cursors.Hand;

                CustomersCard.IsEnabled = true;
                CustomersCard.Opacity = 1.0;
                CustomersCard.Cursor = Cursors.Hand;

                EmployeesCard.IsEnabled = true;
                EmployeesCard.Opacity = 1.0;
                EmployeesCard.Cursor = Cursors.Hand;

                PayrollCard.IsEnabled = true;
                PayrollCard.Opacity = 1.0;
                PayrollCard.Cursor = Cursors.Hand;

                InventoryCard.IsEnabled = true;
                InventoryCard.Opacity = 1.0;
                InventoryCard.Cursor = Cursors.Hand;
            }
        }

        private async Task RefreshUserProfile()
        {
            if (supabase == null) return;

            try
            {
                string employeeId = LoginForm.CurrentEmployeeId;

                if (string.IsNullOrEmpty(employeeId))
                    return;

                var employeeResult = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(e => e.EmployeeID == employeeId)
                    .Get();

                if (employeeResult.Models.Count > 0)
                {
                    var employee = employeeResult.Models[0];

                    if (NameText.Text != employee.EmployeeName)
                        NameText.Text = employee.EmployeeName;

                    if (RoleText.Text != employee.EmployeeRole)
                        RoleText.Text = employee.EmployeeRole;

                    if (!string.IsNullOrEmpty(employee.ProfilePicture) &&
                        CurrentUserPhoto != employee.ProfilePicture)
                    {
                        LoadProfilePicture(employee.ProfilePicture);
                    }
                }
                else
                {
                    var adminResult = await supabase
                        .From<AdminAccount>()
                        .Where(a => a.AdminLogin == employeeId)
                        .Get();

                    if (adminResult.Models.Count > 0)
                    {
                        var admin = adminResult.Models[0];

                        if (NameText.Text != admin.AdminName)
                            NameText.Text = admin.AdminName;

                        if (RoleText.Text != admin.AdminRole)
                            RoleText.Text = admin.AdminRole;

                        if (!string.IsNullOrEmpty(admin.ProfilePicture) &&
                            CurrentUserPhoto != admin.ProfilePicture)
                        {
                            LoadProfilePicture(admin.ProfilePicture);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Profile refresh error: {ex}");
            }
        }

        private void LoadProfilePicture(string imageData)
        {
            try
            {
                if (!string.IsNullOrEmpty(imageData))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();

                    if (imageData.StartsWith("http://") || imageData.StartsWith("https://"))
                    {
                        bitmap.UriSource = new Uri(imageData, UriKind.Absolute);
                    }
                    else
                    {
                        byte[] imageBytes = Convert.FromBase64String(imageData);
                        using (var ms = new System.IO.MemoryStream(imageBytes))
                        {
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }
                    }

                    if (bitmap.UriSource != null)
                    {
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                    }

                    var profileBorder = FindName("ProfileImageBorder") as Border;
                    if (profileBorder != null)
                    {
                        var image = profileBorder.Child as System.Windows.Controls.Image;
                        if (image != null)
                        {
                            image.Source = bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profile picture: {ex.Message}");
            }
        }

        private async Task LoadEmployeeCount()
        {
            if (supabase == null) return;

            var result = await supabase
                .From<BarbershopManagementSystem>()
                .Get();

            int total = result.Models.Count;
            int cashierCount = result.Models.Count(e => e.EmployeeRole?.Equals("Cashier", StringComparison.OrdinalIgnoreCase) == true);
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

        public class ComparisonConverter : IValueConverter
        {
            private static ComparisonConverter _instance;
            public static ComparisonConverter Instance => _instance ??= new ComparisonConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double actualValue && parameter is string parameterString)
                {
                    if (double.TryParse(parameterString, out double threshold))
                    {
                        return actualValue < threshold;
                    }
                }
                return false;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsNotification();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 190;
            currentModalWindow.Top = this.Top + 135;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 190;
            currentModalWindow.Top = this.Top + 135;

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

        private void Customers_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!CustomersCard.IsEnabled) return;

            Customers Customers = new Customers();
            Customers.Show();
            this.Hide();
        }

        private void Employees_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!EmployeesCard.IsEnabled) return;

            EMenu EMenu = new EMenu();
            EMenu.Show();
            this.Hide();
        }

        private void Payroll_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!PayrollCard.IsEnabled) return;

            Payroll Payroll = new Payroll();
            Payroll.Show();
            this.Hide();
        }

        private void Inventory_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!InventoryCard.IsEnabled) return;

            Inventory Inventory = new Inventory();
            Inventory.Show();
            this.Hide();
        }

        private void Appointments_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!AppointmentsCard.IsEnabled) return;

            Appointments Appointments = new Appointments();
            Appointments.Show();

            this.Close();
        }

        [Table("Add_Employee")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("Employee_ID", false)]
            public string EmployeeID { get; set; } = string.Empty;

            [Column("Full_Name")]
            public string EmployeeName { get; set; } = string.Empty;

            [Column("Employee_Role")]
            public string EmployeeRole { get; set; } = string.Empty;

            [Column("Photo")]
            public string? ProfilePicture { get; set; }
        }

        [Table("Admin_Account")]
        public class AdminAccount : BaseModel
        {
            [PrimaryKey("Admin_Login", false)]
            public string AdminLogin { get; set; } = string.Empty;

            [Column("Admin_Name")]
            public string AdminName { get; set; } = string.Empty;

            [Column("Admin_Role")]
            public string AdminRole { get; set; } = string.Empty;

            [Column("Admin_Password")]
            public string AdminPassword { get; set; } = string.Empty;

            [Column("Photo")]
            public string? ProfilePicture { get; set; }
        }
    }
}