using System.Text;
using System.Windows;

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


            string EncryptGMPassword = Encryption.SimpleEncryptWithPassword(txPasswordGM.Text, MainWindow.CurConfig["gmailusername"], myBytes);
            MainWindow.CurConfig["gmailpassword"] = EncryptGMPassword;


            ConfigData.SetConfigData();

            ConfigData newConf = new ConfigData();
            MainWindow.CurConfig = newConf.GetConfigData();

        }

        private void buttonBS_Click(object sender, RoutedEventArgs e)
        {
            byte[] myBytes = Encoding.ASCII.GetBytes("leach");
            string EncryptBSPassword = Encryption.SimpleEncryptWithPassword(txPasswordBS.Text, MainWindow.CurConfig["username"], myBytes);
            MainWindow.CurConfig["password"] = EncryptBSPassword;


            ConfigData.SetConfigData();

            ConfigData newConf = new ConfigData();
            MainWindow.CurConfig = newConf.GetConfigData();


            
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
