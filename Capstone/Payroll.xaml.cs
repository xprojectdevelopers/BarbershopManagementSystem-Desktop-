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
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }
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
            txtEmployeeID.Text = string.Empty;
            txtName.Clear();
            txtRole.Clear();

            txtHaircut.Clear();
            txtHaircutReservation.Clear();
            txtHaircutWash.Clear();
            txtHaircutHotTowel.Clear();
            txtHaircutHairDye.Clear();
            txtHaircutHairColor.Clear();
            txtHaircutHighlights.Clear();
            txtHaircutHotBleaching.Clear();
            txtHaircutPerm.Clear();
            txtRebondShort.Clear();
            txtRebondLong.Clear();
            txtBraid.Clear();

            txtCashAdvance.Clear();
            txtLate.Clear();
            txtAbsent.Clear();
            txtSavingFund.Clear();

            txtGrossPay.Clear();
            txtTotalDeduction.Clear();
            txtNetPay.Clear();

            dpStartDate.SelectedDate = null;
            dpEndDate.SelectedDate = null;
            dpReleaseDate.SelectedDate = null;
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            string employeeId = txtEmployeeID.Text.Trim();

            if (string.IsNullOrEmpty(employeeId))
            {
                ShowError(txtEmployeeIDError, "Employee ID is required");
                return;
            }

            txtEmployeeIDError.Visibility = Visibility.Collapsed;

            try
            {
                var result = await supabase
                    .From<Employee>()
                    .Where(x => x.Eid == employeeId)
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

                int haircutCount = int.TryParse(txtHaircut.Text.Trim(), out int hc) ? hc : 0;
                int haircutReservationCount = int.TryParse(txtHaircutReservation.Text.Trim(), out int hrc) ? hrc : 0;
                int haircutWashCount = int.TryParse(txtHaircutWash.Text.Trim(), out int hwc) ? hwc : 0;
                int haircutHotTowelCount = int.TryParse(txtHaircutHotTowel.Text.Trim(), out int hhtc) ? hhtc : 0;
                int haircutHairDyeCount = int.TryParse(txtHaircutHairDye.Text.Trim(), out int hhdc) ? hhdc : 0;
                int haircutHairColorCount = int.TryParse(txtHaircutHairColor.Text.Trim(), out int hhcc) ? hhcc : 0;
                int haircutHighlightsCount = int.TryParse(txtHaircutHighlights.Text.Trim(), out int hhlc) ? hhlc : 0;
                int haircutHotBleachingCount = int.TryParse(txtHaircutHotBleaching.Text.Trim(), out int hhbc) ? hhbc : 0;
                int haircutPermCount = int.TryParse(txtHaircutPerm.Text.Trim(), out int hpc) ? hpc : 0;
                int rebondShortCount = int.TryParse(txtRebondShort.Text.Trim(), out int rsc) ? rsc : 0;
                int rebondLongCount = int.TryParse(txtRebondLong.Text.Trim(), out int rlc) ? rlc : 0;

                decimal braidAmount = decimal.TryParse(txtBraid.Text.Trim(), out decimal ba) ? ba : 0;

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

                decimal cashAdvance = decimal.TryParse(txtCashAdvance.Text.Trim(), out decimal ca) ? ca : 0;
                int lateCount = int.TryParse(txtLate.Text.Trim(), out int lc) ? lc : 0;
                int absentCount = int.TryParse(txtAbsent.Text.Trim(), out int ac) ? ac : 0;
                decimal lateDeduction = lateCount * LATE_DEDUCTION;
                decimal absentDeduction = absentCount * ABSENT_DEDUCTION;
                decimal savingFund = decimal.TryParse(txtSavingFund.Text.Trim(), out decimal sf) ? sf : 0;

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

        private async void Release_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving) return;

            try
            {
                isSaving = true;
                Button saveButton = (Button)sender;
                saveButton.IsEnabled = false;
                HideAllErrorMessages();

                // Validation
                if (string.IsNullOrWhiteSpace(txtEmployeeID.Text.Trim()))
                {
                    ShowError(txtEmployeeIDError, "Employee ID is required");
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

                string employeeId = txtEmployeeID.Text.Trim();
                // Fix timezone issue - use Date only and specify as UTC
                DateTime releaseDate = DateTime.SpecifyKind(dpReleaseDate.SelectedDate.Value.Date, DateTimeKind.Utc);

                // Check duplicate
                var existingPayroll = await supabase.From<PayrollRecord>().Where(x => x.EmID == employeeId).Get();
                var duplicateRecord = existingPayroll.Models.FirstOrDefault(p => p.Release.Date == releaseDate);

                if (duplicateRecord != null)
                {
                    ShowError(txtReleasDateError, "Release Date Same");
                    return;
                }

                // Parse values
                decimal grossPayDecimal = decimal.Parse(txtGrossPay.Text.Trim().Replace(",", ""));
                decimal netPayDecimal = decimal.Parse(txtNetPay.Text.Trim().Replace(",", ""));
                decimal cashAdvanceDecimal = decimal.TryParse(txtCashAdvance.Text.Trim(), out var ca) ? ca : 0;
                decimal savingFundDecimal = decimal.TryParse(txtSavingFund.Text.Trim(), out var sf) ? sf : 0;
                long absentCount = long.TryParse(txtAbsent.Text.Trim(), out var ab) ? ab : 0;

                var newPayroll = new PayrollRecord
                {
                    EmID = employeeId,
                    Name = txtName.Text.Trim(),
                    BRole = txtRole.Text.Trim(),
                    GrossPay = (long)(grossPayDecimal * 100),
                    NetPay = (long)(netPayDecimal * 100),
                    CashAdvance = (long)(cashAdvanceDecimal * 100),
                    SavingFund = (long)(savingFundDecimal * 100),
                    Absent = absentCount,
                    Release = releaseDate
                };

                // Debug log
                System.Diagnostics.Debug.WriteLine($"Inserting: EmID={employeeId}, GrossPay={newPayroll.GrossPay}");

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

        // Employee table model (Add_Employee)
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
            public string Eid { get; set; }
        }

        // Payroll table model - Match Supabase int8 (bigint) types
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
            public long GrossPay { get; set; }

            [Column("Saving_Fund")]
            public long SavingFund { get; set; }

            [Column("Cash_Advance")]
            public long CashAdvance { get; set; }

            [Column("Attendance_Deduction")]
            public long Absent { get; set; }

            [Column("Net_Pay")]
            public long NetPay { get; set; }

            [Column("Release_Date")]
            public DateTime Release { get; set; }
        }
    }
}