using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Net.Mail;

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

        public void LogFromThread(string Message)
        {
            txMain.Dispatcher.Invoke(
            new ThreadLoggerCallback(this.ThreadLogger),
            new object[] { Message });
        }

        private void ThreadLogger(string Message)
        {
            Message = DateTime.Now.ToString("HH:mm:ss") + " " + Message;
            txMain.AppendText(Message + L);
            txMain.ScrollToEnd();
        }

        public void Checker()
        {
            while (isStarted)
            {
                try
                {
                    string path = Environment.CurrentDirectory;
                    WebAutomationToolkit.Web.WebDriver = new ChromeDriver();
                    WebAutomationToolkit.Web.NavigateToURL(CurConfig["url"]);
                    WebAutomationToolkit.Web.Sync.SyncByID("ctl00_ContentPlaceHolder1_UsernameTextBox", 30);
                    LogFromThread("Attempting to log in with username: " + CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_UsernameTextBox", CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_PasswordTextBox", CurConfig["password"]);
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
                    }
                    else
                    {
                        LogFromThread("Login failed.");
                        SendEmail(CurConfig["tolist"], "BlueSource is Down", "BlueSource is not accessable using username: " + CurConfig["username"] + L + CurConfig["url"]);
                    }

                    WebAutomationToolkit.Web.CloseBrowser();
                    WebAutomationToolkit.Web.WebDriver.Quit();
                    Thread.Sleep(30000);
                }
                catch (Exception ex)
                {
                    LogFromThread("Something bad happened...");
                }
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
                isStarted = false;
                btStartStop.Content = "Start";
                CheckingThread = null;
            }
            else
            {
                CheckingThread = new Thread(new ThreadStart(Checker));
                isStarted = true;
                CheckingThread.Start();
                btStartStop.Content = "Stop";
            }
        }

        private void btTestEmail_Click(object sender, RoutedEventArgs e)
        {
            SendEmail(CurConfig["tolist"], "Test Email", "This is a test email from the bluesource checker application.");
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
                    SmtpServer.Credentials = new System.Net.NetworkCredential(CurConfig["gmailusername"], CurConfig["gmailpassword"]);
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
    }
}
        
 
