using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Capstone
{
    public partial class ListSubscribers : Window
    {
        private Supabase.Client? supabase;

        private ObservableCollection<SubscriberGridItem> subscribersList = new ObservableCollection<SubscriberGridItem>();

        private int CurrentPage = 1;
        private int PageSize = 5;
        private int TotalPages = 1;

        public ListSubscribers()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
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
                // Fetch email subscribers (ID = long)
                var emailResult = await supabase.From<SubscriberEmail>().Get();
                var emails = emailResult.Models;

                // Fetch mobile subscribers (user_id = long)
                var mobileResult = await supabase.From<SubscriberMobile>().Get();
                var mobiles = mobileResult.Models;

                // Combine into grid items
                subscribersList.Clear();
                foreach (var email in emails)
                {
                    // Match mobile by user_id (long)
                    var mobile = mobiles.FirstOrDefault(m => m.UserId == email.Id);
                    subscribersList.Add(new SubscriberGridItem
                    {
                        Email = email.Email,
                        ContactNumber = mobile?.ContactNumber ?? ""
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
            public long Id { get; set; }

            [Column("user_id")]
            public long UserId { get; set; } // long matches subscribers.id

            [Column("contact_number")]
            public string ContactNumber { get; set; } = string.Empty;
        }

        // 🔹 Grid item for DataGrid
        public class SubscriberGridItem
        {
            public string Email { get; set; } = string.Empty;
            public string ContactNumber { get; set; } = string.Empty;
        }
    }
}
