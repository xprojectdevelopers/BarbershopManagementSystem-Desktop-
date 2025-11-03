using System;
using System.Windows;

namespace Capstone.AppointmentOptions
{
    public partial class ViewReceipt : Window
    {
        public ViewReceipt(string appointmentNumber, DateTime? appointmentDate, TimeSpan? appointmentTime, string paymentStatus)
        {
            InitializeComponent();

            // Set the appointment number
            txtAppointmentNumber.Text = string.IsNullOrWhiteSpace(appointmentNumber) ? "N/A" : appointmentNumber;

            // Format date and time
            if (appointmentDate.HasValue && appointmentTime.HasValue)
            {
                string formattedDate = appointmentDate.Value.ToString("MM/dd/yyyy");
                string formattedTime = DateTime.Today.Add(appointmentTime.Value).ToString("h:mm tt");
                string formattedDateTime = $"{formattedDate} ({formattedTime})";
                txtDateTime.Text = formattedDateTime;
            }
            else
            {
                txtDateTime.Text = "N/A";
            }

            // Set payment status
            txtPaymentStatus.Text = string.IsNullOrWhiteSpace(paymentStatus) || paymentStatus == "N/A"
                ? "N/A"
                : paymentStatus;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}