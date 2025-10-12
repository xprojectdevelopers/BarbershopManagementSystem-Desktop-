using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
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
using static Supabase.Postgrest.Constants;

namespace Capstone
{  
    public partial class Appointments : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> employees = new ObservableCollection<BarbershopManagementSystem>();

        private int CurrentPage = 1;
        private int PageSize = 5; // 5 employees per page
        private int TotalPages = 1;

        public Appointments()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadEmployees();
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

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }

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
        }
    }
}
