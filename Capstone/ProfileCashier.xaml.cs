using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Capstone
{
    public partial class ProfileCashier : Window
    {
        private Supabase.Client supabase;
        private string currentEmployeeId;

        public ProfileCashier()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadCashierProfile();
        }

        private async Task InitializeSupabaseAsync()
        {
            string supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            supabase = new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        private async Task LoadCashierProfile()
        {
            try
            {
                currentEmployeeId = LoginForm.CurrentEmployeeId;

                if (string.IsNullOrEmpty(currentEmployeeId))
                {
                    MessageBox.Show("No employee logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Changed from CashierEmployee to Employee
                var result = await supabase
                    .From<Employee>()
                    .Where(e => e.EmployeeID == currentEmployeeId)
                    .Get();

                if (result.Models.Count > 0)
                {
                    var employee = result.Models[0];

                    // Display profile information
                    NameText.Text = employee.EmployeeName;
                    RoleText.Text = employee.EmployeeRole;

                    // Fill form fields (read-only)
                    txtName.Text = employee.EmployeeName;
                    txtEmployeeID.Text = employee.EmployeeID;
                    txtRole.Text = employee.EmployeeRole;

                    // Load profile picture
                    if (!string.IsNullOrEmpty(employee.ProfilePicture))
                    {
                        LoadProfilePicture(employee.ProfilePicture);
                    }
                }
                else
                {
                    MessageBox.Show("Employee profile not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
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
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
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
                        bitmap.EndInit();
                    }

                    var profileBorder = ProfileImageBorder;
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }

        [Table("Add_Employee")]
        public class Employee : BaseModel
        {
            [PrimaryKey("Employee_ID", false)]
            public string EmployeeID { get; set; } = string.Empty;

            [Column("Full_Name")]
            public string EmployeeName { get; set; } = string.Empty;

            [Column("Employee_Role")]
            public string EmployeeRole { get; set; } = string.Empty;

            [Column("Photo")]
            public string ProfilePicture { get; set; }

            [Column("Employee_Password")]
            public string EmployeePassword { get; set; } = string.Empty;
        }
    }
}