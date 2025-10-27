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
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadItems();
            await LoadItemCount();
            await LoadProductCount();
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

        private async Task LoadProductCount()
        {
            if (supabase == null) return;

            var result = await supabase
                .From<BarbershopManagementSystem>()
                .Get();

            // 1️⃣ Total number of products (unique Item IDs)
            int totalProducts = result.Models.Count;

            // 2️⃣ Total stock across all products (sum of Quantity_Stock)
            int totalStock = result.Models
                .Where(e => e.QuantityStock.HasValue) // ignore null values
                .Sum(e => e.QuantityStock.Value);

            // 3️⃣ Display in UI
            TotalProductText.Text = totalProducts.ToString();
            TotalStockText.Text = totalStock.ToString();
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

        private void ProductUpdate_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ProductDescription();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
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

        // Helper method to get ComboBox selected value
        private string GetComboBoxSelectedValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString() ?? "";
            }
            return "";
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