using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Capstone
{
    /// <summary>
    /// Interaction logic for ModalsSetting.xaml
    /// </summary>
    public partial class ModalsSetting : Window
    {
        public ModalsSetting()
        {
            InitializeComponent();
            ConfigureMenuBasedOnRole();
        }

        private void ConfigureMenuBasedOnRole()
        {
            try
            {
                string userRole = LoginForm.CurrentUserRole;

                // If Cashier, disable Website Admin and Mobile Admin
                if (userRole != null && userRole.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
                {
                    WebsiteAdminBorder.IsEnabled = false;
                    WebsiteAdminBorder.Opacity = 0.5;
                    WebsiteAdminBorder.Cursor = Cursors.No;

                    MobileAdminBorder.IsEnabled = false;
                    MobileAdminBorder.Opacity = 0.5;
                    MobileAdminBorder.Cursor = Cursors.No;
                }
                // If Admin, enable Website Admin and Mobile Admin
                else if (userRole != null && userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    WebsiteAdminBorder.IsEnabled = true;
                    WebsiteAdminBorder.Opacity = 1.0;
                    WebsiteAdminBorder.Cursor = Cursors.Hand;

                    MobileAdminBorder.IsEnabled = true;
                    MobileAdminBorder.Opacity = 1.0;
                    MobileAdminBorder.Cursor = Cursors.Hand;
                }
                else
                {
                    // Default: disable admin options if role is unknown
                    WebsiteAdminBorder.IsEnabled = false;
                    WebsiteAdminBorder.Opacity = 0.5;
                    WebsiteAdminBorder.Cursor = Cursors.No;

                    MobileAdminBorder.IsEnabled = false;
                    MobileAdminBorder.Opacity = 0.5;
                    MobileAdminBorder.Cursor = Cursors.No;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring menu: {ex.Message}");
            }
        }

        private void MyProfile_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Get the current user's role from LoginForm
                string userRole = LoginForm.CurrentUserRole;

                if (string.IsNullOrEmpty(userRole))
                {
                    MessageBox.Show("User role not found. Please log in again.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Window profileWindow = null;

                // Open appropriate profile window based on role
                if (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    profileWindow = new ProfileAdmin();
                }
                else if (userRole.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
                {
                    profileWindow = new ProfileCashier();
                }
                else
                {
                    MessageBox.Show($"Unknown user role: {userRole}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Show the profile window first
                if (profileWindow != null)
                {
                    profileWindow.Show();

                    // Close all other windows except the new profile window
                    foreach (Window window in Application.Current.Windows.Cast<Window>().ToList())
                    {
                        if (window != profileWindow)
                        {
                            window.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open profile: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MobileAdmin_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string url = "https://admin-panel-molave.vercel.app/mobile";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebsiteAdmin_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string url = "https://admin-panel-molave.vercel.app/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogOut_Click(object sender, MouseButtonEventArgs e)
        {
            // Show confirmation dialog
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear static user data
                LoginForm.CurrentEmployeeId = null;
                LoginForm.CurrentUserRole = null;
                Menu.CurrentUserRole = null;
                Menu.CurrentUserName = null;
                Menu.CurrentUserPhoto = null;

                // Open LoginForm
                LoginForm loginForm = new LoginForm();
                loginForm.Show();

                // Close all windows including the parent Menu window
                foreach (Window window in Application.Current.Windows.Cast<Window>().ToList())
                {
                    if (window != loginForm)
                    {
                        window.Close();
                    }
                }
            }
        }
    }
}