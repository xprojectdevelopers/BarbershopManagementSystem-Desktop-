using Microsoft.Win32;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Capstone
{
    public partial class Payroll : Window
    {
        private Client supabase;
        private ObservableCollection<Employee> employees;
        private Window currentModalWindow;
        private bool isSavingFundEnabled = true;
        private bool isSaving = false;

        public Payroll()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            SetupPlaceholders();
        }

        private void SetupPlaceholders()
        {
            // Service Count textboxes
            SetupPlaceholder(txtHaircut);
            SetupPlaceholder(txtHaircutReservation);
            SetupPlaceholder(txtHaircutWash);
            SetupPlaceholder(txtHaircutHotTowel);
            SetupPlaceholder(txtHaircutHairDye);
            SetupPlaceholder(txtHaircutHairColor);
            SetupPlaceholder(txtHaircutHighlights);
            SetupPlaceholder(txtHaircutHotBleaching);
            SetupPlaceholder(txtHaircutPerm);
            SetupPlaceholder(txtRebondShort);
            SetupPlaceholder(txtRebondLong);
            SetupPlaceholder(txtBraid);

            // Employee Deduction textboxes
            SetupPlaceholder(txtCashAdvance);
            SetupPlaceholder(txtLate);
            SetupPlaceholder(txtAbsent);
            SetupPlaceholder(txtSavingFund);
        }

        private void SetupPlaceholder(TextBox textBox)
        {
            // Set initial placeholder
            textBox.Text = "0";
            textBox.Foreground = Brushes.Gray;

            // GotFocus: Remove placeholder
            textBox.GotFocus += (s, e) =>
            {
                if (textBox.Text == "0" && textBox.Foreground == Brushes.Gray)
                {
                    textBox.Text = "";
                    textBox.Foreground = Brushes.Black;
                }
            };

            // LostFocus: Restore placeholder if empty
            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "0";
                    textBox.Foreground = Brushes.Gray;
                }
                else
                {
                    textBox.Foreground = Brushes.Black;
                }
            };
        }

        private async Task InitializeSupabaseAsync()
        {
            string supabaseUrl = ConfigurationManager.AppSettings["SupabaseUrl"];
            string supabaseKey = ConfigurationManager.AppSettings["SupabaseKey"];

            supabase = new Client(supabaseUrl, supabaseKey, new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            });

            await supabase.InitializeAsync();
        }

        private async Task InitializeData()
        {
            await InitializeSupabaseAsync();
            employees = new ObservableCollection<Employee>();

            var result = await supabase.From<Employee>().Get();

            // Filter only employees with "Barber" role
            var barbers = result.Models.Where(emp => emp.Role?.Trim().Equals("Barber", StringComparison.OrdinalIgnoreCase) == true);

            foreach (var emp in barbers)
            {
                employees.Add(emp);
            }

            // Populate ComboBox with Barber Employee IDs
            cmbItemID.Items.Clear();

            // Add placeholder item
            var placeholderItem = new ComboBoxItem
            {
                Content = "Select Employee ID",
                IsEnabled = false,
                Foreground = Brushes.Gray
            };
            cmbItemID.Items.Add(placeholderItem);

            // Add barber employee IDs
            foreach (var emp in employees.OrderBy(e => e.EmID))
            {
                var item = new ComboBoxItem
                {
                    Content = emp.EmID,
                    Tag = emp // Store the employee object for later use
                };
                cmbItemID.Items.Add(item);
            }

            // Select placeholder by default
            cmbItemID.SelectedIndex = 0;
        }

        private void cmbItemID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtEmployeeIDError.Visibility = Visibility.Collapsed;
        }

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private void HideAllErrorMessages()
        {
            txtEmployeeIDError.Visibility = Visibility.Collapsed;
            txtNameError.Visibility = Visibility.Collapsed;
            txtReleasDateError.Visibility = Visibility.Collapsed;
            txtReleasDateSame.Visibility = Visibility.Collapsed;
        }

        private void ClearForm()
        {
            cmbItemID.SelectedIndex = 0; // Reset to placeholder
            txtName.Clear();
            txtRole.Clear();

            // Reset to placeholder "0"
            ResetToPlaceholder(txtHaircut);
            ResetToPlaceholder(txtHaircutReservation);
            ResetToPlaceholder(txtHaircutWash);
            ResetToPlaceholder(txtHaircutHotTowel);
            ResetToPlaceholder(txtHaircutHairDye);
            ResetToPlaceholder(txtHaircutHairColor);
            ResetToPlaceholder(txtHaircutHighlights);
            ResetToPlaceholder(txtHaircutHotBleaching);
            ResetToPlaceholder(txtHaircutPerm);
            ResetToPlaceholder(txtRebondShort);
            ResetToPlaceholder(txtRebondLong);
            ResetToPlaceholder(txtBraid);

            ResetToPlaceholder(txtCashAdvance);
            ResetToPlaceholder(txtLate);
            ResetToPlaceholder(txtAbsent);
            ResetToPlaceholder(txtSavingFund);

            txtGrossPay.Clear();
            txtTotalDeduction.Clear();
            txtNetPay.Clear();

            dpStartDate.SelectedDate = null;
            dpEndDate.SelectedDate = null;
            dpReleaseDate.SelectedDate = null;
        }

        private void ResetToPlaceholder(TextBox textBox)
        {
            textBox.Text = "0";
            textBox.Foreground = Brushes.Gray;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"Payroll_History_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            try
            {
                var payrollResult = await supabase.From<PayrollRecord>().Get();

                var validPayrolls = payrollResult.Models
                    .Where(p => !string.IsNullOrWhiteSpace(p.EmID))
                    .OrderByDescending(p => p.Id)
                    .ToList();

                if (!validPayrolls.Any())
                {
                    MessageBox.Show("No payroll data to export.", "Empty Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var csv = new StringBuilder();

                csv.AppendLine("PAYROLL HISTORY REPORT");
                csv.AppendLine($"\"Generated on: {DateTime.Now:MMMM dd, yyyy hh:mm tt}\"");
                csv.AppendLine($"\"Total Records: {validPayrolls.Count}\"");
                csv.AppendLine();

                csv.AppendLine("\"Employee ID\",\"Employee Name\",\"Role\",\"Gross Pay\",\"Saving Fund\",\"Cash Advance\",\"Attendance Deduction\",\"Net Pay\",\"Release Date\"");

                foreach (var payroll in validPayrolls)
                {
                    string[] row = new string[]
                    {
                        CsvEscape(payroll.EmID ?? ""),
                        CsvEscape(payroll.Name ?? ""),
                        CsvEscape(payroll.BRole ?? ""),
                        FormatAmount(payroll.GrossPay),
                        FormatAmount(payroll.SavingFund),
                        FormatAmount(payroll.CashAdvance),
                        FormatAmount(payroll.Absent),
                        FormatAmount(payroll.NetPay),
                        payroll.Release != default(DateTime) ? payroll.Release.ToLocalTime().ToString("yyyy-MM-dd") : ""
                    };

                    csv.AppendLine(string.Join(",", row));
                }

                File.WriteAllText(saveFileDialog.FileName, csv.ToString(), new UTF8Encoding(true));

                MessageBox.Show("✅ Payroll history successfully exported!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatAmount(string amount)
        {
            if (string.IsNullOrWhiteSpace(amount))
                return "0";

            if (decimal.TryParse(amount, out decimal value))
            {
                return value.ToString("0.00");
            }

            return CsvEscape(amount);
        }

        private string CsvEscape(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
                return $"\"{field.Replace("\"", "\"\"")}\"";

            return $"\"{field}\"";
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            // Get selected employee ID from ComboBox
            string employeeId = "";

            if (cmbItemID.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is Employee)
            {
                employeeId = selectedItem.Content.ToString();
            }

            if (string.IsNullOrEmpty(employeeId) || cmbItemID.SelectedIndex == 0)
            {
                ShowError(txtEmployeeIDError, "Please select an Employee ID");
                return;
            }

            txtEmployeeIDError.Visibility = Visibility.Collapsed;

            try
            {
                var result = await supabase
                    .From<Employee>()
                    .Where(x => x.EmID == employeeId)
                    .Get();

                if (result.Models.Count > 0)
                {
                    var employee = result.Models.First();
                    PopulateForm(employee);
                    HideAllErrorMessages();
                }
                else
                {
                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new notfound();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching for employee: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateForm(Employee employee)
        {
            txtName.Text = employee.Fname ?? "";
            txtRole.Text = employee.Role ?? "";
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
            this.Close();
        }

        private void ToggleSavingFund_Click(object sender, RoutedEventArgs e)
        {
            Button toggleButton = sender as Button;
            if (toggleButton != null)
            {
                isSavingFundEnabled = !isSavingFundEnabled;

                if (isSavingFundEnabled)
                {
                    toggleButton.Content = "On";
                    txtSavingFund.IsEnabled = true;
                    txtSavingFund.Background = Brushes.White;
                }
                else
                {
                    toggleButton.Content = "Off";
                    txtSavingFund.IsEnabled = true;
                    txtSavingFund.Background = Brushes.White;
                }
            }
        }

        private void Compute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const decimal HAIRCUT_PRICE = 75;
                const decimal HAIRCUT_RESERVATION_PRICE = 100;
                const decimal HAIRCUT_WASH_PRICE = 125;
                const decimal HAIRCUT_HOT_TOWEL_PRICE = 125;
                const decimal HAIRCUT_HAIR_DYE_PRICE = 175;
                const decimal HAIRCUT_HAIR_COLOR_PRICE = 200;
                const decimal HAIRCUT_HIGHLIGHTS_PRICE = 250;
                const decimal HAIRCUT_HOT_BLEACHING_PRICE = 400;
                const decimal HAIRCUT_PERM_PRICE = 500;
                const decimal REBOND_SHORT_PRICE = 500;
                const decimal REBOND_LONG_PRICE = 500;

                const decimal LATE_DEDUCTION = 30;
                const decimal ABSENT_DEDUCTION = 50;

                int haircutCount = GetNumericValue(txtHaircut);
                int haircutReservationCount = GetNumericValue(txtHaircutReservation);
                int haircutWashCount = GetNumericValue(txtHaircutWash);
                int haircutHotTowelCount = GetNumericValue(txtHaircutHotTowel);
                int haircutHairDyeCount = GetNumericValue(txtHaircutHairDye);
                int haircutHairColorCount = GetNumericValue(txtHaircutHairColor);
                int haircutHighlightsCount = GetNumericValue(txtHaircutHighlights);
                int haircutHotBleachingCount = GetNumericValue(txtHaircutHotBleaching);
                int haircutPermCount = GetNumericValue(txtHaircutPerm);
                int rebondShortCount = GetNumericValue(txtRebondShort);
                int rebondLongCount = GetNumericValue(txtRebondLong);

                decimal braidAmount = GetDecimalValue(txtBraid);

                decimal haircutTotal = haircutCount * HAIRCUT_PRICE;
                decimal haircutReservationTotal = haircutReservationCount * HAIRCUT_RESERVATION_PRICE;
                decimal haircutWashTotal = haircutWashCount * HAIRCUT_WASH_PRICE;
                decimal haircutHotTowelTotal = haircutHotTowelCount * HAIRCUT_HOT_TOWEL_PRICE;
                decimal haircutHairDyeTotal = haircutHairDyeCount * HAIRCUT_HAIR_DYE_PRICE;
                decimal haircutHairColorTotal = haircutHairColorCount * HAIRCUT_HAIR_COLOR_PRICE;
                decimal haircutHighlightsTotal = haircutHighlightsCount * HAIRCUT_HIGHLIGHTS_PRICE;
                decimal haircutHotBleachingTotal = haircutHotBleachingCount * HAIRCUT_HOT_BLEACHING_PRICE;
                decimal haircutPermTotal = haircutPermCount * HAIRCUT_PERM_PRICE;
                decimal rebondShortTotal = rebondShortCount * REBOND_SHORT_PRICE;
                decimal rebondLongTotal = rebondLongCount * REBOND_LONG_PRICE;

                decimal grossPay = haircutTotal + haircutReservationTotal + haircutWashTotal +
                                  haircutHotTowelTotal + haircutHairDyeTotal + haircutHairColorTotal +
                                  haircutHighlightsTotal + haircutHotBleachingTotal + haircutPermTotal +
                                  rebondShortTotal + rebondLongTotal + braidAmount;

                txtGrossPay.Text = grossPay.ToString("N2");

                decimal cashAdvance = GetDecimalValue(txtCashAdvance);
                int lateCount = GetNumericValue(txtLate);
                int absentCount = GetNumericValue(txtAbsent);
                decimal lateDeduction = lateCount * LATE_DEDUCTION;
                decimal absentDeduction = absentCount * ABSENT_DEDUCTION;
                decimal savingFund = GetDecimalValue(txtSavingFund);

                decimal totalDeduction;
                decimal netPay;

                if (isSavingFundEnabled)
                {
                    totalDeduction = cashAdvance + lateDeduction + absentDeduction + savingFund;
                    netPay = grossPay - totalDeduction;
                }
                else
                {
                    totalDeduction = cashAdvance + lateDeduction + absentDeduction;
                    netPay = grossPay - totalDeduction + savingFund;
                }

                txtTotalDeduction.Text = totalDeduction.ToString("N2");
                txtNetPay.Text = netPay.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error computing payroll: {ex.Message}", "Computation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetNumericValue(TextBox textBox)
        {
            string text = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || (text == "0" && textBox.Foreground == Brushes.Gray))
                return 0;
            return int.TryParse(text, out int value) ? value : 0;
        }

        private decimal GetDecimalValue(TextBox textBox)
        {
            string text = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || (text == "0" && textBox.Foreground == Brushes.Gray))
                return 0;
            return decimal.TryParse(text, out decimal value) ? value : 0;
        }

        private string SafeValue(TextBox txt)
        {
            string text = txt.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || (text == "0" && txt.Foreground == Brushes.Gray))
                return "0";
            return text;
        }

        private async void Release_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving) return;

            try
            {
                isSaving = true;
                Button saveButton = (Button)sender;
                saveButton.IsEnabled = false;
                HideAllErrorMessages();

                // Get Employee ID from ComboBox
                string employeeId = "";
                if (cmbItemID.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is Employee)
                {
                    employeeId = selectedItem.Content.ToString();
                }

                if (string.IsNullOrWhiteSpace(employeeId) || cmbItemID.SelectedIndex == 0)
                {
                    ShowError(txtEmployeeIDError, "Please select an Employee ID");
                    return;
                }

                if (!dpReleaseDate.SelectedDate.HasValue)
                {
                    ShowError(txtReleasDateError, "Release Date is required");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtGrossPay.Text.Trim()))
                {
                    MessageBox.Show("Please compute the payroll first.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime releaseDate = DateTime.SpecifyKind(dpReleaseDate.SelectedDate.Value.Date, DateTimeKind.Utc);

                var existingPayroll = await supabase.From<PayrollRecord>().Where(x => x.EmID == employeeId).Get();
                var duplicateRecord = existingPayroll.Models.FirstOrDefault(p => p.Release.Date == releaseDate);

                if (duplicateRecord != null)
                {
                    ShowError(txtReleasDateError, "Release Date Same");
                    return;
                }

                var newPayroll = new PayrollRecord
                {
                    EmID = employeeId,
                    Name = txtName.Text.Trim(),
                    BRole = txtRole.Text.Trim(),
                    GrossPay = SafeValue(txtGrossPay),
                    NetPay = SafeValue(txtNetPay),
                    CashAdvance = SafeValue(txtCashAdvance),
                    SavingFund = SafeValue(txtSavingFund),
                    Absent = SafeValue(txtAbsent),
                    Release = releaseDate
                };

                var result = await supabase.From<PayrollRecord>().Insert(newPayroll);

                if (result != null && result.Models.Count > 0)
                {
                    ModalOverlay.Visibility = Visibility.Visible;
                    currentModalWindow = new ItemSuccessful();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    ClearForm();
                }
                else
                {
                    MessageBox.Show("Failed to save to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack: {ex.StackTrace}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isSaving = false;
                ((Button)sender).IsEnabled = true;
            }
        }

        [Table("Add_Employee")]
        public class Employee : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Full_Name")]
            public string Fname { get; set; }

            [Column("Employee_Role")]
            public string Role { get; set; }

            [Column("Employee_ID")]
            public string EmID { get; set; }
        }

        [Table("Payroll")]
        public class PayrollRecord : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Employee_ID")]
            public string EmID { get; set; }

            [Column("Employee_Name")]
            public string Name { get; set; }

            [Column("Role")]
            public string BRole { get; set; }

            [Column("Gross_Pay")]
            public string GrossPay { get; set; }

            [Column("Saving_Fund")]
            public string SavingFund { get; set; }

            [Column("Cash_Advance")]
            public string CashAdvance { get; set; }

            [Column("Attendance_Deduction")]
            public string Absent { get; set; }

            [Column("Net_Pay")]
            public string NetPay { get; set; }

            [Column("Release_Date")]
            public DateTime Release { get; set; }
        }
    }
}