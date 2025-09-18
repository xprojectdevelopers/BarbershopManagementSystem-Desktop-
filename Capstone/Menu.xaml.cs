using System.Windows;

namespace Capstone
{
    public partial class Menu : Window
    {
        public Menu()
        {
            InitializeComponent();
        }


        private void Employees_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EMenu EMenu = new EMenu();
            EMenu.Show();
            this.Hide();
        }
    }
}
