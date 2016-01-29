using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using OpenQA.Selenium.Chrome;
using SimpleBrowser.Network;
using SimpleBrowser.Elements;
using SimpleBrowser.Parser;
using SimpleBrowser.Query.Selectors;
using OpenQA.Selenium;
using System.Net.Mail;
using SimpleBrowser.WebDriver;
using System.Diagnostics;
using System.Text;

namespace BlueChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void ThreadLoggerCallback(string message);
        public static bool isStarted = false;
        public Thread CheckingThread;
        public static Dictionary<string, string> CurConfig = new Dictionary<string, string>();
        public static bool LoggedIn = false;
        public static char c1 = (char)10;
        public static string L = c1.ToString();
        public static byte[] myBytes = Encoding.ASCII.GetBytes("leach");

        public void LogFromThread(string Message)
        {
            txMain.Dispatcher.Invoke(
            new ThreadLoggerCallback(this.ThreadLogger),
            new object[] { Message });
        }

        private void ThreadLogger(string Message)
        {
            Message = DateTime.Now.ToString("MM/dd/yy HH:mm:ss") + " " + Message;
            txMain.AppendText(Message + L);
            txMain.ScrollToEnd();
        }

        public void Checker()
        {
            while (isStarted)
            {
                try
                {
       
                    WebAutomationToolkit.Web.WebDriver = new ChromeDriver();
                    WebAutomationToolkit.Web.NavigateToURL(CurConfig["url"]);
                    WebAutomationToolkit.Web.Sync.SyncByID("ctl00_ContentPlaceHolder1_UsernameTextBox", 30);
                    LogFromThread("Attempting to log in with username: " + CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_UsernameTextBox", CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_PasswordTextBox", Encryption.SimpleDecryptWithPassword(MainWindow.CurConfig["password"], CurConfig["username"], myBytes.Length));
                    WebAutomationToolkit.Web.Button.ClickByID("ctl00_ContentPlaceHolder1_SubmitButton");
                    WebAutomationToolkit.Web.Sync.SyncByID("search-bar", 30);

                    try
                    {
                        WebAutomationToolkit.Web.WebDriver.FindElement(By.Id("search-bar"));
                        LoggedIn = true;
                    }
                    catch (NoSuchElementException)
                    {
                        LoggedIn = false;
                    }

                    if (LoggedIn)
                    {
                        LogFromThread("Login was successfull!");
                        ConfigData.WriteToLog("Login was successfull with username: " + CurConfig["username"]);
                        DateTime DT1 = DateTime.Now;
                        DateTime DT2 = DT1.AddSeconds(Int32.Parse(CurConfig["cycletimeinseconds"]));
                        LogFromThread("Next check will take place around: " + DT2.ToString("MM/dd/yy HH:mm:ss"));
                        
                    }
                    else
                    {
                        LogFromThread("Login failed.");
                        SendEmail(CurConfig["tolist"], "BlueSource is Down", "BlueSource is not accessable using username: " + CurConfig["username"] + L + CurConfig["url"]);
                        ConfigData.WriteToLog("Login was NOT successfull with username: " + CurConfig["username"] + " emails have been sent: " + CurConfig["tolist"]);
                    }

                    WebAutomationToolkit.Web.CloseBrowser();
                    WebAutomationToolkit.Web.WebDriver.Quit();
 
                }
                catch (Exception ex)
                {
                    LogFromThread("Something very bad happened...See logs for details.");
                    ConfigData.WriteToLog("BlueSource checking service has encountered a serious error:" + L + L + ex.ToString());
                }
                Int32 t = Int32.Parse(CurConfig["cycletimeinseconds"]) * 1000;
                Thread.Sleep(t);
            }
            var n = 1;
        }

        public MainWindow()
        {
            InitializeComponent();
           ConfigData CurCon = new ConfigData();
            CurConfig = CurCon.GetConfigData();
        }

        private void btStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (isStarted)
            {
                //Stopping checker thread
                isStarted = false;
                btStartStop.Content = "Start";
                CheckingThread = null;

                LogFromThread("BlueSource checking service has been shutdown.");
                ConfigData.WriteToLog("BlueSource checking service has been shutdown.");
            }
            else
            {

                //starting checker
                ConfigData CurCon = new ConfigData();
                CurConfig = CurCon.GetConfigData();
                CheckingThread = new Thread(new ThreadStart(Checker));
                isStarted = true;
                CheckingThread.Start();
                btStartStop.Content = "Stop";

                LogFromThread("BlueSource checking service has started.");
                ConfigData.WriteToLog("BlueSource checking service has started.");
            }
        }

        private void btTestEmail_Click(object sender, RoutedEventArgs e)
        {
            SendEmail(CurConfig["tolist"], "Test Email", "This is a test email from the bluesource checker application.");
            ConfigData.WriteToLog("Test emails have been sent: " + CurConfig["tolist"]);
        }

        private void SendEmail(string toList, string subject, string body)
        {

            string[] recipients = toList.Split(',');
            foreach (string recipient in recipients)
            {
                try
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                    mail.From = new MailAddress(CurConfig["gmailusername"] + "@gmail.com");
                    mail.To.Add(recipient);
                    mail.Subject = subject;
                    mail.Body = body;

                    SmtpServer.Port = 587;
                    SmtpServer.Credentials = new System.Net.NetworkCredential(CurConfig["gmailusername"], Encryption.SimpleDecryptWithPassword(MainWindow.CurConfig["gmailpassword"], CurConfig["gmailusername"], myBytes.Length));
                    SmtpServer.EnableSsl = true;

                    SmtpServer.Send(mail);
                    LogFromThread("EMail sent to: " + recipient);
                }
                catch (Exception em)
                {
                    LogFromThread(em.ToString());
                }         
            }

                      
        }

        private void btClearlog_Click(object sender, RoutedEventArgs e)
        {
            txMain.Clear();
        }

        private void btOpenLog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("notepad.exe", ConfigData.LogFullPath);
        }

        private void btOpenConfig_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("notepad.exe", ConfigData.ConfigPath);
        }

        private void btEncPassword_Click(object sender, RoutedEventArgs e)
        {

            PassEnc NewPassEnc = new PassEnc();
            NewPassEnc.Show();
            //Window1.IsVisibleProperty = true;
        }
    }
}
        
 
