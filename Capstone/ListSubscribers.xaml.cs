using Microsoft.Win32;
using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;

namespace Capstone
{
    public partial class ListSubscribers : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<SubscriberGridItem> subscribersList = new ObservableCollection<SubscriberGridItem>();
        private int CurrentPage = 1;
        private int PageSize = 5;
        private int TotalPages = 1;
        private Window? currentModalWindow;

        public ListSubscribers()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadSubscribers();
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

        // 🔹 Load email subscribers
        private async Task LoadSubscribers()
        {
            if (supabase == null) return;

            try
            {
                // Fetch email subscribers only
                var emailResult = await supabase.From<SubscriberEmail>().Get();
                var emails = emailResult.Models.ToList();

                subscribersList.Clear();

                // Add email subscribers to the list
                foreach (var email in emails)
                {
                    subscribersList.Add(new SubscriberGridItem
                    {
                        Email = email.Email
                    });
                }

                // Pagination
                TotalPages = (int)Math.Ceiling(subscribersList.Count / (double)PageSize);
                LoadSubscriberPage(CurrentPage);
                GeneratePaginationButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading subscribers: {ex.Message}");
            }
        }

        private void LoadSubscriberPage(int pageNumber)
        {
            CurrentPage = pageNumber;

            var pageData = subscribersList
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            SubscribersGrid.ItemsSource = pageData;
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
                    Foreground = (i == CurrentPage) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Gray,
                    FontWeight = (i == CurrentPage) ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = 16,
                    Cursor = Cursors.Hand,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0)
                };

                int pageNum = i;
                btn.Click += (s, e) =>
                {
                    LoadSubscriberPage(pageNum);
                    GeneratePaginationButtons();
                };

                PaginationPanel.Children.Add(btn);
            }
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

        // 🔹 Model for email subscribers
        [Table("subscribers")]
        public class SubscriberEmail : BaseModel
        {
            [PrimaryKey("id", false)]
            public string Id { get; set; } = string.Empty;

            [Column("email")]
            public string Email { get; set; } = string.Empty;
        }

        // 🔹 Grid item for DataGrid
        public class SubscriberGridItem
        {
            public string Email { get; set; } = string.Empty;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Customers Customers = new();
            Customers.Show();
            this.Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (subscribersList.Count == 0)
            {
                MessageBox.Show("No subscribers to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
                    csvContent.AppendLine("Email");

                    // Loop through all subscribers (not just current page)
                    foreach (var item in subscribersList)
                    {
                        csvContent.AppendLine($"{item.Email}");
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
    }
}