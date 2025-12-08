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

namespace ACControllerServer.Views
{
    /// <summary>
    /// Interaction logic for ScanQRPassWindow.xaml
    /// </summary>
    public partial class ScanQRPassWindow : Window
    {
        public ScanQRPassWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; 
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtQRCode.Text = string.Empty;
            txtQRCode.Focus();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
