using Microsoft.Win32;
using Supabase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
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
using static Capstone.EMenu;

namespace Capstone
{
    /// <summary>
    /// Interaction logic for Testing.xaml
    /// </summary>
    public partial class Testing : Window
    {
        private Client supabase;
        private ObservableCollection<Employee> employees;

        public Testing()
        {
            InitializeComponent();
            Loaded += async (s, e) => InitializeData(); // Initialize when window is loaded
        }

        private async Task InitializeSupabaseAsync()
        {
            string supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            supabase = new Client(supabaseUrl, supabaseKey, new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        private async void InitializeData()
        {
            await InitializeSupabaseAsync();

            employees = new ObservableCollection<Employee>();

            // Fetch data from Supabase
            var result = await supabase.From<Employee>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }

            
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            EMenu EMenu = new EMenu();
            EMenu.Show();
            this.Close();
        }

        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            // Open File Dialog para pumili ng image
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Load image sa PhotoPreview
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.EndInit();

                    PhotoPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load image: " + ex.Message);
                }
            }
        }

        private void btnGenerateID_Click(object sender, RoutedEventArgs e)
        {
            // Kunin ang current year
            string currentYear = DateTime.Now.Year.ToString();

            // Hanapin lahat ng existing employee IDs para sa kasalukuyang taon
            var yearEmployees = employees
                .Where(emp => emp.EmployeeID.StartsWith($"MBS-{currentYear}"))
                .ToList();

            // Kumuha ng last number
            int nextNumber = 1;
            if (yearEmployees.Any())
            {
                // I-extract ang numeric part (e.g., from MBS-2025-003 → 003)
                var lastId = yearEmployees
                    .Select(emp => emp.EmployeeID)
                    .OrderByDescending(id => id)
                    .FirstOrDefault();

                if (lastId != null)
                {
                    string[] parts = lastId.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }
            }

            // Format with leading zeros (3 digits)
            string employeeId = $"MBS-{currentYear}-{nextNumber:D3}";

            // Set to textboxes
            txtEmployeeID.Text = employeeId;
        }

        private void btnGeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            // Generate a random password
            string password = GenerateRandomPassword(8); // 8 character password
            txtEmployeePassword.Text = password;
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            Random random = new Random();

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
