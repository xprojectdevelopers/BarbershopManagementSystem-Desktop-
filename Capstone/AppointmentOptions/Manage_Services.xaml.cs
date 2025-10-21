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

namespace Capstone.AppointmentOptions
{
    public partial class Manage_Services : Window
    {
        private Window currentModalWindow;
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> employees = new ObservableCollection<BarbershopManagementSystem>();

        private int CurrentPage = 1;
        private int PageSize = 5; // 5 employees per page
        private int TotalPages = 1;

        public Manage_Services()
        {
            InitializeComponent();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
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
                .Order(x => x.EmiD, Ordering.Ascending) // always in registration order
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
                    EmiD = "",
                    BN = "",
                    Service = "",
                    Price = "",
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
            Appointments Appointments = new Appointments();
            Appointments.Show();
            this.Close();
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

        private void Service_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new AssignNew_Service();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsNotification();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 95;
            currentModalWindow.Top = this.Top + 110;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 95;
            currentModalWindow.Top = this.Top + 110;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        [Table("AssignNew_Service")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Emp_ID")]
            public String EmiD { get; set; }

            [Column("Barber_Nickname")]
            public string BN { get; set; }

            [Column("Service")]
            public string Service { get; set; }

            [Column("Price")]
            public string Price { get; set; }
        }
    }
}
