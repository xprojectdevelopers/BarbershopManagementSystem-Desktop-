using Microsoft.Win32;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Capstone
{
    public partial class EmployeeProfile : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;

        public EmployeeProfile()
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

            [Column("Service_Offered")]
            public string ServicesOffered { get; set; }

            [Column("Date_Hired")]
            public DateTime? DateHired { get; set; }

            [Column("Employee_Status")]
            public string Estatus { get; set; }

            [Column("Work_Sched")]
            public string Wsched { get; set; }
        }
    }
}
