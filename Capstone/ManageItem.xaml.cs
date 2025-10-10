using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone
{

    public partial class ManageItem : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> employees = new ObservableCollection<BarbershopManagementSystem>();

        private int CurrentPage = 1;
        private int PageSize = 5; // 5 employees per page
        private int TotalPages = 1;

        private Window currentModalWindow;

        public ManageItem()
        {
            InitializeComponent();
            Loaded += async(s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadEmployees();
            await LoadEmployeeCount();
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


        private async Task LoadEmployees()
        {
            if (supabase == null) return;

            var result = await supabase
                .From<BarbershopManagementSystem>()
                .Order(x => x.ItemID, Ordering.Ascending) // always in registration order
                .Get();

            employees = new ObservableCollection<BarbershopManagementSystem>(result.Models);

            // compute total pages
            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);

            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }


        private void LoadPage(int pageNumber)
        {
            CurrentPage = pageNumber;

            var pageData = employees
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
                    Price = null,
                    SupplierName = "",
                    SCNumber = null,
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
                    Foreground = System.Windows.Media.Brushes.Gray, // Default gray color
                    FontWeight = (i == CurrentPage) ? FontWeights.Bold : FontWeights.Normal, // Bold for current page
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

        // Count employees for total
        private async Task LoadEmployeeCount()
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

            // Open PurchaseOrders as a regular window
            currentModalWindow = new AddItem();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Subscribe to Closed event
            currentModalWindow.Closed += ModalWindow_Closed;

            // Show as regular window
            currentModalWindow.Show();
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private void ModalOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            // Close the modal window when clicking on the overlay
            if (currentModalWindow != null)
            {
                currentModalWindow.Close();
            }

            // Mark event as handled
            e.Handled = true;
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
            public int? Price { get; set; }

            [Column("Supplier_Name")]
            public string SupplierName { get; set; } = string.Empty;

            [Column("Supplier_CNumber")]
            public long? SCNumber { get; set; }

            [Column("Date")]
            public string Date { get; set; } = string.Empty;
        }

    }
}
