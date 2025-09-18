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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient;
        private readonly string supabaseUrl;
        private readonly string supabaseKey;

        public MainWindow()
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
            // Clear previous status
            lblStatus.Content = "";

            // Get input values
            string employeeId = txtEmployeeId.Text.Trim();
            string password = txtPassword.Password;

            // Debug: Show what we're trying to authenticate
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

            // Disable login button during authentication
            btnLogin.IsEnabled = false;
            lblStatus.Content = "Logging in...";

            try
            {
                bool isAuthenticated = await AuthenticateUser(employeeId, password);

                if (isAuthenticated)
                {
                    lblStatus.Content = "Login successful!";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Green;

                    // Wait a moment then open Window1
                    await Task.Delay(1000);
                    Menu Menu = new Menu();
                    Menu.Show();
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

                // Show error to user for debugging
                MessageBox.Show($"Error details: {ex.Message}", "Debug Error");
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private async Task<bool> AuthenticateUser(string employeeId, string password)
        {
            try
            {
                // Test query to confirm connection
                string testQuery = $"{supabaseUrl}/rest/v1/Employees_Login?select=emID";

                System.Diagnostics.Debug.WriteLine($"Testing connection to: {testQuery}");

                HttpResponseMessage testResponse = await httpClient.GetAsync(testQuery);
                System.Diagnostics.Debug.WriteLine($"Test response status: {testResponse.StatusCode}");

                if (!testResponse.IsSuccessStatusCode)
                {
                    string errorContent = await testResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Test error content: {errorContent}");

                    MessageBox.Show($"Database connection failed:\nStatus: {testResponse.StatusCode}\nError: {errorContent}", "Connection Error");
                    return false;
                }

                string testContent = await testResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Available employees: {testContent}");

                // Actual authentication query
                string query = $"{supabaseUrl}/rest/v1/Employees_Login?emID=eq.{employeeId}&empassword=eq.{password}&select=*";
                System.Diagnostics.Debug.WriteLine($"Auth query: {query}");

                HttpResponseMessage response = await httpClient.GetAsync(query);
                System.Diagnostics.Debug.WriteLine($"Auth response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Auth response content: {responseContent}");

                    JArray users = JArray.Parse(responseContent);
                    System.Diagnostics.Debug.WriteLine($"Number of matching users: {users.Count}");

                    // If we get any results, authentication is successful
                    return users.Count > 0;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Auth HTTP Error: {response.StatusCode} - {response.ReasonPhrase}");
                    System.Diagnostics.Debug.WriteLine($"Error content: {errorContent}");

                    MessageBox.Show($"Authentication failed:\nStatus: {response.StatusCode}\nError: {errorContent}", "Auth Error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Full exception: {ex}");
                throw; // Re-throw to be handled by calling method
            }
        }

        // Clean up HttpClient when window is closed
        protected override void OnClosed(EventArgs e)
        {
            httpClient?.Dispose();
            base.OnClosed(e);
        }
    }
}
