using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading;
using OpenQA.Selenium;
using System.Net.Mail;
using System.Diagnostics;
using System.Text;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Chrome;

namespace BlueChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void ThreadLoggerCallback(string message);
        public delegate void StatusCallback(string message);
        public delegate void ProgressCallback(int min, int max, int val);

        public static bool isStarted = false;
        public Thread CheckingThread;
        public static Dictionary<string, string> CurConfig = new Dictionary<string, string>();
        public bool LoggedIn = false;
        public bool FailedLastTime = false;
        public static char c1 = (char)10;
        public string L = c1.ToString();
        public byte[] myBytes = Encoding.ASCII.GetBytes("leach");
        public Int64 LastSentAt = 0;
        public Int64 CurrentEPOCH;
        public DateTime StartTime;
        public DateTime CurrentTime;
        public DateTime ElapsedTime;
        public Int32 NumberOfPasses = 0;
        public Int32 NumberOfFailures = 0;
        public Int32 LogLines = 0;
        public static Int64[] aLastSentAt;
        public static bool[] aFailedLastTime;
        public int URLCount = 0;
        public string[] urlandids;
        public string[] ids;
        public Stopwatch stopwatch = new Stopwatch();
        public string BCVersion;
        public int CurrentProgress = 1;

        public void Waiter()
        {
            int t = int.Parse(CurConfig["cycletimeinseconds"]);
            t = t * 100;
            ProgressFromThread(0, t, t);


            for (int w = t; w > 0; w--)
            {
                if (!isStarted)
                {
                    Stopper();
                    break; 
                }
                ProgressFromThread(0, t, w);
                Thread.Sleep(10);
                UpdateStatus("Waiting... ");
                
            }

            ProgressFromThread(0, t, 0);
            CurrentProgress = 1;


        }

        public void Stopper()
        {
            UpdateStatus("Stopped");
            ProgressFromThread(0, 1, 0);
            CurrentProgress = 1;
        }

        #region Delegators

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
            LogLines++;


            if (LogLines > 990)
            {
                Message = DateTime.Now.ToString("MM/dd/yy HH:mm:ss") + " " + "UI log will be cleared soon to conserve memory. Check log file for detailed history";
                txMain.AppendText(Message + L);
                txMain.ScrollToEnd();
            }

            if (LogLines > 1000)
            {
                txMain.Clear();
                Message = DateTime.Now.ToString("MM/dd/yy HH:mm:ss") + " " + "UI log has been cleared soon to conserve memory. Check log file for detailed history";
                txMain.AppendText(Message + L);
                txMain.ScrollToEnd();
                LogLines = 0;
            }

        }

        public void ProgressFromThread(int min, int max, int val)
        {
            pbMainLoad.Dispatcher.Invoke(
            new ProgressCallback(this.ProgressLogger),
            new object[] { min, max, val });
        }

        private void ProgressLogger(int min, int max, int val)
        {
            pbMainLoad.Minimum = min;
            pbMainLoad.Maximum = max;
            pbMainLoad.Value = val;
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
            lbStatus.Content = "Running since: " + StartTime.ToString("MM/dd/yy HH:mm:ss") + " - " + NumberOfPasses.ToString() + " Passes | " + NumberOfFailures.ToString() + " Failures - " + Message; 
        
        }

        #endregion

        public void Checker()
        {
            while (isStarted)
            {
                if (CurConfig["username"] == "usernamehere")
                {
                    LogFromThread("Cannot start because your configuration file is not set up. Please set up your configuration file and try again.");
                    isStarted = false;
                    CheckingThread = null;
                    break;
                }

                CurrentEPOCH = MainMethods.GetEPOCHTimeInMilliSeconds();
                try
                {
                    UpdateStatus("Setting up driver...");
                    var driverService = PhantomJSDriverService.CreateDefaultService();
                    driverService.HideCommandPromptWindow = true;
                    WebAutomationToolkit.Web.WebDriver = new PhantomJSDriver(driverService);

                    //WebAutomationToolkit.Web.WebDriver = new ChromeDriver();

                    #region Check BlueSource
                    UpdateStatus("Currently checking: BlueSource");
                    
                    stopwatch.Reset();
                    stopwatch.Start();
                    WebAutomationToolkit.Web.NavigateToURL(CurConfig["url"]);
                    WebAutomationToolkit.Web.Sync.SyncByID("ctl00_ContentPlaceHolder1_UsernameTextBox", 30);
                    LogFromThread("Attempting to log in with username: " + CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_UsernameTextBox", CurConfig["username"]);
                    WebAutomationToolkit.Web.Edit.SetTextByID("ctl00_ContentPlaceHolder1_PasswordTextBox", Encryption.SimpleDecryptWithPassword(MainWindow.CurConfig["password"], CurConfig["username"], myBytes.Length));
                    WebAutomationToolkit.Web.Button.ClickByID("ctl00_ContentPlaceHolder1_SubmitButton");
                    
                    UpdateStatus("BlueSource");
      
                    if (WebAutomationToolkit.Web.Sync.SyncByID("search-bar", 30))
                    {
                        stopwatch.Stop();
                        if (FailedLastTime)
                        {
                            SendEmail(CurConfig["tolist"], "BlueSource is back up!", "BlueSource is now accessable using username: " + CurConfig["username"] + L + CurConfig["url"] + L + "Loged in from on machine located @ " + MainMethods.GetPublicIP() + L + L + BCVersion);
                        }
                        LogFromThread("BlueSource login was successfull: " + stopwatch.Elapsed.ToString());
                        ConfigData.WriteToLog("Login was successfull with username: " + CurConfig["username"] + ": " + stopwatch.Elapsed.ToString());
                        FailedLastTime = false;
                        NumberOfPasses++;
                    }
                    else
                    {
                        LogFromThread("BlueSource login failed.");
                        Int64 EI = Int64.Parse(CurConfig["emailintervalinseconds"]) * 1000;
                        Int64 TimeElapsed = CurrentEPOCH - LastSentAt;

                        if (TimeElapsed > EI)
                        {
                            SendEmail(CurConfig["tolist"], "BlueSource is Down", "BlueSource is not accessable using username: " + CurConfig["username"] + L + CurConfig["url"] + L + "Detected on machine located @ " + MainMethods.GetPublicIP() + L + L + BCVersion);
                            LastSentAt = MainMethods.GetEPOCHTimeInMilliSeconds();
                        }
                        FailedLastTime = true;
                        ConfigData.WriteToLog("Login was NOT successfull with username: " + CurConfig["username"] + " emails have been sent: " + CurConfig["tolist"]);
                        NumberOfFailures++;
                    }

                    ProgressFromThread(0, URLCount + 1, CurrentProgress++);
       
                    #endregion
                    #region Check Generic Sites
                    for (int i = 0; i < URLCount; i++)
                    {
                        if (!isStarted)
                        {
                            Stopper();
                            break;
                        }
                        string[] aurlandids = urlandids[i].Split(';');

                        string CheckURL = aurlandids[0];
                        string CheckID = aurlandids[1];
                        UpdateStatus("Currently checking: " + CheckURL);
                        LogFromThread("Attempting to navigate to " + CheckURL);
                        stopwatch.Reset();
                        stopwatch.Start();
                        WebAutomationToolkit.Web.NavigateToURL(CheckURL);

                        UpdateStatus(CheckURL);

                        if (WebAutomationToolkit.Web.Sync.SyncByID(CheckID, 30))
                        {
                             stopwatch.Stop();
                           
                            if (aFailedLastTime[i])
                            {
                                SendEmail(CurConfig["tolist"], CheckURL + " is back up!", CheckURL + " is now accessable. " + L + "Viewed from on machine located @ " + MainMethods.GetPublicIP() + L + L + BCVersion);
                            }
                            LogFromThread("Navigating to " + CheckURL + " was successfull: " + stopwatch.Elapsed.ToString());
                            ConfigData.WriteToLog("Navigating to " + CheckURL + " was successfull: " + stopwatch.Elapsed.ToString());

                            aFailedLastTime[i] = false;
                            NumberOfPasses++;
                        }
                        else
                        {
                            stopwatch.Stop();
                            LogFromThread("Navigating to " + CheckURL + " failed.");
                            Int64 EI = Int64.Parse(CurConfig["emailintervalinseconds"]) * 1000;
                            Int64 TimeElapsed = CurrentEPOCH - aLastSentAt[i];

                            if (TimeElapsed > EI)
                            {
                                SendEmail(CurConfig["tolist"], CheckURL + " is Down", CheckURL + " is not accessable." + L + "Detected on machine located @ " + MainMethods.GetPublicIP() + L + L + BCVersion);
                                aLastSentAt[i] = MainMethods.GetEPOCHTimeInMilliSeconds();
                            }
                            aFailedLastTime[i] = true;
                            ConfigData.WriteToLog("Navigating to " + CheckURL + " failed. emails have been sent: " + CurConfig["tolist"]);
                            NumberOfFailures++;
                        }
                        ProgressFromThread(0, URLCount + 1, CurrentProgress++);
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

                //Clean up
                WebAutomationToolkit.Web.WebDriver = null;
                System.GC.Collect();
                MainMethods.KillProcessByName("phantomjs");
                MainMethods.KillProcessByName("conhost");
                //MainMethods.RunCMD(@"Taskkill /IM phantomjs.exe /F");
                Waiter();

            }

        }

        public MainWindow()
        {
            InitializeComponent();
            ConfigData CurCon = new ConfigData();
            CurConfig = CurCon.GetConfigData();
            MinimizeToTray.Enable(this);

            if (CurConfig["autostart"] == "TRUE")
            {
                StartTime = DateTime.Now;
                CheckingThread = new Thread(new ThreadStart(Checker));
                isStarted = true;
                CheckingThread.Start();
                btStartStop.Content = "Stop";
                LogFromThread("BlueSource checking service has started.");
                ConfigData.WriteToLog("BlueSource checking service has started.");
            }

            urlandids = CurConfig["urlandids"].Split(',');
 
            URLCount = urlandids.Length;
            aFailedLastTime = new bool[URLCount];
            aLastSentAt = new Int64[URLCount];

            BCVersion = this.Title;

            pbMainLoad.Maximum = URLCount + 1;
            pbMainLoad.Minimum = 0;
            


        }

        #region UI
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
                CurConfig = null;
                ConfigData CurCon = new ConfigData();
                CurConfig = CurCon.GetConfigData();
                CheckingThread = new Thread(new ThreadStart(Checker));
                isStarted = true;
                CheckingThread.Start();
                btStartStop.Content = "Stop";

                LogFromThread("BlueSource checking service is starting.");
                ConfigData.WriteToLog("BlueSource checking service is starting.");
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

        private void wnBlueCheck_Closed(object sender, EventArgs e)
        {

        }

        private void wnBlueCheck_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion
    }
}
        
 
