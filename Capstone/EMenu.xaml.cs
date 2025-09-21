using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    public partial class EMenu : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<Employee> employees = new ObservableCollection<Employee>();

        private int CurrentPage = 1;
        private int PageSize = 5; // 5 employees per page
        private int TotalPages = 1;

        public EMenu()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
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
                .From<Employee>()
                .Order(x => x.EmployeeID, Ordering.Ascending) // always in registration order
                .Get();

            employees = new ObservableCollection<Employee>(result.Models);

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
                pageData.Add(new Employee
                {
                    EmployeeID = "", 
                    EmployeeName = "",
                    ContactNumber = "",
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
                    Background = (i == CurrentPage) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.LightGray,
                    Foreground = (i == CurrentPage) ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black
                };

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
                .From<Employee>()
                .Get();

            int total = result.Models.Count;
            TotalEmployeesText.Text = total.ToString();
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }

        private void Addemployee_Click(object sender, RoutedEventArgs e)
        {
            Testing Testing = new Testing();
            Testing.Show();
            this.Close();
        }

        private void Employee_Click(object sender, RoutedEventArgs e)
        {
            EmployeeProfile EmployeeProfile = new EmployeeProfile();
            EmployeeProfile.Show();
            this.Close();
        }

        // ✅ Employee model
        [Table("Register_Employees")]
        public class Employee : BaseModel
        {
            [PrimaryKey("Eid", false)]
            public string EmployeeID { get; set; } = string.Empty;

            [Column("Fname")]
            public string EmployeeName { get; set; } = string.Empty;

            [Column("PNumber")]
            public string ContactNumber { get; set; } = string.Empty;

            [Column("Econtact")]
            public string EmergencyContact { get; set; } = string.Empty;
        }
    }
}
