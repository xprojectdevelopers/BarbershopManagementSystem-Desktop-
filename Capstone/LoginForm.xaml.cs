using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace Capstone
{
    public partial class LoginForm : Window
    {
        private readonly HttpClient httpClient;
        private readonly string supabaseUrl;
        private readonly string supabaseKey;
        public static string CurrentEmployeeId { get; set; } // Static property to store current user
        public static string CurrentUserRole { get; set; } // Static property to store current user role

        public LoginForm()
        {
            InitializeComponent();
            httpClient = new HttpClient();

            // Get Supabase configuration from App.config
            supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            // Debug: Check if config values are loaded
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                MessageBox.Show("Error: Supabase configuration not found in App.config", "Configuration Error");
                return;
            }

            // Set default headers for Supabase
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            lblStatus.Content = "";

            string employeeId = txtEmployeeId.Text.Trim();
            string password = txtPassword.Password;

            System.Diagnostics.Debug.WriteLine($"Attempting login with Employee ID: {employeeId}");

            // Validate input
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
                // Try to authenticate as Admin first, then as Employee
                var (isAuthenticated, userType, userRole) = await AuthenticateUser(employeeId, password);

                if (isAuthenticated)
                {
                    lblStatus.Content = $"Login successful as {userType}!";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Green;

                    // Store the logged-in employee ID and role
                    CurrentEmployeeId = employeeId;
                    CurrentUserRole = userRole;
                    Menu.CurrentUserRole = userRole; // Set role BEFORE opening window

                    System.Diagnostics.Debug.WriteLine($"Logged in as: {userType} with role: {userRole}");

                    // Wait a moment then open Menu window
                    await Task.Delay(1000);
                    Menu menuWindow = new Menu();
                    menuWindow.Show();
                    this.Close();
                }
                else
                {
                    lblStatus.Content = "Invalid Employee ID or Password";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Red;
                    txtPassword.Password = ""; // Clear password field
                }
            }
            catch (Exception ex)
            {
                lblStatus.Content = "Connection error. Please try again.";
                lblStatus.Foreground = System.Windows.Media.Brushes.Red;

                // Show detailed error in debug
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Full exception: {ex}");

                MessageBox.Show($"Error details: {ex.Message}", "Debug Error");
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private async Task<(bool isAuthenticated, string userType, string userRole)> AuthenticateUser(string employeeId, string password)
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
                        System.Diagnostics.Debug.WriteLine($"Admin login successful! Role: {adminRole}");
                        return (true, "Admin", adminRole);
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
                        string employeeRole = employeeUsers[0]["Employee_Role"]?.ToString() ?? "Employee";
                        System.Diagnostics.Debug.WriteLine($"Employee login successful! Role: {employeeRole}");
                        return (true, "Employee", employeeRole);
                    }
                }
                else
                {
                    string errorContent = await employeeResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Employee table error: {employeeResponse.StatusCode} - {errorContent}");
                }

                // No match found in either table
                System.Diagnostics.Debug.WriteLine("No matching credentials found in either table");
                return (false, null, null);
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