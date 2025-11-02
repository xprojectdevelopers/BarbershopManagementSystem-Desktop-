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
using Microsoft.Win32;
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

        // 🔹 Load subscribers with emails + contact numbers
        private async Task LoadSubscribers()
        {
            if (supabase == null) return;

            try
            {
                // Fetch email subscribers (website)
                var emailResult = await supabase.From<SubscriberEmail>().Get();
                var emails = emailResult.Models.ToList();

                // Fetch mobile subscribers (mobile)
                var mobileResult = await supabase.From<SubscriberMobile>().Get();
                var mobiles = mobileResult.Models.ToList();

                subscribersList.Clear();

                int maxCount = Math.Max(emails.Count, mobiles.Count);

                // Combine them row by row
                for (int i = 0; i < maxCount; i++)
                {
                    string email = i < emails.Count ? emails[i].Email : "";
                    string contact = i < mobiles.Count ? mobiles[i].ContactNumber : "";

                    // Add combined row
                    subscribersList.Add(new SubscriberGridItem
                    {
                        Email = email,
                        ContactNumber = contact
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

        // 🔹 Models
        [Table("subscribers")]
        public class SubscriberEmail : BaseModel
        {
            [PrimaryKey("id", false)]
            public long Id { get; set; } // long matches bigint in Supabase

            [Column("email")]
            public string Email { get; set; } = string.Empty;
        }

        [Table("subscribers_mobile")]
        public class SubscriberMobile : BaseModel
        {
            [PrimaryKey("id", false)]
            [Column("id")]
            public Guid Id { get; set; } // UUID -> use Guid

            [Column("user_id")]
            public Guid UserId { get; set; }

            [Column("contact_number")]
            public string ContactNumber { get; set; } = string.Empty;

            [Column("created_at")]
            public DateTime? CreatedAt { get; set; }
        }

        // 🔹 Grid item for DataGrid
        public class SubscriberGridItem
        {
            public string Email { get; set; } = string.Empty;
            public string ContactNumber { get; set; } = string.Empty;
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
                    foreach (var item in SubscribersGrid.Items)
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
    }
}

