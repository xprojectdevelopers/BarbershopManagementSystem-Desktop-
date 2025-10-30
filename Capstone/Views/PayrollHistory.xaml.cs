using Microsoft.Win32;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone
{
    public class PesoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(str))
                return ""; // No peso sign if no value

            // Format numbers with commas and decimals
            if (decimal.TryParse(str, out decimal number))
                return $"₱ {number:N2}";

            return $"₱ {str}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            string str = value.ToString();
            return str.Replace("₱", "").Trim();
        }
    }

    public partial class PayrollHistory : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> allEmployees = new();
        private ObservableCollection<BarbershopManagementSystem> employees = new();
        private List<string> distinctEmployeeIDs = new();

        private int CurrentPage = 1;
        private int PageSize = 5;
        private int TotalPages = 1;
        private Window? currentModalWindow;

        public PayrollHistory()
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

            allEmployees = new ObservableCollection<BarbershopManagementSystem>(result.Models);
            employees = new ObservableCollection<BarbershopManagementSystem>(allEmployees);

            // Populate ComboBox with distinct Employee IDs
            distinctEmployeeIDs = allEmployees
                .Where(emp => !string.IsNullOrWhiteSpace(emp.EmID))
                .Select(emp => emp.EmID)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            PopulateEmployeeIDComboBox();

            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }

        private void PopulateEmployeeIDComboBox()
        {
            cmbItemID.Items.Clear();

            // Add placeholder
            ComboBoxItem placeholder = new ComboBoxItem
            {
                Content = "Select Employee ID",
                IsEnabled = false,
                Foreground = System.Windows.Media.Brushes.Gray
            };
            cmbItemID.Items.Add(placeholder);



            // Add distinct employee IDs
            foreach (var empID in distinctEmployeeIDs)
            {
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = empID,
                    Tag = empID
                };
                cmbItemID.Items.Add(item);
            }

            cmbItemID.SelectedIndex = 0;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear error message
            if (txtEmployeeIDError != null)
            {
                txtEmployeeIDError.Visibility = Visibility.Collapsed;
            }

            // Check if an employee ID is selected
            if (cmbItemID.SelectedIndex <= 0)
            {
                if (txtEmployeeIDError != null)
                {
                    txtEmployeeIDError.Text = "Please select an Employee ID";
                    txtEmployeeIDError.Visibility = Visibility.Visible;
                }
                return;
            }

            var selectedItem = cmbItemID.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string selectedTag = selectedItem.Tag?.ToString() ?? "";

            if (selectedTag == "ALL")
            {
                // Show all employees
                employees = new ObservableCollection<BarbershopManagementSystem>(allEmployees);
            }
            else
            {
                // Filter by selected Employee ID
                var filteredEmployees = allEmployees
                    .Where(emp => emp.EmID == selectedTag)
                    .ToList();

                if (!filteredEmployees.Any())
                {
                    if (txtEmployeeIDError != null)
                    {
                        txtEmployeeIDError.Text = "No payroll records found for this Employee ID";
                        txtEmployeeIDError.Visibility = Visibility.Visible;
                    }
                    employees = new ObservableCollection<BarbershopManagementSystem>();
                }
                else
                {
                    employees = new ObservableCollection<BarbershopManagementSystem>(filteredEmployees);
                }
            }

            // Reset pagination
            CurrentPage = 1;
            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            LoadPage(CurrentPage);
            GeneratePaginationButtons();
        }

        private void cmbItemID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear error message when selection changes
            if (txtEmployeeIDError != null)
            {
                txtEmployeeIDError.Visibility = Visibility.Collapsed;
            }
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

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"Payroll_History_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var validEmployees = employees
                        .Where(emp => !string.IsNullOrWhiteSpace(emp.EmID))
                        .OrderByDescending(emp => emp.Id)
                        .ToList();

                    if (!validEmployees.Any())
                    {
                        MessageBox.Show("No payroll data to export.", "Empty Data", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    StringBuilder csv = new();
                    csv.AppendLine("PAYROLL HISTORY REPORT");
                    csv.AppendLine($"Generated on: {DateTime.Now:MMMM dd, yyyy hh:mm tt}");
                    csv.AppendLine($"Total Employees: {validEmployees.Count}");
                    csv.AppendLine();
                    csv.AppendLine("Employee ID,Employee Name,Role,Gross Pay,Saving Fund,Cash Advance,Attendance Deduction,Net Pay,Release Date");

                    foreach (var emp in validEmployees)
                    {
                        csv.AppendLine(string.Join(",", new[]
                        {
                            CsvEscape(emp.EmID),
                            CsvEscape(emp.Name),
                            CsvEscape(emp.BRole),
                            CsvEscape(emp.GrossPay),
                            CsvEscape(emp.SavingFund),
                            CsvEscape(emp.CashAdvance),
                            CsvEscape(emp.Absent),
                            CsvEscape(emp.NetPay),
                            CsvEscape(emp.Release ?? "")
                        }));
                    }

                    File.WriteAllText(saveFileDialog.FileName, csv.ToString(), new UTF8Encoding(true));

                    MessageBox.Show("✅ Payroll history successfully exported!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string CsvEscape(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            EMenu EMenu = new();
            EMenu.Show();
            this.Close();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 90;
            currentModalWindow.Top = this.Top + 90;
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

        [Table("Payroll")]
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Employee_ID")]
            public string EmID { get; set; } = string.Empty;

            [Column("Employee_Name")]
            public string Name { get; set; } = string.Empty;

            [Column("Role")]
            public string BRole { get; set; } = string.Empty;

            [Column("Gross_Pay")]
            public string GrossPay { get; set; } = string.Empty;

            [Column("Saving_Fund")]
            public string SavingFund { get; set; } = string.Empty;

            [Column("Cash_Advance")]
            public string CashAdvance { get; set; } = string.Empty;

            [Column("Attendance_Deduction")]
            public string Absent { get; set; } = string.Empty;

            [Column("Net_Pay")]
            public string NetPay { get; set; } = string.Empty;

            [Column("Release_Date")]
            public string Release { get; set; } = string.Empty;
        }
    }
}