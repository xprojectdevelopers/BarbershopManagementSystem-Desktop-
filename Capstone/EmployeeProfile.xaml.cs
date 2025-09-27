using Microsoft.Win32;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Capstone
{
    public partial class EmployeeProfile : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;
        private string currentPhotoPath; // Add this field to track current photo path
        private bool isPhotoChanged = false; // Add this field to track if photo was changed

        public EmployeeProfile()
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

                    // Store the new photo path and mark as changed
                    currentPhotoPath = openFileDialog.FileName;
                    isPhotoChanged = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load image: " + ex.Message);
                }
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            string employeeId = txtEmployeeID.Text.Trim();

            if (string.IsNullOrEmpty(employeeId))
            {
                MessageBox.Show("Please enter an Employee ID to search.", "Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                }
                else
                {
                    MessageBox.Show($"No employee found with ID: {employeeId}", "Employee Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
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
            txtFullName.Text = employee.Fname ?? "";
            bdate.SelectedDate = employee.Bdate;

            SetComboBoxSelection(Gender, employee.Gender);

            txtAddress.Text = employee.Address ?? "";
            txtContactNumber.Text = employee.Cnumber ?? "";
            txtEmail.Text = employee.Email ?? "";
            txtEmergencyName.Text = employee.ECname ?? "";
            txtEmergencyNumber.Text = employee.ECnumber ?? "";

            // Employment Information
            SetComboBoxSelection(cmbRole, employee.Role);
            txtEmployeePassword.Text = employee.Epassword ?? "";
            txtNickname.Text = employee.Nickname ?? "";

            SetComboBoxSelection(cmbBarberExpertise, employee.BarberExpertise);
            SetCheckBoxes(servicesPanel, employee.ServicesOffered);

            dateHiredPicker.SelectedDate = employee.DateHired;
            SetComboBoxSelection(cmbEmploymentStatus, employee.Estatus);
            SetCheckBoxes(workSchedulePanel, employee.Wsched);

            // Load Photo and set current photo path
            currentPhotoPath = employee.PhotoPath;
            isPhotoChanged = false; // Reset photo changed flag
            LoadEmployeePhoto(employee.PhotoPath);
        }

        private void LoadEmployeePhoto(string photoPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(photoPath))
                {
                    if (System.IO.File.Exists(photoPath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(photoPath);
                        bitmap.EndInit();
                        PhotoPreview.Source = bitmap;
                    }
                    else if (Uri.IsWellFormedUriString(photoPath, UriKind.Absolute))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(photoPath);
                        bitmap.EndInit();
                        PhotoPreview.Source = bitmap;
                    }
                    else if (photoPath.StartsWith("data:image") || IsBase64String(photoPath))
                    {
                        LoadFromBase64(photoPath);
                    }
                    else
                    {
                        PhotoPreview.Source = new BitmapImage(new Uri("/profile.png", UriKind.Relative));
                    }
                }
                else
                {
                    PhotoPreview.Source = new BitmapImage(new Uri("/profile.png", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                PhotoPreview.Source = new BitmapImage(new Uri("/profile.png", UriKind.Relative));
                MessageBox.Show($"Error loading photo: {ex.Message}", "Photo Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadFromBase64(string base64String)
        {
            try
            {
                if (base64String.Contains(","))
                {
                    base64String = base64String.Split(',')[1];
                }

                byte[] imageBytes = Convert.FromBase64String(base64String);
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(imageBytes);
                bitmap.EndInit();
                PhotoPreview.Source = bitmap;
            }
            catch
            {
                PhotoPreview.Source = new BitmapImage(new Uri("/profile.png", UriKind.Relative));
            }
        }

        private bool IsBase64String(string s)
        {
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
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

        private void SetComboBoxSelection(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SetCheckBoxes(UniformGrid panel, string values)
        {
            if (string.IsNullOrEmpty(values)) return;

            var selectedValues = values.Split(',').Select(v => v.Trim()).ToList();

            foreach (var child in panel.Children)
            {
                if (child is CheckBox checkBox)
                {
                    checkBox.IsChecked = selectedValues.Contains(checkBox.Content.ToString(), StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        private void ClearForm()
        {
            txtFullName.Clear();
            bdate.SelectedDate = null;
            Gender.SelectedIndex = -1;
            txtAddress.Clear();
            txtContactNumber.Clear();
            txtEmail.Clear();
            txtEmergencyName.Clear();
            txtEmergencyNumber.Clear();

            cmbRole.SelectedIndex = -1;
            txtEmployeePassword.Clear();
            txtNickname.Clear();
            cmbBarberExpertise.SelectedIndex = -1;
            dateHiredPicker.SelectedDate = null;
            cmbEmploymentStatus.SelectedIndex = -1;

            ClearCheckBoxes(servicesPanel);
            ClearCheckBoxes(workSchedulePanel);

            PhotoPreview.Source = new BitmapImage(new Uri("/profile.png", UriKind.Relative));
            currentPhotoPath = null;
            isPhotoChanged = false;
        }

        private void ClearCheckBoxes(UniformGrid panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is CheckBox checkBox)
                {
                    checkBox.IsChecked = false;
                }
            }
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

                    // Find and enable the password label
                    var passwordLabel = FindVisualChild<Label>(this, "lblEmployeePassword");
                    if (passwordLabel != null)
                    {
                        passwordLabel.Foreground = Brushes.Black;
                        passwordLabel.IsEnabled = true;
                    }

                    // Disable barber-related controls
                    cmbBarberExpertise.IsEnabled = false;
                    servicesPanel.IsEnabled = false;

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

                    cmbBarberExpertise.SelectedIndex = -1;
                    foreach (var child in servicesPanel.Children)
                        if (child is CheckBox cb) cb.IsChecked = false;

                    // Gray out all checkboxes in services panel
                    foreach (var child in servicesPanel.Children)
                    {
                        if (child is CheckBox checkBox)
                        {
                            checkBox.Foreground = Brushes.Gray;
                            checkBox.IsEnabled = false;

                            
                        }
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
                    servicesPanel.IsEnabled = true;

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

                    // Enable all checkboxes in services panel
                    foreach (var child in servicesPanel.Children)
                    {
                        if (child is CheckBox checkBox)
                        {
                            checkBox.Foreground = Brushes.Black;
                            checkBox.IsEnabled = true;
                        }
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
                servicesPanel.IsEnabled = false;
                cmbBarberExpertise.Foreground = Brushes.Gray;


                txtEmployeePassword.Text = string.Empty;

                // Gray out all checkboxes
                foreach (var child in servicesPanel.Children)
                {
                    if (child is CheckBox checkBox)
                    {
                        checkBox.Foreground = Brushes.Gray;
                        checkBox.IsEnabled = false;

                    }
                }
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


        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            string employeeId = txtEmployeeID.Text.Trim();

            if (string.IsNullOrEmpty(employeeId))
            {
                MessageBox.Show("Please search for an employee first before deleting.", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete employee with ID: {employeeId}?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var employeeToDelete = await supabase
                        .From<BarbershopManagementSystem>()
                        .Where(x => x.Eid == employeeId)
                        .Get();

                    if (employeeToDelete.Models.Count > 0)
                    {
                        var employee = employeeToDelete.Models.First();

                        await supabase
                            .From<BarbershopManagementSystem>()
                            .Where(x => x.Id == employee.Id)
                            .Delete();

                        var localEmployee = employees.FirstOrDefault(e => e.Eid == employeeId);
                        if (localEmployee != null)
                        {
                            employees.Remove(localEmployee);
                        }

                        DeleteSuccessfull DeleteSuccessfull = new DeleteSuccessfull();
                        DeleteSuccessfull.ShowDialog();
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show($"Employee with ID {employeeId} not found.", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting employee: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            string employeeId = txtEmployeeID.Text.Trim();

            if (string.IsNullOrEmpty(employeeId))
            {
                MessageBox.Show("Please search for an employee first before updating.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateForm())
            {
                MessageBox.Show("Please fill in all required fields correctly.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var existingEmployee = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.Eid == employeeId)
                    .Get();

                if (existingEmployee.Models.Count > 0)
                {
                    var employee = existingEmployee.Models.First();

                    // Update employee data with form values
                    employee.Fname = txtFullName.Text.Trim();
                    employee.Bdate = bdate.SelectedDate;
                    employee.Gender = GetComboBoxSelectedValue(Gender);
                    employee.Address = txtAddress.Text.Trim();
                    employee.Cnumber = txtContactNumber.Text.Trim();
                    employee.Email = txtEmail.Text.Trim();
                    employee.ECname = txtEmergencyName.Text.Trim();
                    employee.ECnumber = txtEmergencyNumber.Text.Trim();
                    employee.Role = GetComboBoxSelectedValue(cmbRole);
                    employee.Epassword = txtEmployeePassword.Text.Trim();
                    employee.Nickname = txtNickname.Text.Trim();
                    employee.BarberExpertise = GetComboBoxSelectedValue(cmbBarberExpertise);
                    employee.ServicesOffered = GetSelectedCheckBoxes(servicesPanel);
                    employee.DateHired = dateHiredPicker.SelectedDate;
                    employee.Estatus = GetComboBoxSelectedValue(cmbEmploymentStatus);
                    employee.Wsched = GetSelectedCheckBoxes(workSchedulePanel);

                    // Handle photo update - Update photo path if photo was changed
                    if (isPhotoChanged && !string.IsNullOrEmpty(currentPhotoPath))
                    {
                        employee.PhotoPath = currentPhotoPath;
                    }
                    // If no photo was changed, keep the existing photo path
                    // employee.PhotoPath remains unchanged

                    // Update in Supabase
                    var updatedEmployee = await supabase
                        .From<BarbershopManagementSystem>()
                        .Where(x => x.Id == employee.Id)
                        .Set(x => x.Fname, employee.Fname)
                        .Set(x => x.Bdate, employee.Bdate)
                        .Set(x => x.Gender, employee.Gender)
                        .Set(x => x.Address, employee.Address)
                        .Set(x => x.Cnumber, employee.Cnumber)
                        .Set(x => x.Email, employee.Email)
                        .Set(x => x.ECname, employee.ECname)
                        .Set(x => x.ECnumber, employee.ECnumber)
                        .Set(x => x.Role, employee.Role)
                        .Set(x => x.Epassword, employee.Epassword)
                        .Set(x => x.Nickname, employee.Nickname)
                        .Set(x => x.BarberExpertise, employee.BarberExpertise)
                        .Set(x => x.ServicesOffered, employee.ServicesOffered)
                        .Set(x => x.DateHired, employee.DateHired)
                        .Set(x => x.Estatus, employee.Estatus)
                        .Set(x => x.Wsched, employee.Wsched)
                        .Set(x => x.PhotoPath, employee.PhotoPath)
                        .Update();

                    // Update local collection
                    var localEmployee = employees.FirstOrDefault(e => e.Eid == employeeId);
                    if (localEmployee != null)
                    {
                        var index = employees.IndexOf(localEmployee);
                        employees[index] = employee;
                    }

                    // Reset photo changed flag after successful update
                    isPhotoChanged = false;

                    // Show success window
                    UpdateSuccessful UpdateSuccessful = new UpdateSuccessful();
                    UpdateSuccessful.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"Employee with ID {employeeId} not found.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating employee: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetComboBoxSelectedValue(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.IsEnabled &&
                !selectedItem.Foreground.Equals(System.Windows.Media.Brushes.Gray))
            {
                return selectedItem.Content.ToString();
            }
            return string.Empty;
        }

        private string GetSelectedCheckBoxes(UniformGrid panel)
        {
            var selectedItems = new List<string>();

            foreach (var child in panel.Children)
            {
                if (child is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    selectedItems.Add(checkBox.Content.ToString());
                }
            }

            return string.Join(", ", selectedItems);
        }

        private bool ValidateForm()
        {
            bool isValid = true;
            HideAllErrorMessages();

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                ShowError(txtFullNameError, "Full name is required");
                isValid = false;
            }

            if (!bdate.SelectedDate.HasValue)
            {
                ShowError(txtBdateError, "Birthdate is required");
                isValid = false;
            }

            if (Gender.SelectedIndex <= 0)
            {
                ShowError(txtGenderError, "Gender is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                ShowError(txtAddressError, "Address is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtContactNumber.Text))
            {
                ShowError(txtContactNumberError, "Contact number is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                ShowError(txtEmailError, "Email is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtEmergencyName.Text))
            {
                ShowError(txtEmergencyNameError, "Emergency contact name is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtEmergencyNumber.Text))
            {
                ShowError(txtEmergencyNumberError, "Emergency contact number is required");
                isValid = false;
            }

            if (cmbRole.SelectedIndex < 0)
            {
                ShowError(txtRoleError, "Employee role is required");
                isValid = false;
            }

            if (cmbRole.SelectedItem is ComboBoxItem roleItem && roleItem.Content.ToString() == "Cashier")
            {
                if (string.IsNullOrWhiteSpace(txtEmployeePassword.Text))
                {
                    ShowError(txtPasswordError, "Password is required for Cashier role");
                    isValid = false;
                }
            }

            if (string.IsNullOrWhiteSpace(txtNickname.Text))
            {
                ShowError(txtNicknameError, "Nickname is required");
                isValid = false;
            }

            if (!dateHiredPicker.SelectedDate.HasValue)
            {
                ShowError(txtDateHiredError, "Date hired is required");
                isValid = false;
            }

            if (cmbEmploymentStatus.SelectedIndex <= 0)
            {
                ShowError(txtEmploymentStatusError, "Employment status is required");
                isValid = false;
            }

            return isValid;
        }

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private void HideAllErrorMessages()
        {
            txtFullNameError.Visibility = Visibility.Collapsed;
            txtBdateError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtAddressError.Visibility = Visibility.Collapsed;
            txtContactNumberError.Visibility = Visibility.Collapsed;
            txtEmailError.Visibility = Visibility.Collapsed;
            txtEmergencyNameError.Visibility = Visibility.Collapsed;
            txtEmergencyNumberError.Visibility = Visibility.Collapsed;
            txtRoleError.Visibility = Visibility.Collapsed;
            txtPasswordError.Visibility = Visibility.Collapsed;
            txtNicknameError.Visibility = Visibility.Collapsed;
            txtBarberExpertiseError.Visibility = Visibility.Collapsed;
            txtDateHiredError.Visibility = Visibility.Collapsed;
            txtEmploymentStatusError.Visibility = Visibility.Collapsed;
        }

        [Table("Add_Employee")]
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

            [Column("Service_Offered")]
            public string ServicesOffered { get; set; }

            [Column("Date_Hired")]
            public DateTime? DateHired { get; set; }

            [Column("Employee_Status")]
            public string Estatus { get; set; }

            [Column("Work_Sched")]
            public string Wsched { get; set; }

            [Column("Photo")]
            public string PhotoPath { get; set; }
        }
    }
}