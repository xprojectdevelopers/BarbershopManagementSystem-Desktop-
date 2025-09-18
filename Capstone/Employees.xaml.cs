using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Capstone
{
    public partial class Employees : Window
    {
        private Client supabase;
        private ObservableCollection<Employee> employees;

        public Employees()
        {
            InitializeComponent();
            Loaded += async (s, e) => InitializeData(); // Initialize when window is loaded
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

        private async void InitializeData()
        {
            await InitializeSupabaseAsync();

            employees = new ObservableCollection<Employee>();

            // Fetch data from Supabase
            var result = await supabase.From<Employee>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }

            dgEmployees.ItemsSource = employees;
        }

        private void btnGenerateID_Click(object sender, RoutedEventArgs e)
        {
            // Kunin ang current year
            string currentYear = DateTime.Now.Year.ToString();

            // Hanapin lahat ng existing employee IDs para sa kasalukuyang taon
            var yearEmployees = employees
                .Where(emp => emp.EmployeeID.StartsWith($"MBS-{currentYear}"))
                .ToList();

            // Kumuha ng last number
            int nextNumber = 1;
            if (yearEmployees.Any())
            {
                // I-extract ang numeric part (e.g., from MBS-2025-003 → 003)
                var lastId = yearEmployees
                    .Select(emp => emp.EmployeeID)
                    .OrderByDescending(id => id)
                    .FirstOrDefault();

                if (lastId != null)
                {
                    string[] parts = lastId.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }
            }

            // Format with leading zeros (3 digits)
            string employeeId = $"MBS-{currentYear}-{nextNumber:D3}";

            // Set to textboxes
            txtEmployeeID.Text = employeeId;
            txtAccEmployeeID.Text = employeeId;
        }


        private void dgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Load selected employee data into form fields
            if (dgEmployees.SelectedItem is Employee selectedEmployee)
            {
                txtFullName.Text = selectedEmployee.FullName;
                txtNickname.Text = selectedEmployee.Nickname;
                txtAddress.Text = selectedEmployee.Address;
                txtPhone.Text = selectedEmployee.Phone.ToString();
                txtEmail.Text = selectedEmployee.Email;
                txtEmergency.Text = selectedEmployee.EmergencyContact.ToString();
                txtEmployeeID.Text = selectedEmployee.EmployeeID;
                txtAccEmployeeID.Text = selectedEmployee.EmployeeID;

                // Set date pickers
                if (DateTime.TryParse(selectedEmployee.Birthday, out DateTime birthday))
                    dpBirthday.SelectedDate = birthday;

                if (DateTime.TryParse(selectedEmployee.DateHired, out DateTime dateHired))
                    dpDateHired.SelectedDate = dateHired;

                // Set combo boxes
                SetComboBoxValue(cbRole, selectedEmployee.Role);
                SetComboBoxValue(cbEmploymentStatus, selectedEmployee.EmploymentStatus);
                SetComboBoxValue(cbExpertise, selectedEmployee.Expertise);

                // Set duty schedule checkboxes
                SetDutySchedule(selectedEmployee.DutySchedule);
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) ||
                string.IsNullOrWhiteSpace(txtEmployeeID.Text))
            {
                MessageBox.Show("Please fill in all required fields (Full Name and Employee ID).",
                               "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (employees.Any(emp => emp.EmployeeID == txtEmployeeID.Text))
            {
                MessageBox.Show("Employee ID already exists. Please generate a new one.",
                               "Duplicate ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Employee newEmployee = CreateEmployeeFromForm();

            // Insert into Supabase
            var response = await supabase.From<Employee>().Insert(newEmployee);

            employees.Add(newEmployee);

            MessageBox.Show("Employee saved successfully!", "Success",
                           MessageBoxButton.OK, MessageBoxImage.Information);
            ClearForm();
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmployees.SelectedItem is Employee selectedEmployee)
            {
                try
                {
                    // I-update mismo ang selectedEmployee para hindi ka gumawa ng bagong instance
                    selectedEmployee.FullName = txtFullName.Text;
                    selectedEmployee.Nickname = txtNickname.Text;
                    selectedEmployee.Address = txtAddress.Text;
                    selectedEmployee.Phone = long.TryParse(txtPhone.Text, out long phone) ? phone : 0;
                    selectedEmployee.Email = txtEmail.Text;
                    selectedEmployee.EmergencyContact = long.TryParse(txtEmergency.Text, out long emergency) ? emergency : 0;
                    selectedEmployee.EmployeeID = txtEmployeeID.Text;
                    selectedEmployee.Birthday = dpBirthday.SelectedDate?.ToString("MM/dd/yyyy") ?? "";
                    selectedEmployee.Role = GetComboBoxValue(cbRole);
                    selectedEmployee.EmploymentStatus = GetComboBoxValue(cbEmploymentStatus);
                    selectedEmployee.Expertise = GetComboBoxValue(cbExpertise);
                    selectedEmployee.DateHired = dpDateHired.SelectedDate?.ToString("MM/dd/yyyy") ?? "";
                    selectedEmployee.DutySchedule = GetDutySchedule();

                    // Diretso update gamit ang PrimaryKey attribute
                    await supabase.From<Employee>().Update(selectedEmployee);

                    dgEmployees.Items.Refresh();

                    MessageBox.Show("Employee updated successfully!", "Success",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating employee: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an employee to update.", "No Selection",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmployees.SelectedItem is Employee selectedEmployee)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete {selectedEmployee.FullName}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Use the employee ID to identify which record to delete
                        await supabase.From<Employee>()
                            .Where(emp => emp.EmployeeID == selectedEmployee.EmployeeID)
                            .Delete();

                        employees.Remove(selectedEmployee);
                        ClearForm();
                        MessageBox.Show("Employee deleted successfully!", "Success",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting employee: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an employee to delete.", "No Selection",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Helper methods
        private Employee CreateEmployeeFromForm()
        {
            return new Employee
            {
                FullName = txtFullName.Text,
                Nickname = txtNickname.Text,
                Address = txtAddress.Text,
                Phone = long.TryParse(txtPhone.Text, out long phone) ? phone : 0,
                Email = txtEmail.Text,
                EmergencyContact = long.TryParse(txtEmergency.Text, out long emergency) ? emergency : 0,
                EmployeeID = txtEmployeeID.Text,
                Birthday = dpBirthday.SelectedDate?.ToString("MM/dd/yyyy") ?? "",
                Role = GetComboBoxValue(cbRole),
                EmploymentStatus = GetComboBoxValue(cbEmploymentStatus),
                Expertise = GetComboBoxValue(cbExpertise),
                DateHired = dpDateHired.SelectedDate?.ToString("MM/dd/yyyy") ?? "",
                DutySchedule = GetDutySchedule()
            };
        }


        private void ClearForm()
        {
            txtFullName.Clear();
            txtNickname.Clear();
            txtAddress.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtEmergency.Clear();
            txtEmployeeID.Clear();
            txtAccEmployeeID.Clear();
            txtPassword.Clear();

            dpBirthday.SelectedDate = null;
            dpDateHired.SelectedDate = null;

            cbRole.SelectedIndex = -1;
            cbEmploymentStatus.SelectedIndex = -1;
            cbExpertise.SelectedIndex = -1;

            chkMonday.IsChecked = false;
            chkTuesday.IsChecked = false;
            chkWednesday.IsChecked = false;
            chkThursday.IsChecked = false;
            chkFriday.IsChecked = false;
            chkSaturday.IsChecked = false;
            chkSunday.IsChecked = false;

            dgEmployees.SelectedItem = null;
        }

        private string GetComboBoxValue(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private string GetDutySchedule()
        {
            var days = new List<string>();
            if (chkMonday.IsChecked == true) days.Add("Monday");
            if (chkTuesday.IsChecked == true) days.Add("Tuesday");
            if (chkWednesday.IsChecked == true) days.Add("Wednesday");
            if (chkThursday.IsChecked == true) days.Add("Thursday");
            if (chkFriday.IsChecked == true) days.Add("Friday");
            if (chkSaturday.IsChecked == true) days.Add("Saturday");
            if (chkSunday.IsChecked == true) days.Add("Sunday");

            return string.Join(", ", days);
        }

        private void SetDutySchedule(string dutySchedule)
        {
            chkMonday.IsChecked = false;
            chkTuesday.IsChecked = false;
            chkWednesday.IsChecked = false;
            chkThursday.IsChecked = false;
            chkFriday.IsChecked = false;
            chkSaturday.IsChecked = false;
            chkSunday.IsChecked = false;

            if (string.IsNullOrWhiteSpace(dutySchedule)) return;

            if (dutySchedule.Contains("Monday")) chkMonday.IsChecked = true;
            if (dutySchedule.Contains("Tuesday")) chkTuesday.IsChecked = true;
            if (dutySchedule.Contains("Wednesday")) chkWednesday.IsChecked = true;
            if (dutySchedule.Contains("Thursday")) chkThursday.IsChecked = true;
            if (dutySchedule.Contains("Friday")) chkFriday.IsChecked = true;
            if (dutySchedule.Contains("Saturday")) chkSaturday.IsChecked = true;
            if (dutySchedule.Contains("Sunday")) chkSunday.IsChecked = true;
        }
    }

    // Employee model class
    [Table("Register_Employees")]
    public class Employee : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("Fname")]
        public string FullName { get; set; }

        [Column("Bname")]
        public string Nickname { get; set; }

        [Column("Address")]
        public string Address { get; set; }

        [Column("Bday")]
        public string Birthday { get; set; }

        [Column("PNumber")]
        public long Phone { get; set; }

        [Column("Email")]
        public string Email { get; set; }

        [Column("Econtact")]
        public long EmergencyContact { get; set; }

        [Column("Eid")]
        public string EmployeeID { get; set; }

        [Column("Role")]
        public string Role { get; set; }

        [Column("Expert")]
        public string Expertise { get; set; }

        [Column("Estatus")]
        public string EmploymentStatus { get; set; }

        [Column("Dsched")]
        public string DutySchedule { get; set; }

        [Column("Dhired")]
        public string DateHired { get; set; }
    }

}
