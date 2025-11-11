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
        private Window? currentModalWindow;

        public Customers()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            await LoadQuickMessages();
            await LoadCustomerProfilesCount();
            await LoadSubscribersCount();
            await LoadMessageCount();
            await LoadLegendCount();
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

        private void SeeAllMLVusers_Click(object sender, RoutedEventArgs e)
        {
            // Create instance of ListSubscribers window
            MLVusers MLVusers = new MLVusers();

            // Show the window
            MLVusers.Show();
            this.Hide();  // if you want to just hide it
        }

        private void SeeAllLegends_Click(object sender, RoutedEventArgs e)
        {
            // Create instance of ListSubscribers window
            MolaveLegend MolaveLegend = new MolaveLegend();

            // Show the window
            MolaveLegend.Show();
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
                // Simple query without SELECT - let it fetch all columns
                var result = await supabase
                    .From<Subscriber>()
                    .Get();

                if (result?.Models != null)
                {
                    int totalCount = result.Models.Count;
                    TotalSubscribersText.Text = totalCount.ToString();
                }
                else
                {
                    TotalSubscribersText.Text = "0";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading subscribers count: {ex.Message}");
                TotalSubscribersText.Text = "0";
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadQuickMessages();
            await LoadCustomerProfilesCount();
            await LoadSubscribersCount();
            await LoadMessageCount();
            await LoadLegendCount();
        }

        private async Task LoadMessageCount()
        {
            if (supabase == null)
                return;

            try
            {
                // 🔹 Fetch only the IDs of customer profiles
                var result = await supabase
                    .From<QuickMessage>()
                    .Select("id") // Only select the id column
                    .Get();

                int totalCount = result.Models.Count; // Count locally
                txtMessage.Text = totalCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customer count: {ex.Message}");
            }
        }

        private async Task LoadLegendCount()
        {
            if (supabase == null)
                return;

            try
            {
                // 🔹 Fetch only records where customer_badge is "Molave Street Legend"
                var result = await supabase
                    .From<Legend>()
                    .Select("id")
                    .Where(x => x.BadgeName == "Molave Street Legend") // Filter by badge name
                    .Get();

                int totalCount = result.Models.Count;
                txtLegend.Text = totalCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading legend count: {ex.Message}");
                txtLegend.Text = "0";
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
            public Guid Id { get; set; }  // ✅ Changed from long to Guid (uuid)

            [Column("email")]
            public string Email { get; set; } = string.Empty;

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }
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

        [Table("appointment_sched")]
        public class Legend : BaseModel
        {
            [PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Column("customer_badge")]
            public string BadgeName { get; set; } = string.Empty;

        }

    }
}
