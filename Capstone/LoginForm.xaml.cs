using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace Capstone
{
    public partial class LoginForm : Window
    {
        private readonly HttpClient httpClient;
        private readonly string supabaseUrl;
        private readonly string supabaseKey;
        private bool isPasswordVisible = false;

        public static string CurrentEmployeeId { get; set; }
        public static string CurrentUserRole { get; set; }

        public LoginForm()
        {
            InitializeComponent();
            httpClient = new HttpClient();

            supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                MessageBox.Show("Error: Supabase configuration not found in App.config", "Configuration Error");
                return;
            }

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (isPasswordVisible)
            {
                // Hide password
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtToggleIcon.Text = "👁";
                isPasswordVisible = false;
            }
            else
            {
                // Show password
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtToggleIcon.Text = "👁‍🗨";
                isPasswordVisible = true;
            }
        }

        private string GetCurrentPassword()
        {
            return isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            lblStatus.Content = "";

            string employeeId = txtEmployeeId.Text.Trim();
            string password = GetCurrentPassword();

            System.Diagnostics.Debug.WriteLine($"Attempting login with Employee ID: {employeeId}");

            if (string.IsNullOrEmpty(employeeId))
            {
                lblStatus.Content = "Please enter Employee ID";
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                lblStatus.Content = "Please enter Password";
                return;
            }

            btnLogin.IsEnabled = false;
            lblStatus.Content = "Logging in...";

            try
            {
                var (isAuthenticated, userType, userRole, userName, userPhoto) = await AuthenticateUser(employeeId, password);

                if (isAuthenticated)
                {
                    lblStatus.Content = $"Login successful as {userType}!";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Green;

                    // Store the logged-in user data
                    CurrentEmployeeId = employeeId;
                    CurrentUserRole = userRole;

                    // Cache user data in Menu static properties
                    Menu.CurrentUserRole = userRole;
                    Menu.CurrentUserName = userName;
                    Menu.CurrentUserPhoto = userPhoto;

                    System.Diagnostics.Debug.WriteLine($"Logged in as: {userType} with role: {userRole}");

                    await Task.Delay(1000);
                    Menu menuWindow = new Menu();
                    menuWindow.Show();
                    this.Close();
                }
                else
                {
                    lblStatus.Content = "Invalid Employee ID or Password";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Red;

                    // Clear both password fields
                    txtPassword.Password = "";
                    txtPasswordVisible.Text = "";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Content = "Connection error. Please try again.";
                lblStatus.Foreground = System.Windows.Media.Brushes.Red;

                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Full exception: {ex}");

                MessageBox.Show($"Error details: {ex.Message}", "Debug Error");
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private async Task<(bool isAuthenticated, string userType, string userRole, string userName, string userPhoto)> AuthenticateUser(string employeeId, string password)
        {
            try
            {
                // First, try to authenticate as Admin
                System.Diagnostics.Debug.WriteLine("Checking Admin_Account table...");
                string adminQuery = $"{supabaseUrl}/rest/v1/Admin_Account?Admin_Login=eq.{employeeId}&Admin_Password=eq.{password}&select=*";

                HttpResponseMessage adminResponse = await httpClient.GetAsync(adminQuery);

                if (adminResponse.IsSuccessStatusCode)
                {
                    string adminContent = await adminResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Admin response: {adminContent}");

                    JArray adminUsers = JArray.Parse(adminContent);

                    if (adminUsers.Count > 0)
                    {
                        string adminRole = adminUsers[0]["Admin_Role"]?.ToString() ?? "Admin";
                        string adminName = adminUsers[0]["Admin_Name"]?.ToString() ?? "Admin";
                        string adminPhoto = adminUsers[0]["Photo"]?.ToString() ?? "";

                        System.Diagnostics.Debug.WriteLine($"Admin login successful! Role: {adminRole}, Name: {adminName}");
                        return (true, "Admin", adminRole, adminName, adminPhoto);
                    }
                }

                // If not found in Admin, try Employee table
                System.Diagnostics.Debug.WriteLine("Checking Add_Employee table...");
                string employeeQuery = $"{supabaseUrl}/rest/v1/Add_Employee?Employee_ID=eq.{employeeId}&Employee_Password=eq.{password}&select=*";

                HttpResponseMessage employeeResponse = await httpClient.GetAsync(employeeQuery);

                if (employeeResponse.IsSuccessStatusCode)
                {
                    string employeeContent = await employeeResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Employee response: {employeeContent}");

                    JArray employeeUsers = JArray.Parse(employeeContent);

                    if (employeeUsers.Count > 0)
                    {
                        string employeeRole = employeeUsers[0]["Employee_Role"]?.ToString() ?? "Cashier";
                        string employeeName = employeeUsers[0]["Employee_Name"]?.ToString() ?? "Employee";
                        string employeePhoto = employeeUsers[0]["Photo"]?.ToString() ?? "";

                        System.Diagnostics.Debug.WriteLine($"Employee login successful! Role: {employeeRole}, Name: {employeeName}");
                        return (true, "Employee", employeeRole, employeeName, employeePhoto);
                    }
                }
                else
                {
                    string errorContent = await employeeResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Employee table error: {employeeResponse.StatusCode} - {errorContent}");
                }

                System.Diagnostics.Debug.WriteLine("No matching credentials found in either table");
                return (false, null, null, null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Full exception: {ex}");
                throw;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            httpClient?.Dispose();
            base.OnClosed(e);
        }
    }
}