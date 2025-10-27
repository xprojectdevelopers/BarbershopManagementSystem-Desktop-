﻿using System;
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

namespace Capstone.AppointmentOptions
{
    /// <summary>
    /// Interaction logic for NewServiceSuccessfully.xaml
    /// </summary>
    public partial class NewServiceSuccessfully : Window
    {
        public NewServiceSuccessfully()
        {
            InitializeComponent();
        }

        private void Successfully_Click(object sender, RoutedEventArgs e)
        {
            // Close the success popup window
            this.Close();
        }
    }
}
