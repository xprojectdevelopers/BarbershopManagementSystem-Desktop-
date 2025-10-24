using Capstone.CustomControls;
using Microsoft.Win32;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Capstone
{

    public partial class AddEmployee : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private string selectedPhotoPath = string.Empty;
        private string photoBase64 = string.Empty;
        private bool isSaving = false;
        private Window currentModalWindow;

        public AddEmployee()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();

            // Prevent leading zero removal
            txtContactNumber.PreviewKeyDown += PhoneNumber_PreviewKeyDown;
            txtEmergencyNumber.PreviewKeyDown += PhoneNumber_PreviewKeyDown;
        }

        // Add this method to handle phone number input
        private void PhoneNumber_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Allow only numbers, backspace, delete, and navigation keys
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) &&
                !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) &&
                e.Key != Key.Back && e.Key != Key.Delete &&
                e.Key != Key.Left && e.Key != Key.Right &&
                e.Key != Key.Tab)
            {
                e.Handled = true;
            }
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

            // Fetch data from Supabase
            var result = await supabase.From<BarbershopManagementSystem>().Get();
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
            openFileDialog.Title = "Select Employee Photo";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Validate file size (limit to 5MB)
                    FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024) // 5MB limit
                    {
                        MessageBox.Show("Photo size must be less than 5MB.", "File Too Large",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Load and display image
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.EndInit();

                    PhotoPreview.Source = bitmap;
                    selectedPhotoPath = openFileDialog.FileName;

                    // Convert to Base64 for database storage
                    photoBase64 = ConvertImageToBase64(openFileDialog.FileName);

                    // Clear any photo error messages
                    txtPhotoError.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load image: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);

                    // Reset photo if failed
                    PhotoPreview.Source = new BitmapImage(new Uri("/profile.png", UriKind.Relative));
                    selectedPhotoPath = string.Empty;
                    photoBase64 = string.Empty;
                }
            }
        }

        private string ConvertImageToBase64(string imagePath)
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting image: {ex.Message}", "Conversion Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        private void btnGenerateID_Click(object sender, RoutedEventArgs e)
        {
            string currentYear = DateTime.Now.Year.ToString();

            var yearEmployees = employees
                .Where(emp => emp.Eid.StartsWith($"MSB-{currentYear}"))
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

            string employeeId = $"MSB-{currentYear}-{nextNumber:D4}";
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
                    // Enable password controls
                    btnGeneratePassword.IsEnabled = true;
                    txtEmployeePassword.IsEnabled = true;

                    // Reset password control colors to normal
                    btnGeneratePassword.Foreground = Brushes.Blue;
                    txtEmployeePassword.Background = Brushes.White;
                    txtEmployeePassword.Foreground = Brushes.Black;
                    cmbBarberExpertise.SelectedIndex = -1;

                    // Find and enable the password label
                    var passwordLabel = FindVisualChild<Label>(this, "lblEmployeePassword");
                    if (passwordLabel != null)
                    {
                        passwordLabel.Foreground = Brushes.Black;
                        passwordLabel.IsEnabled = true;
                    }

                    // Disable barber-related controls
                    cmbBarberExpertise.IsEnabled = false;

                    // Gray out barber controls
                    cmbBarberExpertise.Foreground = Brushes.Gray;

                    // Find and gray out barber expertise label
                    var expertiseLabel = FindVisualChild<Label>(this, "lblBarberExpertise");
                    if (expertiseLabel != null)
                    {
                        expertiseLabel.Foreground = Brushes.Gray;
                        expertiseLabel.IsEnabled = false;
                    }

                    // Find and gray out services label
                    var servicesLabel = FindVisualChild<Label>(this, "lblServicesOffered");
                    if (servicesLabel != null)
                    {
                        servicesLabel.Foreground = Brushes.Gray;
                        servicesLabel.IsEnabled = false;
                    }

                }
                else if (role == "Barber")
                {
                    // Disable password controls
                    btnGeneratePassword.IsEnabled = false;
                    txtEmployeePassword.IsEnabled = false;

                    // Gray out password controls
                    btnGeneratePassword.Foreground = Brushes.Gray;
                    txtEmployeePassword.Background = Brushes.LightGray;
                    txtEmployeePassword.Foreground = Brushes.Gray;
                    txtEmployeePassword.Text = string.Empty; // Clear password when switching to Barber

                    // Find and gray out the password label
                    var passwordLabel = FindVisualChild<Label>(this, "lblEmployeePassword");
                    if (passwordLabel != null)
                    {
                        passwordLabel.Foreground = Brushes.Gray;
                        passwordLabel.IsEnabled = false;
                    }

                    // Enable barber-related controls
                    cmbBarberExpertise.IsEnabled = true;

                    // Reset barber control colors to normal
                    cmbBarberExpertise.Foreground = Brushes.Black;

                    // Find and enable barber expertise label
                    var expertiseLabel = FindVisualChild<Label>(this, "lblBarberExpertise");
                    if (expertiseLabel != null)
                    {
                        expertiseLabel.Foreground = Brushes.Black;
                        expertiseLabel.IsEnabled = true;
                    }

                    // Find and enable services label
                    var servicesLabel = FindVisualChild<Label>(this, "lblServicesOffered");
                    if (servicesLabel != null)
                    {
                        servicesLabel.Foreground = Brushes.Black;
                        servicesLabel.IsEnabled = true;
                    }
                }
            }
            else
            {
                // Disable password controls
                btnGeneratePassword.IsEnabled = false;
                txtEmployeePassword.IsEnabled = false;
                btnGeneratePassword.Foreground = Brushes.Gray;
                txtEmployeePassword.Background = Brushes.LightGray;
                txtEmployeePassword.Foreground = Brushes.Gray;

                // Disable barber controls
                cmbBarberExpertise.IsEnabled = false;
                cmbBarberExpertise.Foreground = Brushes.Gray;

            }
        }


        private static T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child != null && child is T && ((FrameworkElement)child).Name == childName)
                    {
                        return (T)child;
                    }

                    var childOfChild = FindVisualChild<T>(child, childName);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        // Clear all validation errors
        private void ClearAllValidationErrors()
        {
            // Clear all error TextBlocks visibility
            txtFullNameError.Visibility = Visibility.Collapsed;
            txtFullNameTaken.Visibility = Visibility.Collapsed;
            txtBdateError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtAddressError.Visibility = Visibility.Collapsed;
            txtContactNumberError.Visibility = Visibility.Collapsed;
            txtEmailError.Visibility = Visibility.Collapsed;
            txtEmergencyNameError.Visibility = Visibility.Collapsed;
            txtEmergencyNumberError.Visibility = Visibility.Collapsed;
            txtEmployeeIDError.Visibility = Visibility.Collapsed;
            txtEmployeeIDSame.Visibility = Visibility.Collapsed;
            txtRoleError.Visibility = Visibility.Collapsed;
            txtPasswordError.Visibility = Visibility.Collapsed;
            txtPasswordSame.Visibility = Visibility.Collapsed;
            txtNicknameError.Visibility = Visibility.Collapsed;
            txtNicknameTaken.Visibility = Visibility.Collapsed;
            txtBarberExpertiseError.Visibility = Visibility.Collapsed;
            txtDateHiredError.Visibility = Visibility.Collapsed;
            txtEmploymentStatusError.Visibility = Visibility.Collapsed;
            txtWorkScheduleError.Visibility = Visibility.Collapsed;
            txtPhotoError.Visibility = Visibility.Collapsed;
        }

        private void ShowValidationError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private bool ValidateAllRequiredFieldsInline(BarbershopManagementSystem newEmployee)
        {
            bool isValid = true;

            // Clear all previous errors
            ClearAllValidationErrors();

            // Photo validation - required field
            if (string.IsNullOrWhiteSpace(photoBase64))
            {
                ShowValidationError(txtPhotoError, "Employee photo is required");
                isValid = false;
            }

            // Check all other required fields marked with * 
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

            // Enhanced Contact Number Validation
            if (string.IsNullOrWhiteSpace(newEmployee.Cnumber))
            {
                ShowValidationError(txtContactNumberError, "Contact Number is required");
                isValid = false;
            }
            else
            {
                // Remove any whitespace
                string cleanNumber = newEmployee.Cnumber.Trim();

                // Check if it contains only digits
                if (!Regex.IsMatch(cleanNumber, @"^[0-9]+$"))
                {
                    ShowValidationError(txtContactNumberError, "No special character and alphabet");
                    isValid = false;
                }
                // Check if it's exactly 11 digits
                else if (cleanNumber.Length != 11)
                {
                    ShowValidationError(txtContactNumberError, "Contact Number must be 11 digits only");
                    isValid = false;
                }
                // Check if it starts with 0
                else if (!cleanNumber.StartsWith("0"))
                {
                    ShowValidationError(txtContactNumberError, "Contact Number must start with 0");
                    isValid = false;
                }
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Email))
            {
                ShowValidationError(txtEmailError, "Email Address is required");
                isValid = false;
            }
            else if (!Regex.IsMatch(newEmployee.Email, @"^[a-zA-Z0-9._%+-]+@gmail\.com$"))
            {
                ShowValidationError(txtEmailError, "Email must be a valid @gmail.com address");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.ECname))
            {
                ShowValidationError(txtEmergencyNameError, "Emergency Contact Name is required");
                isValid = false;
            }

            // Enhanced Emergency Contact Number Validation
            if (string.IsNullOrWhiteSpace(newEmployee.ECnumber))
            {
                ShowValidationError(txtEmergencyNumberError, "Emergency Contact Number is required");
                isValid = false;
            }
            else
            {
                // Remove any whitespace
                string cleanNumber = newEmployee.ECnumber.Trim();

                // Check if it contains only digits
                if (!Regex.IsMatch(cleanNumber, @"^[0-9]+$"))
                {
                    ShowValidationError(txtEmergencyNumberError, "No special character and alphabet");
                    isValid = false;
                }
                // Check if it's exactly 11 digits
                else if (cleanNumber.Length != 11)
                {
                    ShowValidationError(txtEmergencyNumberError, "Emergency Contact Number must be 11 digits only");
                    isValid = false;
                }
                // Check if it starts with 0
                else if (!cleanNumber.StartsWith("0"))
                {
                    ShowValidationError(txtEmergencyNumberError, "Emergency Contact Number must start with 0");
                    isValid = false;
                }
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

            // Role-specific validations
            if (newEmployee.Role == "Cashier")
            {
                if (string.IsNullOrWhiteSpace(newEmployee.Epassword))
                {
                    ShowValidationError(txtPasswordError, "Employee Password is required for Cashier role");
                    isValid = false;
                }
            }
            else if (newEmployee.Role == "Barber")
            {
                if (string.IsNullOrWhiteSpace(newEmployee.BarberExpertise))
                {
                    ShowValidationError(txtBarberExpertiseError, "Barber Expertise is required for Barber role");
                    isValid = false;

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

        private bool ValidateEmployeeInline(BarbershopManagementSystem newEmployee)
        {
            bool isValid = true;

            // Clear previous errors for uniqueness checks only
            txtFullNameTaken.Visibility = Visibility.Collapsed;
            txtEmployeeIDSame.Visibility = Visibility.Collapsed;
            txtPasswordSame.Visibility = Visibility.Collapsed;
            txtNicknameTaken.Visibility = Visibility.Collapsed;

            // Validate Full Name uniqueness
            if (!string.IsNullOrWhiteSpace(newEmployee.Fname) &&
                employees.Any(emp => emp.Fname.Equals(newEmployee.Fname, StringComparison.OrdinalIgnoreCase)))
            {
                ShowValidationError(txtFullNameTaken, "Full name already exists. Please enter a different one.");
                isValid = false;
            }

            // Validate Employee ID uniqueness
            if (!string.IsNullOrWhiteSpace(newEmployee.Eid) &&
                employees.Any(emp => emp.Eid == newEmployee.Eid))
            {
                ShowValidationError(txtEmployeeIDSame, "Employee ID must be unique.");
                isValid = false;
            }

            // Validate Password uniqueness (only if password is provided and role is Cashier)
            if (newEmployee.Role == "Cashier" && !string.IsNullOrWhiteSpace(newEmployee.Epassword) &&
                employees.Any(emp => emp.Epassword == newEmployee.Epassword))
            {
                ShowValidationError(txtPasswordSame, "Password already in use. Please choose another.");
                isValid = false;
            }

            // Validate Nickname uniqueness
            if (!string.IsNullOrWhiteSpace(newEmployee.Nickname) &&
                employees.Any(emp => emp.Nickname.Equals(newEmployee.Nickname, StringComparison.OrdinalIgnoreCase)))
            {
                ShowValidationError(txtNicknameTaken, "Employee nickname is already taken.");
                isValid = false;
            }

            return isValid;
        }

        private void ClearRequiredFieldValidationErrors()
        {
            // Clear required field error TextBlocks visibility
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
            txtDateHiredError.Visibility = Visibility.Collapsed;
            txtEmploymentStatusError.Visibility = Visibility.Collapsed;
            txtWorkScheduleError.Visibility = Visibility.Collapsed;
            txtPhotoError.Visibility = Visibility.Collapsed;
        }

        private async void SaveEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Prevent double-clicking
            if (isSaving)
                return;

            try
            {
                // Set saving flag and disable button
                isSaving = true;
                Button saveButton = (Button)sender;
                saveButton.IsEnabled = false;

                // Only clear required field validation errors, not uniqueness errors
                ClearRequiredFieldValidationErrors();

                var newEmployee = new BarbershopManagementSystem
                {
                    Fname = txtFullName.Text.Trim(),
                    Eid = txtEmployeeID.Text.Trim(),
                    Nickname = txtNickname.Text.Trim(),
                    Role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    Photo = photoBase64,

                    Bdate = bdate.SelectedDate,
                    Gender = GetSelectedComboBoxValue(Gender),
                    Address = txtAddress.Text.Trim(),
                    // Keep the leading zero by storing as string - trim but preserve the zero
                    Cnumber = string.IsNullOrWhiteSpace(txtContactNumber.Text) ? null : txtContactNumber.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    ECname = txtEmergencyName.Text.Trim(),
                    // Keep the leading zero by storing as string - trim but preserve the zero
                    ECnumber = string.IsNullOrWhiteSpace(txtEmergencyNumber.Text) ? null : txtEmergencyNumber.Text.Trim(),
                    DateHired = dateHiredPicker.SelectedDate,
                    Estatus = GetSelectedComboBoxValue(cmbEmploymentStatus),
                    Wsched = GetSelectedWorkSchedule()
                };

                if (newEmployee.Role == "Cashier")
                {
                    newEmployee.Epassword = txtEmployeePassword.Text.Trim();
                    newEmployee.BarberExpertise = null;
                }
                else if (newEmployee.Role == "Barber")
                {
                    newEmployee.BarberExpertise = GetSelectedComboBoxValue(cmbBarberExpertise);
                    newEmployee.Epassword = null;
                }

                // Validate required fields first
                bool isRequiredValid = ValidateAllRequiredFieldsInline(newEmployee);

                // Then validate uniqueness (this will clear and show uniqueness errors)
                bool isUniqueValid = ValidateEmployeeInline(newEmployee);

                if (!isUniqueValid || !isRequiredValid)
                {
                    // Re-enable button if validation fails
                    saveButton.IsEnabled = true;
                    saveButton.Content = "Add Employee";
                    isSaving = false;
                    return;
                }

                // Debug: Check what's actually being saved
                System.Diagnostics.Debug.WriteLine($"Contact Number being saved: '{newEmployee.Cnumber}'");
                System.Diagnostics.Debug.WriteLine($"Emergency Number being saved: '{newEmployee.ECnumber}'");
                System.Diagnostics.Debug.WriteLine($"Contact Number Length: {newEmployee.Cnumber?.Length}");
                System.Diagnostics.Debug.WriteLine($"Emergency Number Length: {newEmployee.ECnumber?.Length}");

                // Save to Supabase database
                var result = await supabase.From<BarbershopManagementSystem>().Insert(newEmployee);

                if (result != null && result.Models.Count > 0)
                {
                    // Add to local collection only once
                    employees.Add(result.Models[0]);

                    // Show success window only once
                    // Show the overlay FIRST
                    ModalOverlay.Visibility = Visibility.Visible;

                    // Open PurchaseOrders as a regular window
                    currentModalWindow = new succesfull();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Subscribe to Closed event
                    currentModalWindow.Closed += ModalWindow_Closed;

                    // Show as regular window
                    currentModalWindow.Show();

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
            finally
            {
                // Always re-enable button and reset flag
                isSaving = false;
                Button saveButton = (Button)sender;
                saveButton.IsEnabled = true;
                saveButton.Content = "Add Employee";
            }
        }

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private string GetSelectedComboBoxValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.IsEnabled)
            {
                return selectedItem.Content.ToString();
            }
            return null;
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
            PhotoPreview.Source = new BitmapImage(new Uri("/Icon/profile.png", UriKind.Relative));
            selectedPhotoPath = string.Empty;
            photoBase64 = string.Empty;



            // ===== Work schedule =====
            foreach (var child in workSchedulePanel.Children)
                if (child is CheckBox cb) cb.IsChecked = false;
        }

        [Table("Add_Employee")] // pangalan ng table sa Supabase
        public class BarbershopManagementSystem : BaseModel
        {
            [PrimaryKey("id", false)]
            public int Id { get; set; }

            [Column("Full_Name")]
            public string Fname { get; set; }

            [Column("Birthdate")]
            public DateTime? Bdate { get; set; }

            [Column("Gender")]
            public string Gender { get; set; }

            [Column("Address")]
            public string Address { get; set; }

            [Column("Contact_Number")]
            public string Cnumber { get; set; }

            [Column("Email")]
            public string Email { get; set; }

            [Column("EContact_Name")]
            public string ECname { get; set; }

            [Column("EContact_Number")]
            public string ECnumber { get; set; }

            [Column("Employee_ID")]
            public string Eid { get; set; }

            [Column("Employee_Role")]
            public string Role { get; set; }

            [Column("Employee_Password")]
            public string Epassword { get; set; }

            [Column("Employee_Nickname")]
            public string Nickname { get; set; }

            [Column("Barber_Expert")]
            public string BarberExpertise { get; set; }

            [Column("Date_Hired")]
            public DateTime? DateHired { get; set; }

            [Column("Employee_Status")]
            public string Estatus { get; set; }

            [Column("Work_Sched")]
            public string Wsched { get; set; }

            [Column("Photo")]
            public string Photo { get; set; } // Base64 string
        }
    }
}