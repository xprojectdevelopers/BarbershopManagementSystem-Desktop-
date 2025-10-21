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
using System.Reflection;
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

namespace Capstone.AppointmentOptions
{
    public partial class Service_Description : Window
    {
        private Client supabase;
        private ObservableCollection<ItemData> items;
        private ObservableCollection<BarbershopManagementSystem> services;
        private HttpClient httpClient;
        private string supabaseUrl;
        private string supabaseKey;
        private bool isSaving = false;
        private Window currentModalWindow;
        private BarbershopManagementSystem currentService; // For editing existing service
        private int? editingServiceId; // Track if we're editing

        public Service_Description(BarbershopManagementSystem serviceToEdit = null)
        {
            InitializeComponent();
            httpClient = new HttpClient();
            services = new ObservableCollection<BarbershopManagementSystem>();
            currentService = serviceToEdit;
            editingServiceId = serviceToEdit?.Id;
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
                services.Clear();
                foreach (var service in servicesResult.Models)
                {
                    services.Add(service);
                }

                // If editing existing service, populate the form
                if (currentService != null)
                {
                    LoadServiceForEditing();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServiceForEditing()
        {
            try
            {
                // Find and select the Employee ID
                for (int i = 1; i < cmbItemID.Items.Count; i++)
                {
                    if (cmbItemID.Items[i] is ComboBoxItem item &&
                        item.Content?.ToString() == currentService.EmiD)
                    {
                        cmbItemID.SelectedIndex = i;
                        break;
                    }
                }

                // Set Barber Nickname (readonly)
                txtBarberNickname.Text = currentService.BN;

                // Find and select the Service
                for (int i = 0; i < cmbService.Items.Count; i++)
                {
                    if (cmbService.Items[i] is ComboBoxItem serviceItem &&
                        serviceItem.Content?.ToString() == currentService.Service)
                    {
                        cmbService.SelectedIndex = i;
                        break;
                    }
                }

                // Set Price (remove peso sign if present)
                txtPrice.Text = currentService.Price?.Replace("₱", "").Trim();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading service data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate all required fields
                if (!await ValidateInputs())
                {
                    return;
                }

                if (editingServiceId == null)
                {
                    MessageBox.Show("No service selected for editing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get the service from database
                var existingService = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.Id == editingServiceId.Value)
                    .Get();

                if (existingService.Models.Count == 0)
                {
                    MessageBox.Show("Service not found in database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var serviceToUpdate = existingService.Models.First();

                // Get updated values from form
                string selectedEmployeeId = ((ComboBoxItem)cmbItemID.SelectedItem)?.Content?.ToString();
                string barberNickname = txtBarberNickname.Text.Trim();
                string selectedService = ((ComboBoxItem)cmbService.SelectedItem)?.Content?.ToString();
                string price = txtPrice.Text.Trim();

                // Update the service object
                serviceToUpdate.EmiD = selectedEmployeeId;
                serviceToUpdate.BN = barberNickname;
                serviceToUpdate.Service = selectedService;
                serviceToUpdate.Price = price;

                // Update in database
                await serviceToUpdate.Update<BarbershopManagementSystem>();

                MessageBox.Show("Service updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close the window
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating service: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClearForm()
        {
            cmbItemID.SelectedIndex = 0;
            txtBarberNickname.Clear();
            cmbService.SelectedIndex = -1;
            txtPrice.Clear();

            // Hide all error messages
            cmbItemIDError.Visibility = Visibility.Collapsed;
            txtBarberNicknameError.Visibility = Visibility.Collapsed;
            cmbServiceError.Visibility = Visibility.Collapsed;
            cmbServiceSame.Visibility = Visibility.Collapsed;
            txtPriceError.Visibility = Visibility.Collapsed;
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
            if (cmbService.SelectedIndex < 0)
            {
                cmbServiceError.Text = "Please select a Service";
                cmbServiceError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                // Check for duplicate Employee ID + Service combination (except when editing the same record)
                string selectedEmployeeId = ((ComboBoxItem)cmbItemID.SelectedItem)?.Content?.ToString();
                string selectedService = ((ComboBoxItem)cmbService.SelectedItem)?.Content?.ToString();

                bool isDuplicate = services.Any(s =>
                    s.EmiD == selectedEmployeeId &&
                    s.Service == selectedService &&
                    s.Id != editingServiceId); // Exclude current record when editing

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
                else if (price <= 0)
                {
                    txtPriceError.Text = "Price must be greater than zero";
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
            public string EmiD { get; set; }

            [Column("Barber_Nickname")]
            public string BN { get; set; }

            [Column("Service")]
            public string Service { get; set; }

            [Column("Price")]
            public string Price { get; set; }
        }
    }
}