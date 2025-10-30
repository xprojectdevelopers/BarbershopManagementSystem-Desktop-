using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Capstone
{
    public partial class ProfileAdmin : Window
    {
        private Supabase.Client supabase;
        private string currentAdminLogin;
        private bool isPasswordVisible = false;
        private string pendingProfilePictureBase64 = null;
        private Window? currentModalWindow;

        public ProfileAdmin()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
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

                // Update admin information including photo if changed
                var updateQuery = supabase
                    .From<AdminAccount>()
                    .Where(a => a.AdminLogin == currentAdminLogin)
                    .Set(x => x.AdminLogin, newUsername)
                    .Set(x => x.AdminName, txtName.Text.Trim())
                    .Set(x => x.AdminPassword, currentPassword);

                // If there's a pending profile picture, include it in the update
                if (!string.IsNullOrEmpty(pendingProfilePictureBase64))
                {
                    updateQuery = updateQuery.Set(x => x.ProfilePicture, pendingProfilePictureBase64);
                }

                await updateQuery.Update();

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

                // Clear the pending photo after successful save
                pendingProfilePictureBase64 = null;

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

        private void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select image
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Profile Photo",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Get the selected file path
                    string selectedImagePath = openFileDialog.FileName;

                    // Convert image to Base64 and store temporarily
                    byte[] imageBytes = System.IO.File.ReadAllBytes(selectedImagePath);
                    pendingProfilePictureBase64 = Convert.ToBase64String(imageBytes);

                    // Create BitmapImage and set it to the profile image (preview only)
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(selectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // Find the Image control inside the Border
                    Border profileBorder = ProfileImageBorder;
                    if (profileBorder.Child is Image profileImage)
                    {
                        profileImage.Source = bitmap;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 610;
            currentModalWindow.Top = this.Top + 290;
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
                currentModalWindow.Close();

            e.Handled = true;
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