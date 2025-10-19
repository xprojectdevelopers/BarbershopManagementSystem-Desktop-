using Supabase;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Capstone
{
    public partial class Payroll : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private Window currentModalWindow;
        private bool isSavingFundEnabled = true;
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

            employees = new ObservableCollection<BarbershopManagementSystem>();

            var result = await supabase.From<BarbershopManagementSystem>().Get();
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
            // Basic field validation errors
            txtEmployeeIDError.Visibility = Visibility.Collapsed;
            txtNameError.Visibility = Visibility.Collapsed;
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

            // Clear deduction fields
            txtCashAdvance.Clear();
            txtLate.Clear();
            txtAbsent.Clear();
            txtSavingFund.Clear();

            // Clear computation fields
            txtGrossPay.Clear();
            txtTotalDeduction.Clear();
            txtNetPay.Clear();

            // Clear dates
            dpStartDate.SelectedDate = null;
            dpEndDate.SelectedDate = null;
            dpReleaseDate.SelectedDate = null;
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
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

            // Clear deduction fields
            txtCashAdvance.Clear();
            txtLate.Clear();
            txtAbsent.Clear();
            txtSavingFund.Clear();

            // Clear computation fields
            txtGrossPay.Clear();
            txtTotalDeduction.Clear();
            txtNetPay.Clear();

            // Clear dates
            dpStartDate.SelectedDate = null;
            dpEndDate.SelectedDate = null;
            dpReleaseDate.SelectedDate = null;
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            string employeeId = txtEmployeeID.Text.Trim();

            if (string.IsNullOrEmpty(employeeId))
            {
                ShowError(txtEmployeeIDError, "Employee ID is required");
                return;
            }

            // Clear Employee ID validation error when search is performed with valid ID
            txtEmployeeIDError.Visibility = Visibility.Collapsed;

            try
            {
                var result = await supabase
                    .From<BarbershopManagementSystem>()
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
                    // Hide all validation errors before showing not found dialog
                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    // Open PurchaseOrders as a regular window
                    currentModalWindow = new notfound();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Subscribe to Closed event
                    currentModalWindow.Closed += ModalWindow_Closed;

                    // Show as regular window
                    currentModalWindow.Show();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching for employee: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateForm(BarbershopManagementSystem employee)
        {
            // Personal Details
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
                    // Enable savings fund
                    toggleButton.Content = "On";
                    txtSavingFund.IsEnabled = true;
                    txtSavingFund.Background = Brushes.White;

                    // Update button appearance for "On"
                    var border = toggleButton.Template.FindName("ToggleBorder", toggleButton) as Border;
                    if (border != null)
                    {
                        border.Background = Brushes.Blue;
                        border.BorderBrush = Brushes.Blue;
                    }
                }
                else
                {
                    // Disable savings fund
                    toggleButton.Content = "Off";
                    txtSavingFund.IsEnabled = true;
                    txtSavingFund.Background = Brushes.White;

                    // Update button appearance for "Off"
                    var border = toggleButton.Template.FindName("ToggleBorder", toggleButton) as Border;
                    if (border != null)
                    {
                        border.Background = Brushes.Blue;
                        border.BorderBrush = Brushes.Blue;
                    }
                }
            }
        }

        private void Compute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Service prices
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

                // Fixed deduction amounts
                const decimal LATE_DEDUCTION = 30;
                const decimal ABSENT_DEDUCTION = 50;

                // Parse service counts (default to 0 if empty or invalid)
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

                // Parse braid amount (it's already in pesos)
                decimal braidAmount = decimal.TryParse(txtBraid.Text.Trim(), out decimal ba) ? ba : 0;

                // Calculate individual service totals
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

                // Calculate Gross Pay
                decimal grossPay = haircutTotal + haircutReservationTotal + haircutWashTotal +
                                  haircutHotTowelTotal + haircutHairDyeTotal + haircutHairColorTotal +
                                  haircutHighlightsTotal + haircutHotBleachingTotal + haircutPermTotal +
                                  rebondShortTotal + rebondLongTotal + braidAmount;

                txtGrossPay.Text = grossPay.ToString("N2");

                // Parse deductions
                decimal cashAdvance = decimal.TryParse(txtCashAdvance.Text.Trim(), out decimal ca) ? ca : 0;

                // Parse late and absent counts, then multiply by fixed amounts
                int lateCount = int.TryParse(txtLate.Text.Trim(), out int lc) ? lc : 0;
                int absentCount = int.TryParse(txtAbsent.Text.Trim(), out int ac) ? ac : 0;

                decimal lateDeduction = lateCount * LATE_DEDUCTION;
                decimal absentDeduction = absentCount * ABSENT_DEDUCTION;

                decimal savingFund = decimal.TryParse(txtSavingFund.Text.Trim(), out decimal sf) ? sf : 0;

                // Calculate Total Deduction and Net Pay based on Saving Fund status
                decimal totalDeduction;
                decimal netPay;

                if (isSavingFundEnabled) // Saving Fund is ON
                {
                    // Total Deduction = Cash Advance + Late + Absent + Saving Fund
                    totalDeduction = cashAdvance + lateDeduction + absentDeduction + savingFund;
                    // Net Pay = Gross Pay - Total Deduction
                    netPay = grossPay - totalDeduction;
                }
                else // Saving Fund is OFF
                {
                    // Total Deduction = Cash Advance + Late + Absent (WITHOUT Saving Fund)
                    totalDeduction = cashAdvance + lateDeduction + absentDeduction;
                    // Net Pay = Gross Pay - Total Deduction + Saving Fund (added back)
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

        [Table("Add_Employee")]
        public class BarbershopManagementSystem : BaseModel
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
    }
}
