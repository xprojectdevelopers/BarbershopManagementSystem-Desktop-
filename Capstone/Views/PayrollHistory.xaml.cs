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
using System.Windows.Documents;
using System.Windows.Input;
using static Supabase.Postgrest.Constants;

namespace Capstone
{

    public partial class PayrollHistory : Window
    {
        private Supabase.Client? supabase;
        private ObservableCollection<BarbershopManagementSystem> employees = new ObservableCollection<BarbershopManagementSystem>();

        private int CurrentPage = 1;
        private int PageSize = 5; // 5 employees per page
        private int TotalPages = 1;

        public PayrollHistory()
        {
            InitializeComponent();
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
                .Order(x => x.Id, Ordering.Descending) // ✅ Pinaka-recent muna
                .Get();

            employees = new ObservableCollection<BarbershopManagementSystem>(result.Models);

            // compute total pages
            TotalPages = (int)Math.Ceiling(employees.Count / (double)PageSize);

            // Ensure at least 1 page
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

            // Add blank rows if kulang sa PageSize
            while (pageData.Count < PageSize)
            {
                pageData.Add(new BarbershopManagementSystem
                {
                    EmID = "",
                    Name = "",
                    BRole = "",
                    GrossPay = null,
                    SavingFund = null,
                    CashAdvance = null,
                    Absent = null,
                    NetPay = null,
                    Release = null
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

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
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

                    StringBuilder csv = new StringBuilder();

                    // Title and report info
                    csv.AppendLine("PAYROLL HISTORY REPORT");
                    csv.AppendLine($"Generated on: {DateTime.Now:MMMM dd, yyyy hh:mm tt}");
                    csv.AppendLine($"Total Employees: {validEmployees.Count}");
                    csv.AppendLine();

                    // Column headers (clean spacing)
                    csv.AppendLine("Employee ID,Employee Name,Role,Gross Pay,Saving Fund,Cash Advance,Attendance Deduction,Net Pay,Release Date");

                    // Data rows
                    foreach (var emp in validEmployees)
                    {
                        string emID = CsvEscape(emp.EmID);
                        string name = CsvEscape(emp.Name);
                        string role = CsvEscape(emp.BRole);
                        string gross = emp.GrossPay.HasValue ? emp.GrossPay.Value.ToString("N2") : "";
                        string saving = emp.SavingFund.HasValue ? emp.SavingFund.Value.ToString("N2") : "";
                        string advance = emp.CashAdvance.HasValue ? emp.CashAdvance.Value.ToString("N2") : "";
                        string absent = emp.Absent.HasValue ? emp.Absent.Value.ToString("N2") : "";
                        string net = emp.NetPay.HasValue ? emp.NetPay.Value.ToString("N2") : "";
                        string release = CsvEscape(emp.Release ?? "");

                        // No peso symbols in CSV — Excel will detect as numeric
                        csv.AppendLine($"{emID},{name},{role},{gross},{saving},{advance},{absent},{net},{release}");
                    }

                    // Save with UTF-8 BOM for Excel compatibility
                    File.WriteAllText(saveFileDialog.FileName, csv.ToString(), new UTF8Encoding(true));

                    MessageBox.Show(
                        "✅ Payroll history successfully exported and formatted!\n\n" +
                        "💡 Tip: In Excel, select the currency columns and apply '₱ Philippine Peso' format for best appearance.",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // Helper method to properly escape CSV fields
        private string CsvEscape(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            EMenu EMenu = new EMenu();
            EMenu.Show();
            this.Close();
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
            public long? GrossPay { get; set; }

            [Column("Saving_Fund")]
            public long? SavingFund { get; set; }

            [Column("Cash_Advance")]
            public long? CashAdvance { get; set; }

            [Column("Attendance_Deduction")]
            public long? Absent { get; set; }

            [Column("Net_Pay")]
            public long? NetPay { get; set; }

            [Column("Release_Date")]
            public String Release { get; set; }
        }
    }
}