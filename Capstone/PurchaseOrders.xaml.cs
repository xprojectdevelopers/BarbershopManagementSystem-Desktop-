using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
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
    /// <summary>
    /// Interaction logic for PurchaseOrders.xaml
    /// </summary>
    public partial class PurchaseOrders : Window
    {
        private Client supabase;
        private ObservableCollection<BarbershopManagementSystem> employees;

        public PurchaseOrders()
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

            // Fetch data from Supabase
            var result = await supabase.From<BarbershopManagementSystem>().Get();
            foreach (var emp in result.Models)
            {
                employees.Add(emp);
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
