using Microsoft.IdentityModel.Tokens;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Supabase.Postgrest.Constants;


namespace Capstone
{
    /// <summary>
    /// Interaction logic for ProductDescription.xaml
    /// </summary>
    public partial class ProductDescription : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private ObservableCollection<BarbershopManagementSystem> items = new ObservableCollection<BarbershopManagementSystem>();
        private ObservableCollection<BarbershopManagementSystem> filteredItems = new ObservableCollection<BarbershopManagementSystem>();
        private Window currentModalWindow;
        private bool isSaving = false;

        public ProductDescription()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
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

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();

            employees = new ObservableCollection<BarbershopManagementSystem>();

            // Fetch data from Supabase
            var result = await supabase.From<BarbershopManagementSystem>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }
            await LoadItems();
        }

        private async Task LoadItems()
        {
            if (supabase == null) return;

            var result = await supabase
                .From<BarbershopManagementSystem>()
                .Order(x => x.ItemID, Ordering.Ascending)
                .Get();

            items = new ObservableCollection<BarbershopManagementSystem>(result.Models);
            filteredItems = new ObservableCollection<BarbershopManagementSystem>(items);

            // Populate ComboBox with Product IDs
            PopulateProductIDComboBox();
        }

        private void PopulateProductIDComboBox()
        {
            cmbProductItem.Items.Clear();

            // Add placeholder item
            ComboBoxItem placeholder = new ComboBoxItem
            {
                Content = "Select Product ID",
                IsEnabled = false,
                Foreground = Brushes.Gray
            };
            cmbProductItem.Items.Add(placeholder);

            // Add all Product IDs from items
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.ItemID))
                {
                    ComboBoxItem comboItem = new ComboBoxItem
                    {
                        Content = item.ItemID,
                        Tag = item // Store the entire item object for easy access
                    };
                    cmbProductItem.Items.Add(comboItem);
                }
            }

            // Set placeholder as selected
            cmbProductItem.SelectedIndex = 0;
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private void ModalOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (currentModalWindow != null)
            {
                currentModalWindow.Close();
            }
            e.Handled = true;
        }

        // Search and Update Methods
        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            // Check if a valid item is selected in ComboBox
            if (cmbProductItem.SelectedItem == null || cmbProductItem.SelectedIndex <= 0)
            {
                ShowError(txtItemIDError, "Please select a Product ID");
                return;
            }

            txtItemIDError.Visibility = Visibility.Collapsed;

            try
            {
                // Get the selected item from ComboBox
                ComboBoxItem selectedComboItem = cmbProductItem.SelectedItem as ComboBoxItem;
                if (selectedComboItem != null && selectedComboItem.Tag is BarbershopManagementSystem item)
                {
                    // Auto-fill the form with selected item data
                    PopulateForm(item);
                    HideAllErrorMessages();
                }
                else
                {
                    // Fallback: search by ItemID text
                    string itemId = selectedComboItem?.Content?.ToString() ?? "";

                    var result = await supabase
                        .From<BarbershopManagementSystem>()
                        .Where(x => x.ItemID == itemId)
                        .Get();

                    if (result.Models.Count > 0)
                    {
                        var foundItem = result.Models.First();
                        PopulateForm(foundItem);
                        HideAllErrorMessages();
                    }
                    else
                    {
                        HideAllErrorMessages();
                        ModalOverlay.Visibility = Visibility.Visible;

                        currentModalWindow = new notfoundItem();
                        currentModalWindow.Owner = this;
                        currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        currentModalWindow.Closed += ModalWindow_Closed;
                        currentModalWindow.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching for item: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateForm(BarbershopManagementSystem item)
        {
            // Set the ItemID in the form (if you have a separate display field)
            // txtItemID.Text = item.ItemID ?? "";

            // Convert string date to DateTime
            if (!string.IsNullOrEmpty(item.Date) && DateTime.TryParse(item.Date, out DateTime parsedDate))
            {
                ItemDate.SelectedDate = parsedDate;
            }
            else
            {
                ItemDate.SelectedDate = null;
            }

            SetComboBoxSelection(Category, item.Category);
            txtItemName.Text = item.ItemName ?? "";

            // Display price WITHOUT peso symbol (it will show visually due to XAML overlay)
            txtPrice.Text = item.Price ?? "";

            txtSupplierName.Text = item.SupplierName ?? "";
            txtSCNumber.Text = item.SCNumber ?? "";
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            // Get Item ID from selected ComboBox item
            if (cmbProductItem.SelectedItem == null || cmbProductItem.SelectedIndex <= 0)
            {
                ShowError(txtItemIDError, "Please select a Product ID");
                return;
            }

            ComboBoxItem selectedComboItem = cmbProductItem.SelectedItem as ComboBoxItem;
            string itemId = selectedComboItem?.Content?.ToString() ?? "";

            if (string.IsNullOrEmpty(itemId))
            {
                ShowError(txtItemIDError, "Item ID is required");
                return;
            }

            try
            {
                // Check if item exists in database
                var existingItem = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.ItemID == itemId)
                    .Get();

                if (existingItem.Models.Count == 0)
                {
                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new notfound();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    return;
                }

                var item = existingItem.Models.First();

                // Validate all required fields
                if (!ValidateForm())
                {
                    return;
                }

                // Validate uniqueness (excluding current item being updated)
                if (!ValidateItemUniqueness(itemId))
                {
                    return;
                }

                // Get updated values from form
                string itemName = txtItemName.Text.Trim();
                string category = GetComboBoxSelectedValue(Category);

                // Get price WITHOUT peso symbol (clean text value only)
                string price = txtPrice.Text.Trim();

                string supplierName = txtSupplierName.Text.Trim();
                string scNumber = txtSCNumber.Text.Trim();
                string date = ItemDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "";

                // Update the item object (price saved WITHOUT peso symbol)
                item.ItemName = itemName;
                item.Category = category;
                item.Price = price;
                item.SupplierName = supplierName;
                item.SCNumber = scNumber;
                item.Date = date;

                // Update in database using Supabase
                await item.Update<BarbershopManagementSystem>();

                // Update local collection
                var localItem = items.FirstOrDefault(i => i.ItemID == itemId);
                if (localItem != null)
                {
                    var index = items.IndexOf(localItem);
                    items[index] = item;
                }

                // Refresh the display
                await LoadItems();

                // Show success modal
                ModalOverlay.Visibility = Visibility.Visible;
                currentModalWindow = new ItemSuccessfulUpdate();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();

                // Clear form after successful update
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating item: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method to validate form
        private bool ValidateForm()
        {
            bool isValid = true;

            // Validate Item Name
            if (string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                ShowError(txtItemNameError, "Item Name is required");
                isValid = false;
            }
            else
            {
                txtItemNameError.Visibility = Visibility.Collapsed;
            }

            // Validate Category
            if (Category.SelectedItem == null || Category.SelectedIndex <= 0)
            {
                ShowError(txtCategoryError, "Category is required");
                isValid = false;
            }
            else
            {
                txtCategoryError.Visibility = Visibility.Collapsed;
            }

            // Validate Price (check if it's a valid number)
            if (string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                ShowError(txtPriceError, "Price is required");
                isValid = false;
            }
            else if (!decimal.TryParse(txtPrice.Text.Trim(), out _))
            {
                ShowError(txtPriceError, "Price must be a valid number");
                isValid = false;
            }
            else
            {
                txtPriceError.Visibility = Visibility.Collapsed;
            }

            // Validate Supplier Name
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
            {
                ShowError(txtSupplierNameError, "Supplier Name is required");
                isValid = false;
            }
            else
            {
                txtSupplierNameError.Visibility = Visibility.Collapsed;
            }

            // Validate Supplier Contact Number
            if (string.IsNullOrWhiteSpace(txtSCNumber.Text))
            {
                ShowError(txtSCNumberError, "Supplier Contact Number is required");
                isValid = false;
            }
            else
            {
                txtSCNumberError.Visibility = Visibility.Collapsed;
            }

            // Validate Date
            if (ItemDate.SelectedDate == null)
            {
                ShowError(txtDateError, "Date is required");
                isValid = false;
            }
            else
            {
                txtDateError.Visibility = Visibility.Collapsed;
            }

            return isValid;
        }

        // Validate uniqueness when updating (exclude current item)
        private bool ValidateItemUniqueness(string currentItemId)
        {
            bool isValid = true;

            // Clear previous uniqueness errors
            txtItemIDSame.Visibility = Visibility.Collapsed;
            txtCategorySame.Visibility = Visibility.Collapsed;

            string itemName = txtItemName.Text.Trim();
            string category = GetComboBoxSelectedValue(Category);

            // Check if item name already exists in the same category (excluding current item)
            if (!string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(itemName))
            {
                bool hasDuplicate = items.Any(item =>
                    item.ItemID != currentItemId && // Exclude current item
                    item.Category == category &&
                    item.ItemName.Equals(itemName, StringComparison.OrdinalIgnoreCase));

                if (hasDuplicate)
                {
                    ShowError(txtCategorySame, "Item name already exists in this category. Please choose another.");
                    isValid = false;
                }
            }

            return isValid;
        }

        // Helper method to get ComboBox selected value
        private string GetComboBoxSelectedValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem && !selectedItem.IsEnabled)
            {
                return "";
            }

            if (comboBox.SelectedItem is ComboBoxItem validItem)
            {
                return validItem.Content.ToString() ?? "";
            }
            return "";
        }

        // Helper method to clear form
        private void ClearForm()
        {
            cmbProductItem.SelectedIndex = 0; // Reset to placeholder
            txtItemName.Text = "";
            txtPrice.Text = "";
            txtSupplierName.Text = "";
            txtSCNumber.Text = "";
            ItemDate.SelectedDate = null;
            Category.SelectedIndex = 0; // Reset to placeholder
            HideAllErrorMessages();
        }

        private void SetComboBoxSelection(ComboBox comboBox, string value)
        {
            if (comboBox == null || string.IsNullOrEmpty(value)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            if (errorTextBlock != null)
            {
                errorTextBlock.Text = message;
                errorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void HideAllErrorMessages()
        {
            // Basic field validation errors
            txtItemIDError.Visibility = Visibility.Collapsed;
            txtItemNameError.Visibility = Visibility.Collapsed;
            txtCategoryError.Visibility = Visibility.Collapsed;
            txtPriceError.Visibility = Visibility.Collapsed;
            txtSupplierNameError.Visibility = Visibility.Collapsed;
            txtSCNumberError.Visibility = Visibility.Collapsed;
            txtDateError.Visibility = Visibility.Collapsed;

            // Duplicate validation errors
            txtItemIDSame.Visibility = Visibility.Collapsed;
            txtItemNameSame.Visibility = Visibility.Collapsed;
            txtCategorySame.Visibility = Visibility.Collapsed;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            // Get Item ID from selected ComboBox item
            if (cmbProductItem.SelectedItem == null || cmbProductItem.SelectedIndex <= 0)
            {
                ShowError(txtItemIDError, "Please select a Product ID");
                return;
            }

            ComboBoxItem selectedComboItem = cmbProductItem.SelectedItem as ComboBoxItem;
            string employeeId = selectedComboItem?.Content?.ToString() ?? "";

            // Check if Employee ID is empty
            if (string.IsNullOrEmpty(employeeId))
            {
                ShowError(txtItemIDError, "Item ID is required");
                return;
            }

            try
            {
                var employeeToDelete = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.ItemID == employeeId)
                    .Get();

                if (employeeToDelete.Models.Count == 0)
                {
                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    // Open PurchaseOrders as a regular window
                    currentModalWindow = new notfound();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Subscribe to Closed event
                    currentModalWindow.Closed += ModalWindow_Closed;

                    // Show as regular window
                    currentModalWindow.Show();
                    ClearForm();
                    return;
                }

                // Show delete confirmation
                ModalOverlay.Visibility = Visibility.Visible;

                // Open delete confirmation as a regular window
                currentModalWindow = new delete();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // Store reference for dialog result
                delete deleteDialog = (delete)currentModalWindow;

                // Subscribe to Closed event
                currentModalWindow.Closed += ModalWindow_Closed;

                // Show as dialog
                bool? result = deleteDialog.ShowDialog();

                if (result == true)
                {
                    var employee = employeeToDelete.Models.First();

                    await supabase
                        .From<BarbershopManagementSystem>()
                        .Where(x => x.ItemID == employee.ItemID)
                        .Delete();

                    var localEmployee = items.FirstOrDefault(e => e.ItemID == employeeId);
                    if (localEmployee != null)
                    {
                        items.Remove(localEmployee);
                    }

                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    // Open PurchaseOrders as a regular window
                    currentModalWindow = new ItemSuccessfulDelete();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Subscribe to Closed event
                    currentModalWindow.Closed += ModalWindow_Closed;

                    // Show as regular window
                    currentModalWindow.Show();
                    ClearForm();

                    // Reload items to refresh ComboBox
                    await LoadItems();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting employee: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [Table("Add_Item")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("Item_ID", false)]
            public string ItemID { get; set; } = string.Empty;

            [Column("Item_Name")]
            public string ItemName { get; set; } = string.Empty;

            [Column("Category")]
            public string Category { get; set; } = string.Empty;

            [Column("Quantity_Stock")]
            public int? QuantityStock { get; set; }

            [Column("Price")]
            public string Price { get; set; } = string.Empty;

            [Column("Supplier_Name")]
            public string SupplierName { get; set; } = string.Empty;

            [Column("Supplier_CNumber")]
            public string SCNumber { get; set; } = string.Empty;

            [Column("Date")]
            public string Date { get; set; } = string.Empty;
        }
    }
}