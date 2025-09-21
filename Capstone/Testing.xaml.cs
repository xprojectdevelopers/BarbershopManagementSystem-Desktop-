using Microsoft.Win32;
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
using System.Windows.Media.Imaging;

namespace Capstone
{
    public partial class Testing : Window
    {
        private Client supabase;
        private ObservableCollection<Employee> employees;

        public Testing()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData(); // Initialize when window is loaded
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

            // Fetch data from Supabase
            var result = await supabase.From<Employee>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
            }
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            EMenu EMenu = new EMenu();
            EMenu.Show();
            this.Close();
        }

        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.EndInit();

                    PhotoPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load image: " + ex.Message);
                }
            }
        }

        private void btnGenerateID_Click(object sender, RoutedEventArgs e)
        {
            string currentYear = DateTime.Now.Year.ToString();

            var yearEmployees = employees
                .Where(emp => emp.Eid.StartsWith($"MBS-{currentYear}"))
                .ToList();

            int nextNumber = 1;
            if (yearEmployees.Any())
            {
                var lastId = yearEmployees
                    .Select(emp => emp.Eid)
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

            string employeeId = $"MBS-{currentYear}-{nextNumber:D3}";
            txtEmployeeID.Text = employeeId;
        }

        private void btnGeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            string password = GenerateRandomPassword(8);
            txtEmployeePassword.Text = password;
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            Random random = new Random();

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void RoleSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem is ComboBoxItem selectedRole)
            {
                string role = selectedRole.Content.ToString();

                if (role == "Cashier")
                {
                    btnGeneratePassword.IsEnabled = true;
                    txtEmployeePassword.IsEnabled = true;

                    cmbBarberExpertise.IsEnabled = false;
                    servicesPanel.IsEnabled = false;
                }
                else if (role == "Barber")
                {
                    btnGeneratePassword.IsEnabled = false;
                    txtEmployeePassword.IsEnabled = false;

                    cmbBarberExpertise.IsEnabled = true;
                    servicesPanel.IsEnabled = true;
                }
            }
        }

        // Inline Validation Methods
        private void ClearAllValidationErrors()
        {
            // Clear all error TextBlocks visibility
            txtFullNameError.Visibility = Visibility.Collapsed;
            txtBdateError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtAddressError.Visibility = Visibility.Collapsed;
            txtContactNumberError.Visibility = Visibility.Collapsed;
            txtEmailError.Visibility = Visibility.Collapsed;
            txtEmergencyNameError.Visibility = Visibility.Collapsed;
            txtEmergencyNumberError.Visibility = Visibility.Collapsed;
            txtEmployeeIDError.Visibility = Visibility.Collapsed;
            txtRoleError.Visibility = Visibility.Collapsed;
            txtPasswordError.Visibility = Visibility.Collapsed;
            txtNicknameError.Visibility = Visibility.Collapsed;
            txtBarberExpertiseError.Visibility = Visibility.Collapsed;
            txtServicesError.Visibility = Visibility.Collapsed;
            txtDateHiredError.Visibility = Visibility.Collapsed;
            txtEmploymentStatusError.Visibility = Visibility.Collapsed;
            txtWorkScheduleError.Visibility = Visibility.Collapsed;
        }

        private void ShowValidationError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        
        private bool ValidateAllRequiredFieldsInline(Employee newEmployee)
        {
            bool isValid = true;

            // Clear all previous errors
            ClearAllValidationErrors();

            // Check all required fields marked with * 
            if (string.IsNullOrWhiteSpace(newEmployee.Fname))
            {
                ShowValidationError(txtFullNameError, "Full Name is required");
                isValid = false;
            }

            if (!newEmployee.Bdate.HasValue)
            {
                ShowValidationError(txtBdateError, "Birthdate is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Gender))
            {
                ShowValidationError(txtGenderError, "Gender is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Address))
            {
                ShowValidationError(txtAddressError, "Address is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Cnumber))
            {
                ShowValidationError(txtContactNumberError, "Contact Number is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Email))
            {
                ShowValidationError(txtEmailError, "Email Address is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.ECname))
            {
                ShowValidationError(txtEmergencyNameError, "Emergency Contact Name is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.ECnumber))
            {
                ShowValidationError(txtEmergencyNumberError, "Emergency Contact Number is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Eid))
            {
                ShowValidationError(txtEmployeeIDError, "Employee ID is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Role))
            {
                ShowValidationError(txtRoleError, "Employee Role is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Nickname))
            {
                ShowValidationError(txtNicknameError, "Employee Nickname is required");
                isValid = false;
            }

            // Role Choosing
            if (!string.IsNullOrWhiteSpace(newEmployee.Role))
            {
                if (newEmployee.Role == "Cashier")
                {
                    // For Cashier role, password is required
                    if (string.IsNullOrWhiteSpace(newEmployee.Epassword))
                    {
                        ShowValidationError(txtPasswordError, "Employee Password is required for Cashier role");
                        isValid = false;
                    }
                }
                else if (newEmployee.Role == "Barber")
                {
                    // For Barber role, barber expertise is required
                    if (string.IsNullOrWhiteSpace(newEmployee.BarberExpertise))
                    {
                        ShowValidationError(txtBarberExpertiseError, "Barber Expertise is required for Barber role");
                        isValid = false;
                    }

                    
                    if (string.IsNullOrWhiteSpace(newEmployee.ServicesOffered))
                    {
                        ShowValidationError(txtServicesError, "At least one service must be selected for Barber role");
                        isValid = false;
                    }
                }
            }

            if (!newEmployee.DateHired.HasValue)
            {
                ShowValidationError(txtDateHiredError, "Date Hired is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Estatus))
            {
                ShowValidationError(txtEmploymentStatusError, "Employment Status is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Wsched))
            {
                ShowValidationError(txtWorkScheduleError, "At least one work day must be selected");
                isValid = false;
            }

            return isValid;
        }

        
        private bool ValidateEmployeeInline(Employee newEmployee)
        {
            bool isValid = true;

            if (employees.Any(emp => emp.Fname == newEmployee.Fname))
            {
                ShowValidationError(txtFullNameError, "Full name already exists");
                isValid = false;
            }

            if (employees.Any(emp => emp.Eid == newEmployee.Eid))
            {
                ShowValidationError(txtEmployeeIDError, "Employee ID must be unique");
                isValid = false;
            }

   
            if (!string.IsNullOrWhiteSpace(newEmployee.Epassword) &&
                employees.Any(emp => emp.Epassword == newEmployee.Epassword))
            {
                ShowValidationError(txtPasswordError, "Password already in use");
                isValid = false;
            }

            if (employees.Any(emp => emp.Nickname == newEmployee.Nickname))
            {
                ShowValidationError(txtNicknameError, "Nickname already taken");
                isValid = false;
            }

            return isValid;
        }

        private async void SaveEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear validation errors first
                ClearAllValidationErrors();

                
                var newEmployee = new Employee
                {
                    Fname = txtFullName.Text.Trim(),
                    Eid = txtEmployeeID.Text.Trim(),
                    Nickname = txtNickname.Text.Trim(),
                    Role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString(),

                    
                    Bdate = bdate.SelectedDate,
                    Gender = GetSelectedComboBoxValue(Gender),
                    Address = txtAddress.Text.Trim(),
                    Cnumber = string.IsNullOrWhiteSpace(txtContactNumber.Text.Trim()) ? null : txtContactNumber.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    ECname = txtEmergencyName.Text.Trim(),
                    ECnumber = string.IsNullOrWhiteSpace(txtEmergencyNumber.Text.Trim()) ? null : txtEmergencyNumber.Text.Trim(),
                    DateHired = dateHiredPicker.SelectedDate,
                    Estatus = GetSelectedComboBoxValue(cmbEmploymentStatus),
                    Wsched = GetSelectedWorkSchedule()
                };

                
                if (newEmployee.Role == "Cashier")
                {
                    newEmployee.Epassword = txtEmployeePassword.Text.Trim();
                    newEmployee.BarberExpertise = null;
                    newEmployee.ServicesOffered = null;
                }
                else if (newEmployee.Role == "Barber")
                {
                    newEmployee.BarberExpertise = GetSelectedComboBoxValue(cmbBarberExpertise);
                    newEmployee.Epassword = null;
                    newEmployee.ServicesOffered = GetSelectedServices();
                }

                // Validate with inline error display
                bool isUniqueValid = ValidateEmployeeInline(newEmployee);
                bool isRequiredValid = ValidateAllRequiredFieldsInline(newEmployee);

                if (!isUniqueValid || !isRequiredValid)
                    return; 

                // Save to Supabase database
                var result = await supabase.From<Employee>().Insert(newEmployee);

                if (result != null)
                {
                    
                    employees.Add(newEmployee);

                    MessageBox.Show("Employee added successfully to database!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Clear the form after successful save
                    Clear_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Failed to save employee to database.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving employee: {ex.Message}", "Database Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
        private string GetSelectedComboBoxValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.IsEnabled)
            {
                return selectedItem.Content.ToString();
            }
            return null;
        }

        
        private string GetSelectedServices()
        {
            var selectedServices = new List<string>();

            foreach (var child in servicesPanel.Children)
            {
                if (child is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    selectedServices.Add(checkBox.Content.ToString());
                }
            }

            return selectedServices.Count > 0 ? string.Join(", ", selectedServices) : null;
        }

        
        private string GetSelectedWorkSchedule()
        {
            var selectedDays = new List<string>();

            foreach (var child in workSchedulePanel.Children)
            {
                if (child is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    selectedDays.Add(checkBox.Content.ToString());
                }
            }

            return selectedDays.Count > 0 ? string.Join(", ", selectedDays) : null;
        }

        
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            // Clear all validation errors first
            ClearAllValidationErrors();

            // ===== TextBoxes =====
            txtFullName.Text = string.Empty;
            txtEmployeeID.Text = string.Empty;
            txtEmployeePassword.Text = string.Empty;
            txtNickname.Text = string.Empty;
            txtAddress.Text = string.Empty;
            txtContactNumber.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtEmergencyName.Text = string.Empty;
            txtEmergencyNumber.Text = string.Empty;

            // ===== DatePickers =====
            bdate.SelectedDate = null;
            dateHiredPicker.SelectedDate = null;

            // ===== ComboBoxes =====
            Gender.SelectedIndex = -1;
            cmbRole.SelectedIndex = -1;
            cmbBarberExpertise.SelectedIndex = -1;
            cmbEmploymentStatus.SelectedIndex = -1;

            // ===== Image =====
            PhotoPreview.Source = new BitmapImage(new Uri("/profile.png", UriKind.Relative));

            // ===== Services offered =====
            foreach (var child in servicesPanel.Children)
                if (child is CheckBox cb) cb.IsChecked = false;

            // ===== Work schedule =====
            foreach (var child in workSchedulePanel.Children)
                if (child is CheckBox cb) cb.IsChecked = false;
        }

        [Table("Register_Employees")] // pangalan ng table sa Supabase
        public class Employee : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Fname")]
            public string Fname { get; set; }

            [Column("Bdate")]
            public DateTime? Bdate { get; set; }

            [Column("Gender")]
            public string Gender { get; set; }

            [Column("Address")]
            public string Address { get; set; }

            [Column("Cnumber")]
            public string Cnumber { get; set; }

            [Column("Email")]
            public string Email { get; set; }

            [Column("ECname")]
            public string ECname { get; set; }

            [Column("ECnumber")]
            public string ECnumber { get; set; }

            [Column("Eid")]
            public string Eid { get; set; }

            [Column("Erole")]
            public string Role { get; set; }

            [Column("Epassword")]
            public string Epassword { get; set; }

            [Column("Enickname")]
            public string Nickname { get; set; }

            [Column("Bexpert")]
            public string BarberExpertise { get; set; }

            [Column("Soffered")]
            public string ServicesOffered { get; set; }

            [Column("Dhired")]
            public DateTime? DateHired { get; set; }

            [Column("Estatus")]
            public string Estatus { get; set; }

            [Column("Wsched")]
            public string Wsched { get; set; }
        }
    }
}