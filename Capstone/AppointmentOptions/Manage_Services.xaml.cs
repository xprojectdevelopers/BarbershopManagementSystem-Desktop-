using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
using static Capstone.Customers;
using static Supabase.Postgrest.Constants;

namespace Capstone.AppointmentOptions
{
    // ✅ ADD THIS CONVERTER CLASS
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;
            return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class Manage_Services : Window
    {
        private Window currentModalWindow;
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> allEmployees = new ObservableCollection<BarbershopManagementSystem>();
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
                .Order(x => x.EmiD, Ordering.Ascending)
                .Get();

            allEmployees = new ObservableCollection<BarbershopManagementSystem>(result.Models);
            employees = new ObservableCollection<BarbershopManagementSystem>(allEmployees);

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

            // Add blank rows if less than PageSize
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

        private async void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                var service = btn.DataContext as BarbershopManagementSystem;

                // ✅ CHECK IF EMPTY ROW - Just return silently
                if (service == null || string.IsNullOrWhiteSpace(service.EmiD))
                {
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
                    if (supabase == null)
                    {
                        MessageBox.Show("Database connection not initialized.");
                        return;
                    }

                    await supabase
                        .From<BarbershopManagementSystem>()
                        .Where(x => x.Id == service.Id)
                        .Delete();

                    await LoadEmployees();

                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new MangeServiceDelete();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error deleting service: {ex.Message}");
            }
        }

        private void ServiceDes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                var service = btn.DataContext as BarbershopManagementSystem;

                // ✅ CHECK IF EMPTY ROW - Just return silently
                if (service == null || string.IsNullOrWhiteSpace(service.EmiD))
                {
                    return;
                }

                ModalOverlay.Visibility = Visibility.Visible;

                // Pass all 5 required parameters
                currentModalWindow = new Service_Description(
                    service.Id,           // int id
                    service.EmiD,         // string empId
                    service.BN,           // string barberNickname
                    service.Service,      // string service
                    service.Price         // string price
                );
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error opening service details: {ex.Message}");
            }
        }

        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            string searchText = txtEmployeeID.Text.Trim();

            // If empty, show all
            if (string.IsNullOrEmpty(searchText))
            {
                employees = new ObservableCollection<BarbershopManagementSystem>(allEmployees);

                CurrentPage = 1;
                TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
                if (TotalPages == 0) TotalPages = 1;

                LoadPage(CurrentPage);
                GeneratePaginationButtons();
                return;
            }

            // Search by Employee ID (works for single character or full ID)
            var searchResults = allEmployees.Where(emp =>
                !string.IsNullOrEmpty(emp.EmiD) &&
                emp.EmiD.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

            // If NO results found - show notfound modal
            if (searchResults.Count == 0)
            {
                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new notfound();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();

                ClearForm();
                return; // Don't update table
            }

            // If results found - update table
            employees = new ObservableCollection<BarbershopManagementSystem>(searchResults);

            CurrentPage = 1;
            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }

        private void ClearForm()
        {
            txtEmployeeID.Text = string.Empty;
            txtEmployeeIDError.Text = string.Empty;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadEmployees();
        }

        public async void RefreshGrid()
        {
            await LoadEmployees();
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