using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Capstone.ListSubscribers;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    public partial class Customers : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<QuickMessage> quickMessages = new ObservableCollection<QuickMessage>();

        // 🔹 Pagination Variables
        private int CurrentPage = 1;
        private int PageSize = 5; // 5 messages per page
        private int TotalPages = 1;

        public Customers()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadQuickMessages();
            await LoadCustomerProfilesCount();
            await LoadSubscribersCount();
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

        private async Task LoadQuickMessages()
        {
            if (supabase == null)
                return;

            try
            {
                var result = await supabase
                    .From<QuickMessage>()
                    .Order(x => x.CreatedAt, Ordering.Descending)
                    .Get();

                quickMessages = new ObservableCollection<QuickMessage>(result.Models);

                // 🔹 Compute total pages
                TotalPages = (int)Math.Ceiling(quickMessages.Count / (double)PageSize);

                LoadPage(CurrentPage);
                GeneratePaginationButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        // 🔹 Load a specific page
        private void LoadPage(int pageNumber)
        {
            CurrentPage = pageNumber;

            var pageData = quickMessages
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            CustomerGrid.ItemsSource = pageData;
        }

        // 🔹 Generate pagination buttons
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

                // Remove default style
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

                // Hover effect
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
            Menu Menu = new Menu();
            Menu.Show();
            this.Close();
        }

        private void SeeAllSubscribers_Click(object sender, RoutedEventArgs e)
        {
            // Create instance of ListSubscribers window
            ListSubscribers listSubscribersWindow = new ListSubscribers();

            // Show the window
            listSubscribersWindow.Show();
            this.Hide();  // if you want to just hide it
        }


        // ✅ Existing delete/resolve message logic
        private async void ResolveMessage_Click(object sender, RoutedEventArgs e)
        {
            if (supabase == null)
            {
                MessageBox.Show("Supabase is not initialized.");
                return;
            }

            var button = sender as FrameworkElement;
            var quickMessage = button?.DataContext as QuickMessage;

            if (quickMessage == null)
                return;

            try
            {
                await supabase.From<QuickMessage>().Where(x => x.Id == quickMessage.Id).Delete();
                quickMessages.Remove(quickMessage);

                SuccessQuickMessage popup = new SuccessQuickMessage();
                popup.ShowDialog();

                // 🔹 Refresh pagination after deletion
                TotalPages = (int)Math.Ceiling(quickMessages.Count / (double)PageSize);
                LoadPage(CurrentPage);
                GeneratePaginationButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resolving message: {ex.Message}");
            }
        }

        private async Task LoadCustomerProfilesCount()
        {
            if (supabase == null)
                return;

            try
            {
                // 🔹 Fetch only the IDs of customer profiles
                var result = await supabase
                    .From<CustomerProfile>()
                    .Select("id") // Only select the id column
                    .Get();

                int totalCount = result.Models.Count; // Count locally
                TotalUsersText.Text = totalCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customer count: {ex.Message}");
            }
        }

        private async Task LoadSubscribersCount()
        {
            if (supabase == null)
                return;

            try
            {
                // Fetch subscriber IDs (emails)
                var resultSubscribers = await supabase
                    .From<Subscriber>()
                    .Select("id")
                    .Get();

                // Fetch subscriber_mobile user_ids (contacts)
                var resultMobile = await supabase
                    .From<SubscriberMobile>()
                    .Select("user_id")
                    .Get();

                // Combine unique IDs as strings to handle both long and Guid
                var uniqueIds = new HashSet<string>();

                // Add email subscriber IDs (long -> string)
                foreach (var s in resultSubscribers.Models)
                {
                    uniqueIds.Add(s.Id.ToString());
                }

                // Add mobile user_ids (Guid -> string)
                foreach (var m in resultMobile.Models)
                {
                    uniqueIds.Add(m.UserId.ToString());
                }

                // Total unique subscribers
                TotalSubscribersText.Text = uniqueIds.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading subscriber count: {ex.Message}");
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadQuickMessages();
        }




        // ✅ Model for your Supabase table
        [Table("quick_messages")]
        public class QuickMessage : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("name")]
            public string Name { get; set; } = string.Empty;

            [Column("email")]
            public string Email { get; set; } = string.Empty;

            [Column("phone")]
            public string Phone { get; set; } = string.Empty;

            [Column("message")]
            public string Message { get; set; } = string.Empty;

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }
        }

        [Table("customer_profiles")]
        public class CustomerProfile : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }
        }

        [Table("subscribers")]
        public class Subscriber : BaseModel
        {
            [PrimaryKey("id", false)]
            public long Id { get; set; } // Changed from Guid to long
        }

        [Table("subscribers_mobile")]
        public class SubscriberMobile : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("user_id")]
            public Guid UserId { get; set; }

            [Column("contact_number")]
            public string ContactNumber { get; set; } = string.Empty;
        }

    }
}
