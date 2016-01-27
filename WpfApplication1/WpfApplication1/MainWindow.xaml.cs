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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace BlueChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {



        public MainWindow()
        {
            InitializeComponent();
        }



        private void btStartStop_Click(object sender, RoutedEventArgs e)
        {

            
            if (MainMethods.isStarted)
            {
                //if its started, then stop it
                
                MainMethods.thread.Abort();
                MainMethods.isStarted = false;
                btStartStop.Content = "Start";
            }
            else
            {
                MainMethods.thread.Start();
                MainMethods.isStarted = true;
                btStartStop.Content = "Stop";
            }

            
        }
    }
}
