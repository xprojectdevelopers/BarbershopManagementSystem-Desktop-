using Microsoft.Win32;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Text.RegularExpressions;
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
        private string currentPhotoPath;
        private bool isPhotoChanged = false;
        private Window currentModalWindow;

        public EmployeeProfile()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeData();
            ModalOverlay.PreviewMouseLeftButtonDown += ModalOverlay_Click;
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

                // Add Employee IDs to ComboBox
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = emp.Eid,
                    Tag = emp
                };
                cmbEmployeeID.Items.Add(item);
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
            if (cmbEmployeeID.SelectedIndex <= 0)
            {
                ShowError(txtEmployeeIDError, "Please select an Employee ID");
                return;
            }

            txtEmployeeIDError.Visibility = Visibility.Collapsed;

            try
            {
                ComboBoxItem selectedItem = cmbEmployeeID.SelectedItem as ComboBoxItem;
                string employeeId = selectedItem.Content.ToString();

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

        private void ModalWindow_Closed(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
            currentModalWindow = null;
        }

        private bool ValidateEmployeeInlineForUpdate(BarbershopManagementSystem newEmployee, string currentEmployeeId)
        {
            bool isValid = true;

            txtFullNameTaken.Visibility = Visibility.Collapsed;
            txtPasswordSame.Visibility = Visibility.Collapsed;
            txtNicknameTaken.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(newEmployee.Fname) &&
                employees.Any(emp => emp.Eid != currentEmployeeId &&
                                   emp.Fname.Equals(newEmployee.Fname, StringComparison.OrdinalIgnoreCase)))
            {
                ShowError(txtFullNameTaken, "Full name already exists. Please enter a different one.");
                isValid = false;
            }

            if (newEmployee.Role == "Cashier" && !string.IsNullOrWhiteSpace(newEmployee.Epassword) &&
                employees.Any(emp => emp.Eid != currentEmployeeId && emp.Epassword == newEmployee.Epassword))
            {
                ShowError(txtPasswordSame, "Password already in use. Please choose another.");
                isValid = false;
            }

            if (!string.IsNullOrWhiteSpace(newEmployee.Nickname) &&
                employees.Any(emp => emp.Eid != currentEmployeeId &&
                                   emp.Nickname.Equals(newEmployee.Nickname, StringComparison.OrdinalIgnoreCase)))
            {
                ShowError(txtNicknameTaken, "Employee nickname is already taken.");
                isValid = false;
            }

            return isValid;
        }

        private void PopulateForm(BarbershopManagementSystem employee)
        {
            txtFullName.Text = employee.Fname ?? "";
            bdate.SelectedDate = employee.Bdate;
            SetComboBoxSelection(Gender, employee.Gender);
            txtAddress.Text = employee.Address ?? "";
            txtContactNumber.Text = employee.Cnumber ?? "";
            txtEmail.Text = employee.Email ?? "";
            txtEmergencyName.Text = employee.ECname ?? "";
            txtEmergencyNumber.Text = employee.ECnumber ?? "";
            SetComboBoxSelection(cmbRole, employee.Role);
            txtEmployeePassword.Text = employee.Epassword ?? "";
            txtNickname.Text = employee.Nickname ?? "";
            SetComboBoxSelection(cmbBarberExpertise, employee.BarberExpertise);
            dateHiredPicker.SelectedDate = employee.DateHired;
            SetComboBoxSelection(cmbEmploymentStatus, employee.Estatus);
            SetCheckBoxes(workSchedulePanel, employee.Wsched);

            currentPhotoPath = employee.PhotoPath;
            isPhotoChanged = false;
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
                        PhotoPreview.Source = new BitmapImage(new Uri("/Icon/profile.png", UriKind.Relative));
                    }
                }
                else
                {
                    PhotoPreview.Source = new BitmapImage(new Uri("/Icon/profile.png", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                PhotoPreview.Source = new BitmapImage(new Uri("/Icon/profile.png", UriKind.Relative));
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
                PhotoPreview.Source = new BitmapImage(new Uri("/Icon/profile.png", UriKind.Relative));
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
            cmbEmployeeID.SelectedIndex = 0;
            txtEmployeeIDError.Text = string.Empty;
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
            ClearCheckBoxes(workSchedulePanel);
            PhotoPreview.Source = new BitmapImage(new Uri("/Icon/profile.png", UriKind.Relative));
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
                    btnGeneratePassword.IsEnabled = true;
                    txtEmployeePassword.IsEnabled = true;
                    btnGeneratePassword.Foreground = Brushes.Blue;
                    txtEmployeePassword.Background = Brushes.White;
                    txtEmployeePassword.Foreground = Brushes.Black;
                    cmbBarberExpertise.SelectedIndex = -1;

                    var passwordLabel = FindVisualChild<Label>(this, "lblEmployeePassword");
                    if (passwordLabel != null)
                    {
                        passwordLabel.Foreground = Brushes.Black;
                        passwordLabel.IsEnabled = true;
                    }

                    cmbBarberExpertise.IsEnabled = false;
                    cmbBarberExpertise.Foreground = Brushes.Gray;

                    var expertiseLabel = FindVisualChild<Label>(this, "lblBarberExpertise");
                    if (expertiseLabel != null)
                    {
                        expertiseLabel.Foreground = Brushes.Gray;
                        expertiseLabel.IsEnabled = false;
                    }

                    var servicesLabel = FindVisualChild<Label>(this, "lblServicesOffered");
                    if (servicesLabel != null)
                    {
                        servicesLabel.Foreground = Brushes.Gray;
                        servicesLabel.IsEnabled = false;
                    }
                }
                else if (role == "Barber")
                {
                    btnGeneratePassword.IsEnabled = false;
                    txtEmployeePassword.IsEnabled = false;
                    btnGeneratePassword.Foreground = Brushes.Gray;
                    txtEmployeePassword.Background = Brushes.LightGray;
                    txtEmployeePassword.Foreground = Brushes.Gray;
                    txtEmployeePassword.Text = string.Empty;

                    var passwordLabel = FindVisualChild<Label>(this, "lblEmployeePassword");
                    if (passwordLabel != null)
                    {
                        passwordLabel.Foreground = Brushes.Gray;
                        passwordLabel.IsEnabled = false;
                    }

                    cmbBarberExpertise.IsEnabled = true;
                    cmbBarberExpertise.Foreground = Brushes.Black;

                    var expertiseLabel = FindVisualChild<Label>(this, "lblBarberExpertise");
                    if (expertiseLabel != null)
                    {
                        expertiseLabel.Foreground = Brushes.Black;
                        expertiseLabel.IsEnabled = true;
                    }

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
                btnGeneratePassword.IsEnabled = false;
                txtEmployeePassword.IsEnabled = false;
                btnGeneratePassword.Foreground = Brushes.Gray;
                txtEmployeePassword.Background = Brushes.LightGray;
                txtEmployeePassword.Foreground = Brushes.Gray;
                cmbBarberExpertise.IsEnabled = false;
                cmbBarberExpertise.Foreground = Brushes.Gray;
                txtEmployeePassword.Text = string.Empty;
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
            if (cmbEmployeeID.SelectedIndex <= 0)
            {
                ShowError(txtEmployeeIDError, "Please select an Employee ID");
                return;
            }

            ComboBoxItem selectedItem = cmbEmployeeID.SelectedItem as ComboBoxItem;
            string employeeId = selectedItem.Content.ToString();

            try
            {
                var employeeToDelete = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.Eid == employeeId)
                    .Get();

                if (employeeToDelete.Models.Count == 0)
                {
                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new notfound();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    ClearForm();
                    return;
                }

                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new delete();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                delete deleteDialog = (delete)currentModalWindow;
                currentModalWindow.Closed += ModalWindow_Closed;

                bool? result = deleteDialog.ShowDialog();

                if (result == true)
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

                    // Remove from ComboBox
                    cmbEmployeeID.Items.Remove(selectedItem);

                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new DeleteSuccessfull();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting employee: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (cmbEmployeeID.SelectedIndex <= 0)
            {
                ShowError(txtEmployeeIDError, "Please select an Employee ID");
                return;
            }

            ComboBoxItem selectedItem = cmbEmployeeID.SelectedItem as ComboBoxItem;
            string employeeId = selectedItem.Content.ToString();

            try
            {
                var existingEmployee = await supabase
                    .From<BarbershopManagementSystem>()
                    .Where(x => x.Eid == employeeId)
                    .Get();

                if (existingEmployee.Models.Count == 0)
                {
                    HideAllErrorMessages();
                    ModalOverlay.Visibility = Visibility.Visible;

                    currentModalWindow = new notfound();
                    currentModalWindow.Owner = this;
                    currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    currentModalWindow.Closed += ModalWindow_Closed;
                    currentModalWindow.Show();
                    ClearForm();
                    return;
                }

                var employee = existingEmployee.Models.First();

                if (!ValidateForm())
                {
                    return;
                }

                var tempEmployee = new BarbershopManagementSystem
                {
                    Fname = txtFullName.Text.Trim(),
                    Role = GetComboBoxSelectedValue(cmbRole),
                    Epassword = txtEmployeePassword.Text.Trim(),
                    Nickname = txtNickname.Text.Trim()
                };

                if (!ValidateEmployeeInlineForUpdate(tempEmployee, employeeId))
                {
                    return;
                }

                employee.Fname = txtFullName.Text.Trim();
                employee.Bdate = bdate.SelectedDate;
                employee.Gender = GetComboBoxSelectedValue(Gender);
                employee.Address = txtAddress.Text.Trim();
                employee.Cnumber = txtContactNumber.Text.Trim();
                employee.Email = txtEmail.Text.Trim();
                employee.ECname = txtEmergencyName.Text.Trim();
                employee.ECnumber = txtEmergencyNumber.Text.Trim();
                employee.Role = GetComboBoxSelectedValue(cmbRole);

                string selectedRole = GetComboBoxSelectedValue(cmbRole);
                if (selectedRole == "Cashier")
                {
                    employee.Epassword = txtEmployeePassword.Text.Trim();
                }
                else if (selectedRole == "Barber")
                {
                    employee.Epassword = string.Empty;
                }

                employee.Nickname = txtNickname.Text.Trim();

                if (selectedRole == "Barber")
                {
                    employee.BarberExpertise = GetComboBoxSelectedValue(cmbBarberExpertise);
                }
                else if (selectedRole == "Cashier")
                {
                    employee.BarberExpertise = string.Empty;
                }

                employee.DateHired = dateHiredPicker.SelectedDate;
                employee.Estatus = GetComboBoxSelectedValue(cmbEmploymentStatus);
                employee.Wsched = GetSelectedCheckBoxes(workSchedulePanel);

                if (isPhotoChanged && !string.IsNullOrEmpty(currentPhotoPath))
                {
                    employee.PhotoPath = currentPhotoPath;
                }

                await supabase
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
                    .Set(x => x.DateHired, employee.DateHired)
                    .Set(x => x.Estatus, employee.Estatus)
                    .Set(x => x.Wsched, employee.Wsched)
                    .Set(x => x.PhotoPath, employee.PhotoPath)
                    .Update();

                var localEmployee = employees.FirstOrDefault(e => e.Eid == employeeId);
                if (localEmployee != null)
                {
                    var index = employees.IndexOf(localEmployee);
                    employees[index] = employee;
                }

                isPhotoChanged = false;

                ModalOverlay.Visibility = Visibility.Visible;

                currentModalWindow = new UpdateSuccessful();
                currentModalWindow.Owner = this;
                currentModalWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                currentModalWindow.Closed += ModalWindow_Closed;
                currentModalWindow.Show();
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

            var newEmployee = new BarbershopManagementSystem
            {
                Fname = txtFullName.Text.Trim(),
                Cnumber = txtContactNumber.Text.Trim(),
                ECnumber = txtEmergencyNumber.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Role = GetComboBoxSelectedValue(cmbRole),
                Epassword = txtEmployeePassword.Text.Trim(),
                Nickname = txtNickname.Text.Trim()
            };

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
            else if (!Regex.IsMatch(newEmployee.Cnumber, @"^[0-9]+$"))
            {
                ShowError(txtContactNumberError, "No special character and alphabet");
                isValid = false;
            }
            else if (!Regex.IsMatch(newEmployee.Cnumber, @"^[0-9]{11}$"))
            {
                ShowError(txtContactNumberError, "Contact Number must be 11 digits only");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                ShowError(txtEmailError, "Email is required");
                isValid = false;
            }
            else if (!Regex.IsMatch(newEmployee.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError(txtEmailError, "Please enter a valid email address");
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
            else if (!Regex.IsMatch(newEmployee.ECnumber, @"^[0-9]+$"))
            {
                ShowError(txtEmergencyNumberError, "No special character and alphabet");
                isValid = false;
            }
            else if (!Regex.IsMatch(newEmployee.ECnumber, @"^[0-9]{11}$"))
            {
                ShowError(txtEmergencyNumberError, "Emergency contact number must be 11 digits only");
                isValid = false;
            }

            if (cmbRole.SelectedIndex < 0)
            {
                ShowError(txtRoleError, "Employee role is required");
                isValid = false;
            }
            else
            {
                string selectedRole = GetComboBoxSelectedValue(cmbRole);

                if (selectedRole == "Cashier")
                {
                    if (string.IsNullOrWhiteSpace(txtEmployeePassword.Text))
                    {
                        ShowError(txtPasswordError, "Password is required for Cashier role");
                        isValid = false;
                    }
                }
                else if (selectedRole == "Barber")
                {
                    if (cmbBarberExpertise.SelectedIndex <= 0)
                    {
                        ShowError(txtBarberExpertiseError, "Barber expertise is required for Barber role");
                        isValid = false;
                    }
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

            bool hasSelectedDay = false;
            foreach (var child in workSchedulePanel.Children)
            {
                if (child is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    hasSelectedDay = true;
                    break;
                }
            }

            if (!hasSelectedDay)
            {
                ShowError(txtWorkScheduleError, "At least one work day must be selected");
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
            txtEmployeeIDError.Visibility = Visibility.Collapsed;
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
            txtWorkScheduleError.Visibility = Visibility.Collapsed;
            txtFullNameTaken.Visibility = Visibility.Collapsed;
            txtPasswordSame.Visibility = Visibility.Collapsed;
            txtNicknameTaken.Visibility = Visibility.Collapsed;
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Visible;

            currentModalWindow = new ModalsSetting();
            currentModalWindow.Owner = this;
            currentModalWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            currentModalWindow.Left = this.Left + this.ActualWidth - currentModalWindow.Width - 110;
            currentModalWindow.Top = this.Top + 100;

            currentModalWindow.Closed += ModalWindow_Closed;
            currentModalWindow.Show();
        }

        private void ModalOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (currentModalWindow != null)
                currentModalWindow.Close();

            e.Handled = true;
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