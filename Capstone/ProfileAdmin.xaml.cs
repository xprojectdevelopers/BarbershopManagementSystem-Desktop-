using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Capstone
{
    public partial class ProfileAdmin : Window
    {
        private Supabase.Client supabase;
        private string currentAdminLogin;
        private bool isPasswordVisible = false;

        public ProfileAdmin()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadAdminProfile();
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

        private async Task LoadAdminProfile()
        {
            try
            {
                currentAdminLogin = LoginForm.CurrentEmployeeId;

                if (string.IsNullOrEmpty(currentAdminLogin))
                {
                    MessageBox.Show("No admin logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = await supabase
                    .From<AdminAccount>()
                    .Where(a => a.AdminLogin == currentAdminLogin)
                    .Get();

                if (result.Models.Count > 0)
                {
                    var admin = result.Models[0];

                    // Display profile information
                    NameText.Text = admin.AdminName;
                    RoleText.Text = admin.AdminRole;

                    // Fill form fields
                    txtName.Text = admin.AdminName;
                    txtUsername.Text = admin.AdminLogin;
                    txtPassword.Password = admin.AdminPassword;
                    txtPasswordVisible.Text = admin.AdminPassword;

                    // Load profile picture
                    if (!string.IsNullOrEmpty(admin.ProfilePicture))
                    {
                        LoadProfilePicture(admin.ProfilePicture);
                    }
                }
                else
                {
                    MessageBox.Show("Admin profile not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Username is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string currentPassword = GetCurrentPassword();
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    MessageBox.Show("Password is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if username is being changed
                string newUsername = txtUsername.Text.Trim();
                if (newUsername != currentAdminLogin)
                {
                    // Check if new username already exists
                    var existingUser = await supabase
                        .From<AdminAccount>()
                        .Where(a => a.AdminLogin == newUsername)
                        .Get();

                    if (existingUser.Models.Count > 0)
                    {
                        MessageBox.Show("Username already exists. Please choose a different username.",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Update admin information
                await supabase
                    .From<AdminAccount>()
                    .Where(a => a.AdminLogin == currentAdminLogin)
                    .Set(x => x.AdminLogin, newUsername)
                    .Set(x => x.AdminName, txtName.Text.Trim())
                    .Set(x => x.AdminPassword, currentPassword)
                    .Update();

                // Update the display
                NameText.Text = txtName.Text.Trim();

                // Update static variables
                Menu.CurrentUserName = txtName.Text.Trim();
                LoginForm.CurrentEmployeeId = newUsername;

                // Update local variable
                currentAdminLogin = newUsername;

                // Update both password fields
                txtPassword.Password = currentPassword;
                txtPasswordVisible.Text = currentPassword;

                MessageBox.Show("Profile updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
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
            public string ProfilePicture { get; set; }
        }
    }
}