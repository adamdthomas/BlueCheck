using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using OpenQA.Selenium;
using System.Net.Mail;
using System.Diagnostics;
using System.Text;
using OpenQA.Selenium.PhantomJS;

namespace BlueChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void ThreadLoggerCallback(string message);
        public delegate void StatusCallback(string message);

        public static bool isStarted = false;
        public Thread CheckingThread;
        public static Dictionary<string, string> CurConfig = new Dictionary<string, string>();
        public static bool LoggedIn = false;
        public static bool FailedLastTime = false;
        public static bool OCFailedLastTime = false;
        public static bool JEFailedLastTime = false;
        public static bool ARFailedLastTime = false;
        public static char c1 = (char)10;
        public static string L = c1.ToString();
        public static byte[] myBytes = Encoding.ASCII.GetBytes("leach");
        public static Int64 LastSentAt = 0;
        public static Int64 OCLastSentAt = 0;
        public static Int64 JELastSentAt = 0;
        public static Int64 ARLastSentAt = 0;
        public static Int64 CurrentEPOCH;
        public static DateTime StartTime;
        public static DateTime CurrentTime;
        public static DateTime ElapsedTime;
        public static Int32 NumberOfChecks = 0;



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


        public void UpdateStatus(string Message)
        {
            lbStatus.Dispatcher.Invoke(
            new StatusCallback(this.Status),
            new object[] { Message });
        }

        private void Status(string Message)
        {
            //Message = DateTime.Now.ToString("MM/dd/yy HH:mm:ss") + " " + Message;
            lbStatus.Content = "Running since: " + StartTime.ToString("MM/dd/yy HH:mm:ss") + " - " + NumberOfChecks.ToString() + " Checks - " + Message; 
        
        }


        public void Checker()
        {
            while (isStarted)
            {

                CurrentEPOCH = MainMethods.GetEPOCHTimeInMilliSeconds();
                try
                {
                    UpdateStatus("Setting up driver...");
                    var driverService = PhantomJSDriverService.CreateDefaultService();
                    driverService.HideCommandPromptWindow = true;
                    WebAutomationToolkit.Web.WebDriver = new PhantomJSDriver(driverService);

                    #region Check BlueSource
                    UpdateStatus("Currently checking: BlueSource");
                    WebAutomationToolkit.Web.NavigateToURL(CurConfig["url"]);
                    WebAutomationToolkit.Web.Sync.SyncByID("ctl00_ContentPlaceHolder1_UsernameTextBox", 30);
                    LogFromThread("Attempting to log in with username: " + CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_UsernameTextBox", CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_PasswordTextBox", Encryption.SimpleDecryptWithPassword(MainWindow.CurConfig["password"], CurConfig["username"], myBytes.Length));
                    WebAutomationToolkit.Web.Button.ClickByID("ctl00_ContentPlaceHolder1_SubmitButton");
                    
                    NumberOfChecks++;
                    UpdateStatus("BlueSource");
      
                    if (WebAutomationToolkit.Web.Sync.SyncByID("search-bar", 30))
                    {
                        if (FailedLastTime)
                        {
                            SendEmail(CurConfig["tolist"], "BlueSource is back up!", "BlueSource is now accessable using username: " + CurConfig["username"] + L + CurConfig["url"] + L + "Loged in from on machine located @ " + MainMethods.GetPublicIP());
                        }
                        LogFromThread("BlueSource login was successfull!");
                        ConfigData.WriteToLog("Login was successfull with username: " + CurConfig["username"]);
                        FailedLastTime = false;
                    }
                    else
                    {
                        LogFromThread("BlueSource login failed.");
                        Int64 EI = Int64.Parse(CurConfig["emailintervalinseconds"]) * 1000;
                        Int64 TimeElapsed = CurrentEPOCH - LastSentAt;

                        if (TimeElapsed > EI)
                        {
                            SendEmail(CurConfig["tolist"], "BlueSource is Down", "BlueSource is not accessable using username: " + CurConfig["username"] + L + CurConfig["url"] + L + "Detected on machine located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
                            LastSentAt = MainMethods.GetEPOCHTimeInMilliSeconds();
                        }
                        FailedLastTime = true;
                        ConfigData.WriteToLog("Login was NOT successfull with username: " + CurConfig["username"] + " emails have been sent: " + CurConfig["tolist"]);
                    }
                    #endregion

                    #region Check Jenkins
                    UpdateStatus("Currently checking: Jenkins.Orasi.com");
                    LogFromThread("Attempting to navigate to Jenkins.Orasi.com");
                    WebAutomationToolkit.Web.NavigateToURL("http://jenkins.orasi.com");

                    NumberOfChecks++;
                    UpdateStatus("Jenkins.Orasi.com");



                    if (WebAutomationToolkit.Web.Sync.SyncByID("jenkins-home-link", 30))
                    {
                        if (JEFailedLastTime)
                        {
                            SendEmail(CurConfig["tolist"], "Jenkins.Orasi.com is back up!", "Jenkins.Orasi.com is now accessable. " + L + "Viewed from on machine located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
                        }
                        LogFromThread("Navigating to Jenkins.Orasi.com was successfull!");
                        ConfigData.WriteToLog("Navigating to Jenkins.Orasi.com was successfull");

                        JEFailedLastTime = false;
                    }
                    else
                    {
                        LogFromThread("Navigating to Jenkins.Orasi.com failed.");
                        Int64 EI = Int64.Parse(CurConfig["emailintervalinseconds"]) * 1000;
                        Int64 TimeElapsed = CurrentEPOCH - JELastSentAt;

                        if (TimeElapsed > EI)
                        {
                            SendEmail(CurConfig["tolist"], "Jenkins.Orasi.com is Down", "Jenkins.Orasi.com is not accessable." + L + "Detected on machine located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
                            JELastSentAt = MainMethods.GetEPOCHTimeInMilliSeconds();
                        }
                        JEFailedLastTime = true;
                        ConfigData.WriteToLog("Navigating to Jenkins.Orasi.com failed. emails have been sent: " + CurConfig["tolist"]);
                    }
                    #endregion

                    #region Check Orasi.com
                    UpdateStatus("Currently checking: Orasi.com");
                    LogFromThread("Attempting to navigate to Orasi.com");
                    WebAutomationToolkit.Web.NavigateToURL("http://orasi.com");

                    NumberOfChecks++;
                    



                    if (WebAutomationToolkit.Web.Sync.SyncByID("ctl00_onetidHeadbnnr2", 30))
                    {
                        if (OCFailedLastTime)
                        {
                            SendEmail(CurConfig["tolist"], "Orasi.com is back up!", "Orasi.com is now accessable. " + L + "Viewed from on machine located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
                        }
                        LogFromThread("Navigating to Orasi.com was successfull!");
                        ConfigData.WriteToLog("Navigating to Orasi.com was successfull");
                      
                        OCFailedLastTime = false;
                    } 
                    else
                    {
                        LogFromThread("Navigating to Orasi.com failed.");
                        Int64 EI = Int64.Parse(CurConfig["emailintervalinseconds"]) * 1000;
                        Int64 TimeElapsed = CurrentEPOCH - OCLastSentAt;

                        if (TimeElapsed > EI)
                        {
                            SendEmail(CurConfig["tolist"], "Orasi.com is Down", "Orasi.com is not accessable." + L + "Detected on machine located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
                            OCLastSentAt = MainMethods.GetEPOCHTimeInMilliSeconds();
                        }
                        OCFailedLastTime = true;
                        ConfigData.WriteToLog("Navigating to Orasi.com failed. emails have been sent: " + CurConfig["tolist"]);
                    }
                    #endregion

                    #region Check AutoRun
                    UpdateStatus("Currently checking: AutoRun.Orasi.com");
                    LogFromThread("Attempting to navigate to AutoRun.Orasi.com");
                    WebAutomationToolkit.Web.NavigateToURL("https://autorun.orasi.com");

                    NumberOfChecks++;

                    if (WebAutomationToolkit.Web.Sync.SyncByID("jenkins-home-link", 30))
                    {
                        if (ARFailedLastTime)
                        {
                            SendEmail(CurConfig["tolist"], "AutoRun.Orasi.com is back up!", "AutoRun.Orasi.com is now accessable. " + L + "Viewed from on machine located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
                        }
                        LogFromThread("Navigating to AutoRun.Orasi.com was successfull!");
                        ConfigData.WriteToLog("Navigating to AutoRun.Orasi.com was successfull");

                        ARFailedLastTime = false;
                    }
                    else
                    {
                        LogFromThread("Navigating to AutoRun.Orasi.com failed.");
                        Int64 EI = Int64.Parse(CurConfig["emailintervalinseconds"]) * 1000;
                        Int64 TimeElapsed = CurrentEPOCH - ARLastSentAt;

                        if (TimeElapsed > EI)
                        {
                            SendEmail(CurConfig["tolist"], "AutoRun.Orasi.com is Down", "AutoRun.Orasi.com is not accessable." + L + "Detected on machine located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
                            ARLastSentAt = MainMethods.GetEPOCHTimeInMilliSeconds();
                        }
                        ARFailedLastTime = true;
                        ConfigData.WriteToLog("Navigating to AutoRun.Orasi.com failed. emails have been sent: " + CurConfig["tolist"]);
                    }
                    #endregion
                    
                    WebAutomationToolkit.Web.CloseBrowser();
                    WebAutomationToolkit.Web.WebDriver.Quit();
 
                }
                catch (Exception ex)
                {
                    LogFromThread("Something very bad happened...See logs for details.");
                    ConfigData.WriteToLog("BlueSource checking service has encountered a serious error:" + L + L + ex.ToString());
                }




                DateTime DT1 = DateTime.Now;
                DateTime DT2 = DT1.AddSeconds(Int32.Parse(CurConfig["cycletimeinseconds"]));
                LogFromThread("Next check will take place around: " + DT2.ToString("MM/dd/yy HH:mm:ss"));

                Int32 t = Int32.Parse(CurConfig["cycletimeinseconds"]) * 1000;
                UpdateStatus("Waiting...");
                Thread.Sleep(t);

                //Clean thread junk
                WebAutomationToolkit.Web.WebDriver = null;
                System.GC.Collect();

            }

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
                lbStatus.Content = "Checker has been shutdown...";
            }
            else
            {

                StartTime = DateTime.Now;
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
            SendEmail(CurConfig["tolist"], "Test Email", "This is a test email from the bluesource checker application located @ " + MainMethods.GetPublicIP() + L + L + this.Title);
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
        
 
