using System;
using System.Collections.Generic;
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
    /// Interaction logic for AdvanceReport.xaml
    /// </summary>
    public partial class AttendanceReport : Window
    {
        public AttendanceReport()
        {
            InitializeComponent();
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            EMenu EMenu = new EMenu();
            EMenu.Show();
            this.Close();
        }
    }
}
