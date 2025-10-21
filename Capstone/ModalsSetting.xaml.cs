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
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Open LoginForm
                LoginForm loginForm = new LoginForm();
                loginForm.Show();

                // Close all windows including the parent Menu window
                foreach (Window window in Application.Current.Windows)
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