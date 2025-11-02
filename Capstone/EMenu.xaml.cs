using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    public partial class EMenu : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> employees = new ObservableCollection<BarbershopManagementSystem>();

        private int CurrentPage = 1;
        private int PageSize = 5; // 5 employees per page
        private int TotalPages = 1;
        private Window? currentModalWindow;
        public EMenu()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
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
                .Order(x => x.EmployeeID, Ordering.Ascending) // always in registration order
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
                    EmployeeID = "",
                    EmployeeName = "",
                    EmployeeRole = "",
                    ContactNumber = "",
                    EmergencyContactName = "",
                    EmergencyContact = ""
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

            int total = result.Models.Count(e => e.EmployeeRole?.Equals("Barber", StringComparison.OrdinalIgnoreCase) == true);
            int cashierCount = result.Models.Count(e => e.EmployeeRole?.Equals("Cashier", StringComparison.OrdinalIgnoreCase) == true);

            TotalEmployeesText.Text = total.ToString();
            TotalCashierText.Text = cashierCount.ToString();
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }

        private void PayrollHistory_Click(object sender, RoutedEventArgs e)
        {
            PayrollHistory PayrollHistory = new PayrollHistory();
            PayrollHistory.Show();
            this.Close();
        }

        private void Addemployee_Click(object sender, RoutedEventArgs e)
        {
            AddEmployee AddEmployee = new AddEmployee();
            AddEmployee.Show();
            this.Close();
        }

        private void Employee_Click(object sender, RoutedEventArgs e)
        {
            EmployeeProfile EmployeeProfile = new EmployeeProfile();
            EmployeeProfile.Show();
            this.Close();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 70;
            currentModalWindow.Top = this.Top + 110;
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
                currentModalWindow.Close();

            e.Handled = true;
        }

        // ✅ Employee model
        [Table("Add_Employee")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("Employee_ID", false)]
            public string EmployeeID { get; set; } = string.Empty;

            [Column("Full_Name")]
            public string EmployeeName { get; set; } = string.Empty;

            [Column("Employee_Role")]
            public string EmployeeRole { get; set; } = string.Empty;

            [Column("Contact_Number")]
            public string ContactNumber { get; set; } = string.Empty;

            [Column("EContact_Name")]
            public string EmergencyContactName { get; set; } = string.Empty;

            [Column("EContact_Number")]
            public string EmergencyContact { get; set; } = string.Empty;

            // Computed property for displaying emergency contact in the format "Name - Number"
            public string EmergencyContactDisplay
            {
                get
                {
                    if (string.IsNullOrEmpty(EmergencyContactName) && string.IsNullOrEmpty(EmergencyContact))
                        return "";

                    if (string.IsNullOrEmpty(EmergencyContactName))
                        return EmergencyContact;

                    if (string.IsNullOrEmpty(EmergencyContact))
                        return EmergencyContactName;

                    return $"{EmergencyContactName} - {EmergencyContact}";
                }
            }
        }
    }
}