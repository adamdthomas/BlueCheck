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

namespace BlueChecker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PassEnc : Window
    {
        public PassEnc()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

            byte[] myBytes = Encoding.ASCII.GetBytes("leach");
            string EncryptBSPassword = Encryption.SimpleEncryptWithPassword(txPasswordBS.Text, MainWindow.CurConfig["username"], myBytes);
            MainWindow.CurConfig["password"] = EncryptBSPassword;

            string EncryptGMPassword = Encryption.SimpleEncryptWithPassword(txPasswordGM.Text, MainWindow.CurConfig["gmailusername"], myBytes);
            MainWindow.CurConfig["gmailpassword"] = EncryptGMPassword;


            ConfigData.SetConfigData();

            ConfigData newConf = new ConfigData();
            MainWindow.CurConfig = newConf.GetConfigData();
            

            

            this.Close();
        }
    }
}
