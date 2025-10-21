using Newtonsoft.Json.Linq;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Capstone
{
    public partial class SaleItem : Window
    {
        private Client supabase;
        private ObservableCollection<ItemData> items;
        private HttpClient httpClient;
        private string supabaseUrl;
        private string supabaseKey;
        private Window currentModalWindow;
        public string LoggedInEmployeeId { get; set; }

        public SaleItem(string employeeId)
        {
            InitializeComponent();
            LoggedInEmployeeId = employeeId;
            httpClient = new HttpClient();
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

                string userName = await GetUserName(LoggedInEmployeeId);
                txtProcess.Text = userName;
                txtProcess.IsReadOnly = true;

                items = new ObservableCollection<ItemData>();
                var result = await supabase.From<ItemData>().Get();

                cmbItemID.Items.Clear();
                cmbItemID.Items.Add(new ComboBoxItem
                {
                    Content = "Select Item ID",
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> GetUserName(string employeeId)
        {
            try
            {
                string adminQuery = $"{supabaseUrl}/rest/v1/Admin_Account?Admin_Login=eq.{employeeId}&select=Admin_Name";
                HttpResponseMessage adminResponse = await httpClient.GetAsync(adminQuery);

                if (adminResponse.IsSuccessStatusCode)
                {
                    string adminContent = await adminResponse.Content.ReadAsStringAsync();
                    JArray adminUsers = JArray.Parse(adminContent);

                    if (adminUsers.Count > 0)
                    {
                        string adminName = adminUsers[0]["Admin_Name"]?.ToString();
                        if (!string.IsNullOrEmpty(adminName))
                        {
                            return adminName;
                        }
                    }
                }

                string employeeQuery = $"{supabaseUrl}/rest/v1/Add_Employee?Employee_ID=eq.{employeeId}&select=Full_Name";
                HttpResponseMessage employeeResponse = await httpClient.GetAsync(employeeQuery);

                if (employeeResponse.IsSuccessStatusCode)
                {
                    string employeeContent = await employeeResponse.Content.ReadAsStringAsync();
                    JArray employeeUsers = JArray.Parse(employeeContent);

                    if (employeeUsers.Count > 0)
                    {
                        string fullName = employeeUsers[0]["Full_Name"]?.ToString();
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            return fullName;
                        }
                    }
                }

                return employeeId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user name: {ex.Message}");
                return employeeId;
            }
        }

        private async void cmbItemID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbItemID.SelectedItem != null && cmbItemID.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag != null && selectedItem.Tag is ItemData item)
                {
                    txtItemName.Text = item.Item_Name;
                    txtItemName.IsReadOnly = true;
                }
            }
        }

        // ✅ Get current stock from database
        private async Task<int> GetCurrentStock(string itemId)
        {
            try
            {
                string queryUrl = $"{supabaseUrl}/rest/v1/Add_Item?Item_ID=eq.{itemId}&select=Quantity_Stock";
                HttpResponseMessage response = await httpClient.GetAsync(queryUrl);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JArray items = JArray.Parse(content);

                    if (items.Count > 0)
                    {
                        var quantityToken = items[0]["Quantity_Stock"];
                        if (quantityToken != null && quantityToken.Type != JTokenType.Null)
                        {
                            return quantityToken.ToObject<int>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current stock: {ex.Message}");
            }

            return 0;
        }

        // ✅ Async validation with stock checking
        private async Task<bool> ValidateInputs()
        {
            bool isValid = true;

            txtProcessError.Visibility = Visibility.Collapsed;
            cmbItemIDError.Visibility = Visibility.Collapsed;
            txtItemNameError.Visibility = Visibility.Collapsed;
            txtQuantityError.Visibility = Visibility.Collapsed;
            txtDateError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(txtProcess.Text))
            {
                txtProcessError.Text = "Processed By is required";
                txtProcessError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (cmbItemID.SelectedIndex <= 0)
            {
                cmbItemIDError.Text = "Please select an Item ID";
                cmbItemIDError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                txtItemNameError.Text = "Item Name is required";
                txtItemNameError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtQuantity.Text))
            {
                txtQuantityError.Text = "Quantity is required";
                txtQuantityError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
            {
                txtQuantityError.Text = "Please enter a valid positive number";
                txtQuantityError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                // ✅ CHECK STOCK AVAILABILITY
                var selectedComboItem = cmbItemID.SelectedItem as ComboBoxItem;
                var selectedItem = selectedComboItem?.Tag as ItemData;

                if (selectedItem != null)
                {
                    int currentStock = await GetCurrentStock(selectedItem.Item_ID);
                    int requestedQty = int.Parse(txtQuantity.Text);

                    // ✅ Check if stock is 0
                    if (currentStock == 0)
                    {
                        txtQuantityError.Text = "Cannot proceed. Item is out of stock.";
                        txtQuantityError.Visibility = Visibility.Visible;
                        isValid = false;
                    }
                    // ✅ Check if requested quantity exceeds available stock
                    else if (requestedQty > currentStock)
                    {
                        txtQuantityError.Text = $"Insufficient stock. Only {currentStock} available.";
                        txtQuantityError.Visibility = Visibility.Visible;
                        isValid = false;
                    }
                }
            }

            if (!ItemDate.SelectedDate.HasValue)
            {
                txtDateError.Text = "Date is required";
                txtDateError.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private async void btnStockIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ Use async validation with stock checking
                if (!await ValidateInputs())
                {
                    return;
                }

                var selectedComboItem = cmbItemID.SelectedItem as ComboBoxItem;
                var selectedItem = selectedComboItem?.Tag as ItemData;

                if (selectedItem == null)
                {
                    MessageBox.Show("Please select a valid item", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int quantityToSubtract = int.Parse(txtQuantity.Text);

                // Get current quantity
                int currentQuantity = await GetCurrentStock(selectedItem.Item_ID);

                // Calculate new quantity (SUBTRACT for Stock Out)
                int newQuantity = currentQuantity - quantityToSubtract;

                // Insert into Item_Order table
                var itemTransaction = new BarbershopManagementSystem
                {
                    Date = ItemDate.SelectedDate.Value,
                    Transaction = "Stock Out",
                    Quantity = quantityToSubtract.ToString(),
                    ItemName = txtItemName.Text,
                    ProcessedBy = txtProcess.Text
                };

                await supabase.From<BarbershopManagementSystem>().Insert(itemTransaction);

                // Update Quantity_Stock in Add_Item table
                string updateUrl = $"{supabaseUrl}/rest/v1/Add_Item?Item_ID=eq.{selectedItem.Item_ID}";
                var updateData = new JObject
                {
                    ["Quantity_Stock"] = newQuantity
                };

                var jsonContent = new StringContent(updateData.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage updateResponse = await httpClient.PatchAsync(updateUrl, jsonContent);

                if (updateResponse.IsSuccessStatusCode)
                {
                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new ItemSaleSuccessful();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();

                    ClearForm();
                }
                else
                {
                    string errorContent = await updateResponse.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error updating quantity: {errorContent}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during Stock Out: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            cmbItemID.SelectedIndex = 0;
            txtItemName.Clear();
            txtQuantity.Clear();
            ItemDate.SelectedDate = null;

            txtProcessError.Visibility = Visibility.Collapsed;
            cmbItemIDError.Visibility = Visibility.Collapsed;
            txtItemNameError.Visibility = Visibility.Collapsed;
            txtQuantityError.Visibility = Visibility.Collapsed;
            txtDateError.Visibility = Visibility.Collapsed;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        [Table("Add_Item")]
        public class ItemData : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Item_ID")]
            public string Item_ID { get; set; }

            [Column("Item_Name")]
            public string Item_Name { get; set; }

            [Column("Quantity_Stock")]
            public int? Quantity_Stock { get; set; }
        }

        [Table("Item_Order")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Date")]
            public DateTime? Date { get; set; }

            [Column("Transaction")]
            public string Transaction { get; set; }

            [Column("Quantity")]
            public string Quantity { get; set; }

            [Column("ItemName")]
            public string ItemName { get; set; }

            [Column("ProcessedBy")]
            public string ProcessedBy { get; set; }
        }

        protected override void OnClosed(EventArgs e)
        {
            httpClient?.Dispose();
            base.OnClosed(e);
        }
    }
}