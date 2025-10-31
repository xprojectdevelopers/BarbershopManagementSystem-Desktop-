using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Capstone.AppointmentOptions
{

    public partial class Appointment_Records : Window
    {
        private Window currentModalWindow;
        private Supabase.Client client;
        private ObservableCollection<AppointmentModel> appointments = new ObservableCollection<AppointmentModel>();
        private List<AppointmentModel> allAppointments = new List<AppointmentModel>();
        private List<AppointmentModel> originalAppointments = new List<AppointmentModel>();

        private int CurrentPage = 1;
        private int PageSize = 10; // Fixed at 10 items per page
        private int TotalPages = 1;

        public Appointment_Records()
        {
            InitializeComponent();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;

            AppointmentDataGrid.ItemsSource = appointments;

            InitializeSupabase();
        }

        private async void InitializeSupabase()
        {
            try
            {
                string supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
                string supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

                client = new Supabase.Client(supabaseUrl, supabaseKey);
                await client.InitializeAsync();

                await LoadAppointments();
                PopulateBarberDropdown();
                await UpdateStatistics();
                GeneratePaginationButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Supabase: {ex.Message}");
            }
        }

        private async Task LoadAppointments()
        {
            try
            {
                var response = await client.From<AppointmentModel>().Get();

                if (response.Models != null && response.Models.Count > 0)
                {
                    originalAppointments = response.Models.ToList();
                    allAppointments = new List<AppointmentModel>(originalAppointments);

                    // Calculate total pages based on fixed PageSize of 10
                    TotalPages = (int)Math.Ceiling(allAppointments.Count / (double)PageSize);

                    LoadPage(CurrentPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments: {ex.Message}");
            }
        }

        private void LoadPage(int pageNumber)
        {
            CurrentPage = pageNumber;

            var pageData = allAppointments
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            appointments.Clear();
            foreach (var appt in pageData)
            {
                appointments.Add(appt);
            }

            // Fill remaining rows with empty items to always show 10 rows
            while (appointments.Count < PageSize)
            {
                appointments.Add(new AppointmentModel());
            }
        }

        private async Task UpdateStatistics()
        {
            if (client == null) return;

            var result = await client.From<AppointmentModel>().Get();

            int approvedCount = result.Models.Count(e =>
                e.AppointmentStatus?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true
            );

            int ongoingCount = result.Models.Count(e =>
                e.AppointmentStatus != null &&
                e.AppointmentStatus.Replace(" ", "", StringComparison.OrdinalIgnoreCase)
                    .Equals("Ongoing", StringComparison.OrdinalIgnoreCase)
            );

            TotalApprovedText.Text = approvedCount.ToString();
            TotalOngoingText.Text = ongoingCount.ToString();
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

        private void PopulateBarberDropdown()
        {
            var barberComboBox = FindName("BarberComboBox") as ComboBox;

            if (barberComboBox != null)
            {
                var staticBarbers = new List<string>
                {
                    "Barber - Zer",
                    "Barber - Aurbey",
                    "Barber - Klein Eagle",
                    "Barber - Aljames",
                    "Barber - Arel",
                    "Barber - Jay R",
                    "Barber - Cube",
                    "Barber - Andrei"
                };

                var uniqueBarbers = originalAppointments
                    .Where(a => !string.IsNullOrEmpty(a.BarberAssigned))
                    .Select(a => a.BarberAssigned)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();

                var allBarbers = staticBarbers
                    .Union(uniqueBarbers)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();

                barberComboBox.Items.Clear();

                var defaultItem = new ComboBoxItem
                {
                    Content = "All Barbers",
                    IsEnabled = false,
                    IsSelected = true,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                barberComboBox.Items.Add(defaultItem);

                foreach (var barber in allBarbers)
                {
                    barberComboBox.Items.Add(new ComboBoxItem { Content = barber });
                }

                barberComboBox.SelectedIndex = 0;
            }
        }

        private void ApplyFilters()
        {
            var statusComboBox = FindName("StatusComboBox") as ComboBox;
            var barberComboBox = FindName("BarberComboBox") as ComboBox;

            var filtered = originalAppointments.AsEnumerable();

            if (statusComboBox != null && statusComboBox.SelectedIndex > 0)
            {
                var selectedItem = statusComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string selectedStatus = selectedItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedStatus))
                    {
                        filtered = filtered.Where(a => a.AppointmentStatus == selectedStatus);
                    }
                }
            }

            if (barberComboBox != null && barberComboBox.SelectedIndex > 0)
            {
                var selectedItem = barberComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string selectedBarber = selectedItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedBarber))
                    {
                        filtered = filtered.Where(a => a.BarberAssigned == selectedBarber);
                    }
                }
            }

            var filteredList = filtered.ToList();
            allAppointments = filteredList;

            // Recalculate total pages based on filtered results with fixed PageSize of 10
            TotalPages = (int)Math.Ceiling(allAppointments.Count / (double)PageSize);

            CurrentPage = 1;
            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAppointments();
            await UpdateStatistics();
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Appointments Appointments = new Appointments();
            Appointments.Show();
            this.Close();
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

        private void AppointmentUpdate_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;
            currentModalWindow = new AppointmentTracker();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        [Table("appointment_sched")]
        public class AppointmentModel : BaseModel
        {
            [PrimaryKey("id", false)]
            public string Id { get; set; }

            [Column("sched_date")]
            public string AppointmentDate { get; set; }

            [Column("sched_time")]
            public string AppointmentTime { get; set; }

            [Column("customer_name")]
            public string CustomerName { get; set; }

            [Column("contact_number")]
            public string ContactNumber { get; set; }

            [Column("barber_id")]
            public string BarberAssigned { get; set; }

            [Column("status")]
            public string AppointmentStatus { get; set; }
        }
    }
}