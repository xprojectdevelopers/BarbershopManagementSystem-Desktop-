using Microsoft.Win32;
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
using System.IO;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    /// <summary>
    /// Interaction logic for MLVusers.xaml
    /// </summary>
    public partial class MLVusers : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> employees = new();

        private int CurrentPage = 1;
        private int PageSize = 10;
        private int TotalPages = 1;
        private Window? currentModalWindow;

        public MLVusers()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
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
                .Order(x => x.Id, Ordering.Descending)
                .Get();

            employees = new ObservableCollection<BarbershopManagementSystem>(result.Models);

            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

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

            while (pageData.Count < PageSize)
            {
                pageData.Add(new BarbershopManagementSystem());
            }

            EmployeeGrid.ItemsSource = pageData;
        }

        private void GeneratePaginationButtons()
        {
            PaginationPanel.Children.Clear();

            for (int i = 1; i <= TotalPages; i++)
            {
                Button btn = new()
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
            Customers Customers = new();
            Customers.Show();
            this.Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Ask where to save the CSV file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "subscribers_export.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    StringBuilder csvContent = new StringBuilder();
                    csvContent.AppendLine("Email,Contact Number");

                    // Loop through DataGrid items
                    foreach (var item in EmployeeGrid.Items)
                    {
                        dynamic row = item;
                        string email = row.Email != null ? row.Email.ToString() : "";
                        string contact = row.ContactNumber != null ? row.ContactNumber.ToString() : "";
                        csvContent.AppendLine($"{email},{contact}");
                    }

                    // Save to file
                    File.WriteAllText(saveFileDialog.FileName, csvContent.ToString(), Encoding.UTF8);

                    MessageBox.Show("Subscribers successfully exported!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exporting data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 70;
            currentModalWindow.Top = this.Top + 100;
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

        [Table("customer_profiles")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("email")]
            public string Email { get; set; } = string.Empty;

            [Column("display_name")]
            public string DisplayName { get; set; } = string.Empty;

            [Column("contact_number")]
            public string ContactNumber { get; set; } = string.Empty;
        }
    }
}
