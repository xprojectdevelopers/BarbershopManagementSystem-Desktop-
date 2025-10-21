using Newtonsoft.Json.Linq;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net.Http;
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
using static Capstone.Payroll;

namespace Capstone.AppointmentOptions
{
    public partial class AssignNew_Service : Window
    {
        private Client supabase;
        private ObservableCollection<ItemData> items;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private HttpClient httpClient;
        private string supabaseUrl;
        private string supabaseKey;
        private bool isSaving = false;
        private Window currentModalWindow;

        public AssignNew_Service()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            employees = new ObservableCollection<BarbershopManagementSystem>();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeSupabaseAsync()
        {
            supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            supabase = new Client(supabaseUrl, supabaseKey, new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

            await supabase.InitializeAsync();
        }

        private async Task InitializeData()
        {
            try
            {
                await InitializeSupabaseAsync();

                items = new ObservableCollection<ItemData>();
                var result = await supabase.From<ItemData>().Get();

                cmbItemID.Items.Clear();
                cmbItemID.Items.Add(new ComboBoxItem
                {
                    Content = "Select Employee ID",
                    IsEnabled = false,
                    Foreground = System.Windows.Media.Brushes.Gray
                });

                cmbItemID.SelectedIndex = 0;

                foreach (var item in result.Models)
                {
                    items.Add(item);
                    cmbItemID.Items.Add(new ComboBoxItem
                    {
                        Content = item.Item_ID,
                        Tag = item
                    });
                }

                // Load existing services
                var servicesResult = await supabase.From<BarbershopManagementSystem>().Get();
                employees.Clear();
                foreach (var service in servicesResult.Models)
                {
                    employees.Add(service);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmbItemID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbItemID.SelectedItem != null && cmbItemID.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag != null && selectedItem.Tag is ItemData item)
                {
                    // Populate the Barber Nickname field with Employee_Nickname
                    txtBarberNickname.Text = item.Item_Name;
                    txtBarberNickname.IsReadOnly = true;
                }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Prevent double-clicking
            if (isSaving)
                return;

            try
            {
                if (!await ValidateInputs())
                {
                    return;
                }

                // Set saving flag and disable button
                isSaving = true;
                Button saveButton = (Button)sender;
                saveButton.IsEnabled = false;

                var newEmployee = new BarbershopManagementSystem
                {
                    EmiD = cmbItemID.Text.Trim(),
                    BN = txtBarberNickname.Text.Trim(),
                    Service = cmbService.Text.Trim(),
                    Price = txtPrice.Text.Trim(),
                };

                // Save to Supabase database
                var result = await supabase.From<BarbershopManagementSystem>().Insert(newEmployee);

                if (result != null && result.Models.Count > 0)
                {
                    // Add to local collection only once
                    employees.Add(result.Models[0]);

                    // Show success window only once
                    // Show the overlay FIRST
                    ModalOverlay.Visibility = Visibility.Visible;

                    // Open PurchaseOrders as a regular window
                    currentModalWindow = new succesfull();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Subscribe to Closed event
                    currentModalWindow.Closed += ModalWindow_Closed;

                    // Show as regular window
                    currentModalWindow.Show();

                    // Clear the form after successful save
                    ClearForm();
                }
                else
                {
                    MessageBox.Show("Failed to save employee to database.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving employee: {ex.Message}", "Database Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Always re-enable button and reset flag
                isSaving = false;
                Button saveButton = (Button)sender;
                saveButton.IsEnabled = true;
                saveButton.Content = "Add Employee";
            }
        }

        private void ClearForm()
        {
            cmbItemID.SelectedIndex = 0;
            txtBarberNickname.Clear();
            cmbService.SelectedIndex = 0;
            txtPrice.Clear();
        }

        private async Task<bool> ValidateInputs()
        {
            bool isValid = true;

            // Hide all error messages first
            cmbItemIDError.Visibility = Visibility.Collapsed;
            txtBarberNicknameError.Visibility = Visibility.Collapsed;
            cmbServiceError.Visibility = Visibility.Collapsed;
            cmbServiceSame.Visibility = Visibility.Collapsed;
            txtPriceError.Visibility = Visibility.Collapsed;

            // Validate Employee ID ComboBox
            if (cmbItemID.SelectedIndex <= 0)
            {
                cmbItemIDError.Text = "Please select an Employee ID";
                cmbItemIDError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Barber Nickname TextBox
            if (string.IsNullOrWhiteSpace(txtBarberNickname.Text))
            {
                txtBarberNicknameError.Text = "Barber Nickname is required";
                txtBarberNicknameError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Service ComboBox
            if (cmbService.SelectedIndex < 0 || cmbService.SelectedIndex >= cmbService.Items.Count - 1)
            {
                cmbServiceError.Text = "Please select a Service";
                cmbServiceError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                // Check for duplicate Employee ID + Service combination
                string selectedEmployeeId = cmbItemID.Text.Trim();
                string selectedService = cmbService.Text.Trim();

                bool isDuplicate = employees.Any(e =>
                    e.EmiD == selectedEmployeeId &&
                    e.Service == selectedService);

                if (isDuplicate)
                {
                    cmbServiceSame.Text = "Service already exists for the selected barber";
                    cmbServiceSame.Visibility = Visibility.Visible;
                    isValid = false;
                }
            }

            // Validate Price TextBox
            if (string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                txtPriceError.Text = "Price is required";
                txtPriceError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                // Validate if price is a valid number (no letters allowed)
                if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price))
                {
                    txtPriceError.Text = "Please enter a valid number";
                    txtPriceError.Visibility = Visibility.Visible;
                    isValid = false;
                }
            }

            return isValid;
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        [Table("Add_Employee")]
        public class ItemData : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Employee_ID")]
            public string Item_ID { get; set; }

            [Column("Employee_Nickname")]
            public string Item_Name { get; set; }
        }

        [Table("AssignNew_Service")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Emp_ID")]
            public String EmiD { get; set; }

            [Column("Barber_Nickname")]
            public string BN { get; set; }

            [Column("Service")]
            public string Service { get; set; }

            [Column("Price")]
            public string Price { get; set; }
        }
    }
}