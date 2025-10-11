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

namespace Capstone
{
    /// <summary>
    /// Interaction logic for AddItem.xaml
    /// </summary>
    public partial class AddItem : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private Window currentModalWindow;
        private bool isSaving = false;

        public AddItem()
        {

            InitializeComponent();
            Loaded += async (s, e) => await InitializeData(); // Initialize when window is loaded

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
        }

        private void btnGenerateID_Click(object sender, RoutedEventArgs e)
        {
            string prefix = "MSBI";
            int nextNumber = 1;

            // Find all items with IDs starting with "MSBI-"
            var existingIDs = employees
                .Where(emp => !string.IsNullOrEmpty(emp.ItemID) && emp.ItemID.StartsWith(prefix))
                .Select(emp => emp.ItemID)
                .ToList();

            if (existingIDs.Any())
            {
                // Extract the last numeric part from each ID
                var maxNumber = existingIDs
                    .Select(id =>
                    {
                        var parts = id.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int num))
                            return num;
                        return 0;
                    })
                    .Max();

                nextNumber = maxNumber + 1;
            }

            // Format as MSBI-0001, MSBI-0002, etc.
            string newID = $"{prefix}-{nextNumber:D4}";

            // Display or assign the new ID (example: txtItemID is your TextBox)
            txtItemID.Text = newID;
        }

        private void ClearAllValidationErrors()
        {
            // Clear all error TextBlocks visibility
            txtItemIDError.Visibility = Visibility.Collapsed;
            txtItemNameError.Visibility = Visibility.Collapsed;
            txtCategoryError.Visibility = Visibility.Collapsed;
            txtPriceError.Visibility = Visibility.Collapsed;
            txtSupplierNameError.Visibility = Visibility.Collapsed;
            txtSCNumberError.Visibility = Visibility.Collapsed;
            txtDateError.Visibility = Visibility.Collapsed;
        }

        private void ShowValidationError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private bool ValidateAllRequiredFieldsInline(BarbershopManagementSystem newEmployee)
        {
            bool isValid = true;

            // Clear all previous errors
            ClearAllValidationErrors();

            if (string.IsNullOrWhiteSpace(newEmployee.ItemID))
            {
                ShowValidationError(txtItemIDError, "Item ID is required");
                isValid = false;
            }

            // Check all other required fields marked with * 
            if (string.IsNullOrWhiteSpace(newEmployee.ItemName))
            {
                ShowValidationError(txtItemNameError, "Item Name is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Category))
            {
                ShowValidationError(txtCategoryError, "Category is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Price))
            {
                ShowValidationError(txtPriceError, "Price is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.SupplierName))
            {
                ShowValidationError(txtSupplierNameError, "Supplier Name Name is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.SCNumber))
            {
                ShowValidationError(txtSCNumberError, "Supplier Contact Number is required");
                isValid = false;
            }


            if (!newEmployee.Date.HasValue)
            {
                ShowValidationError(txtDateError, "Date is required");
                isValid = false;
            }

            return isValid;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Prevent double-clicking
            if (isSaving)
                return;

            try
            {
                // Set saving flag and disable button
                isSaving = true;
                Button saveButton = (Button)sender;
                saveButton.IsEnabled = false;

                // Only clear required field validation errors, not uniqueness errors
                ClearAllValidationErrors();

                var newEmployee = new BarbershopManagementSystem
                {
                    ItemID = txtItemID.Text.Trim(),
                    ItemName = txtItemName.Text.Trim(),
                    Category = GetSelectedComboBoxValue(Category),
                    Price = txtPrice.Text.Trim(),
                    SupplierName = txtSupplierName.Text.Trim(),
                    SCNumber = txtSCNumber.Text.Trim(),
                    Date = ItemDate.SelectedDate,
                };

           

                // Validate required fields first
                bool isRequiredValid = ValidateAllRequiredFieldsInline(newEmployee);

                if (!isRequiredValid)
                {
                    // Re-enable button if validation fails
                    saveButton.IsEnabled = true;
                    saveButton.Content = "Save";
                    isSaving = false;
                    return;
                }

                // Save to Supabase database
                var result = await supabase.From<BarbershopManagementSystem>().Insert(newEmployee);

                if (result != null && result.Models.Count > 0)
                {
                    // Add to local collection only once
                    employees.Add(result.Models[0]);

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

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private string GetSelectedComboBoxValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.IsEnabled)
            {
                return selectedItem.Content.ToString();
            }
            return null;
        }

        [Table("Add_Item")] // pangalan ng table sa Supabase
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Item_ID")]
            public string ItemID { get; set; }

            [Column("Item_Name")]
            public string ItemName { get; set; }

            [Column("Category")]
            public string Category { get; set; }

            [Column("Price")]
            public string Price { get; set; }

            [Column("Supplier_Name")]
            public string SupplierName { get; set; }

            [Column("Supplier_CNumber")]
            public string SCNumber { get; set; }

            [Column("Date")]
            public DateTime? Date { get; set; }
        }  
    }
}
