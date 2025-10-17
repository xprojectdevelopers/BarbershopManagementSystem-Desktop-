using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    // Price Converter Class
    public class PriceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return "";

            string priceValue = value.ToString().Trim();
            if (string.IsNullOrEmpty(priceValue))
                return "";

            return "₱ " + priceValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            string strValue = value.ToString();
            return strValue.Replace("₱", "").Trim();
        }
    }

    public partial class ManageItem : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> items = new ObservableCollection<BarbershopManagementSystem>();
        private ObservableCollection<BarbershopManagementSystem> filteredItems = new ObservableCollection<BarbershopManagementSystem>();

        private int CurrentPage = 1;
        private int PageSize = 10; // 5 items per page
        private int TotalPages = 1;

        private Window currentModalWindow;

        public ManageItem()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadItems();
            await LoadItemCount();
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

        private async Task LoadItems()
        {
            if (supabase == null) return;

            var result = await supabase
                .From<BarbershopManagementSystem>()
                .Order(x => x.ItemID, Ordering.Ascending)
                .Get();

            items = new ObservableCollection<BarbershopManagementSystem>(result.Models);
            filteredItems = new ObservableCollection<BarbershopManagementSystem>(items);

            // compute total pages
            TotalPages = (int)Math.Ceiling(filteredItems.Count / (double)PageSize);

            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }

        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            string selectedCategory = GetComboBoxSelectedValue(Gender);

            if (string.IsNullOrEmpty(selectedCategory) || selectedCategory == "All Items")
            {
                // Show all items sorted by date (latest first)
                filteredItems = new ObservableCollection<BarbershopManagementSystem>(
                    items.OrderByDescending(item => ParseDate(item.Date))
                );
            }
            else
            {
                // Filter by category and sort by date (latest first)
                filteredItems = new ObservableCollection<BarbershopManagementSystem>(
                    items.Where(item => item.Category == selectedCategory)
                        .OrderByDescending(item => ParseDate(item.Date))
                );
            }

            // Reset to first page and update display
            CurrentPage = 1;
            TotalPages = (int)Math.Ceiling(filteredItems.Count / (double)PageSize);
            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }

        private DateTime ParseDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                return parsedDate;
            }
            return DateTime.MinValue; // Return minimum date if parsing fails
        }

        private void LoadPage(int pageNumber)
        {
            CurrentPage = pageNumber;

            var pageData = filteredItems
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Add blank rows if kulang sa PageSize
            while (pageData.Count < PageSize)
            {
                pageData.Add(new BarbershopManagementSystem
                {
                    ItemID = "",
                    ItemName = "",
                    Category = "",
                    QuantityStock = null,
                    Price = "",
                    SupplierName = "",
                    SCNumber = "",
                    Date = ""
                });
            }

            EmployeeGrid.ItemsSource = pageData;
        }

        // Generate ng buttons
        private void GeneratePaginationButtons()
        {
            PaginationPanel.Children.Clear();

            for (int i = 1; i <= TotalPages; i++)
            {
                Button btn = new Button
                {
                    Content = i.ToString(),
                    Margin = new Thickness(5, 0, 5, 0),
                    Padding = new Thickness(10, 5, 10, 5),
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontWeight = (i == CurrentPage) ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = 20,
                    Cursor = Cursors.Hand
                };

                // Custom template to remove default hover effects
                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
                border.SetValue(Border.BorderThicknessProperty, new Thickness(0));

                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

                border.AppendChild(contentPresenter);
                template.VisualTree = border;

                btn.Template = template;

                // Add hover effect
                btn.MouseEnter += (s, e) => btn.Foreground = System.Windows.Media.Brushes.Black;
                btn.MouseLeave += (s, e) => btn.Foreground = System.Windows.Media.Brushes.Gray;

                int pageNum = i;
                btn.Click += (s, e) =>
                {
                    LoadPage(pageNum);
                    GeneratePaginationButtons();
                };

                PaginationPanel.Children.Add(btn);
            }
        }

        // Count items for total
        private async Task LoadItemCount()
        {
            if (supabase == null) return;

            var result = await supabase
                .From<BarbershopManagementSystem>()
                .Get();

            int total = result.Models.Count;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Inventory Inventory = new Inventory();
            Inventory.Show();
            this.Close();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new AddItem();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;

            // Refresh the items list after modal closes
            _ = LoadItems();
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
            string itemId = txtItemID.Text.Trim();

            if (string.IsNullOrEmpty(itemId))
            {
                ShowError(txtItemIDError, "Item ID is required");
                return;
            }

            txtItemIDError.Visibility = Visibility.Collapsed;

            try
            {
                var result = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.ItemID == itemId)
                    .Get();

                if (result.Models.Count > 0)
                {
                    var item = result.Models.First();
                    PopulateForm(item);
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching for item: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateForm(BarbershopManagementSystem item)
        {
            txtItemID.Text = item.ItemID ?? "";

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
            string itemId = txtItemID.Text.Trim();

            // Validate Item ID
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
            if (Category.SelectedItem == null)
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
            if (comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString() ?? "";
            }
            return "";
        }

        // Helper method to clear form
        private void ClearForm()
        {
            txtItemID.Text = "";
            txtItemName.Text = "";
            txtPrice.Text = "";
            txtSupplierName.Text = "";
            txtSCNumber.Text = "";
            ItemDate.SelectedDate = null;
            Category.SelectedItem = null;
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
            string employeeId = txtItemID.Text.Trim();

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