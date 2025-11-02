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
        private Supabase.Client? supabase;
        private int serviceId;
        private Window currentModalWindow;

        public Service_Description(int id, string empId, string barberNickname, string service, string price)
        {
            InitializeComponent();

            serviceId = id;

            // Auto-fill the form fields
            FillFormData(empId, barberNickname, service, price);

            Loaded += async (s, e) => await InitializeSupabaseAsync();
        }

        private async Task InitializeSupabaseAsync()
        {
            string? supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string? supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                MessageBox.Show("Supabase configuration missing in App.config!");
                return;
            }

            supabase = new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        private void FillFormData(string empId, string barberNickname, string service, string price)
        {
            // Set Employee ID in ComboBox (READ-ONLY)
            cmbItemID.Items.Clear();
            cmbItemID.Items.Add(new ComboBoxItem { Content = empId });
            cmbItemID.SelectedIndex = 0;
            cmbItemID.IsEnabled = false; // Make it read-only

            // Set Barber Nickname (READ-ONLY)
            txtBarberNickname.Text = barberNickname ?? "";
            txtBarberNickname.IsReadOnly = true; // Make it read-only
            txtBarberNickname.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Gray background to show it's disabled

            // Set Service in ComboBox (EDITABLE)
            cmbService.Items.Clear();
            var serviceItems = new List<string>
            {
                "Haircut",
                "Haircut (Reservation)",
                "Haircut/Wash",
                "Haircut/Hot Towel",
                "Haircut/Hair Dye",
                "Haircut/Hair Color",
                "Haircut/Highlights",
                "Haircut/Bleaching",
                "Haircut/Perm",
                "Rebond/Short Hair",
                "Rebond/Long Hair",
                "Braid"
            };

            foreach (var item in serviceItems)
            {
                cmbService.Items.Add(new ComboBoxItem { Content = item });
            }

            // Select the matching service
            for (int i = 0; i < cmbService.Items.Count; i++)
            {
                var item = cmbService.Items[i] as ComboBoxItem;
                if (item?.Content?.ToString() == service)
                {
                    cmbService.SelectedIndex = i;
                    break;
                }
            }

            // Set Price (EDITABLE - remove ₱ symbol if present)
            txtPrice.Text = price?.Replace("₱", "").Trim() ?? "";
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs (only Service and Price since EmpID and Name are read-only)
                if (!ValidateInputs())
                    return;

                if (supabase == null)
                {
                    MessageBox.Show("Database connection not initialized.");
                    return;
                }

                // Get values (EmpID and Name remain the same, only Service and Price can change)
                var selectedEmpId = (cmbItemID.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var barberNickname = txtBarberNickname.Text.Trim();
                var selectedService = (cmbService.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var price = txtPrice.Text.Trim();


                // Update only Service and Price in database
                await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.Id == serviceId)
                    .Set(x => x.Service, selectedService)
                    .Set(x => x.Price, "₱" + price)
                    .Update();

                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new ServiceDescriptionSuccessful();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();

                // Refresh parent window
                if (Owner is Manage_Services parentWindow)
                {
                    parentWindow.RefreshGrid();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error updating service: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool ValidateInputs()
        {
            bool isValid = true;

            // Reset errors
            if (cmbServiceError != null)
                cmbServiceError.Visibility = Visibility.Collapsed;

            if (txtPriceError != null)
                txtPriceError.Visibility = Visibility.Collapsed;

            // Validate Service (only editable field)
            if (cmbService.SelectedIndex < 0)
            {
                if (cmbServiceError != null)
                {
                    cmbServiceError.Text = "Service is required";
                    cmbServiceError.Visibility = Visibility.Visible;
                }
                isValid = false;
            }

            // Validate Price (only editable field)
            if (string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                if (txtPriceError != null)
                {
                    txtPriceError.Text = "Price is required";
                    txtPriceError.Visibility = Visibility.Visible;
                }
                isValid = false;
            }
            else if (!decimal.TryParse(txtPrice.Text, out _))
            {
                if (txtPriceError != null)
                {
                    txtPriceError.Text = "Price must be a valid number";
                    txtPriceError.Visibility = Visibility.Visible;
                }
                isValid = false;
            }

            return isValid;
        }

        private void cmbItemID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Not needed since cmbItemID is now disabled, but keeping for safety
            if (cmbItemIDError != null)
            {
                cmbItemIDError.Visibility = Visibility.Collapsed;
            }
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
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