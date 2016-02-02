using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace BlueChecker
{
    class MainMethods
    {
        public static string GetPublicIP()
        {
            String direction = "";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                direction = stream.ReadToEnd();
            }

            //Search for the ip in the html
            int first = direction.IndexOf("Address: ") + 9;
            int last = direction.LastIndexOf("</body>");
            direction = direction.Substring(first, last - first);

            return direction;
        }

        public static Int64 GetEPOCHTimeInMilliSeconds()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Convert.ToInt64(t.TotalMilliseconds);
        }

        public static void RunCMD(string CMD)
        {
            string strCmdText;
            strCmdText = "/C " + CMD; //copy /b Image1.jpg + Archive.rar Image2.jpgTEST";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
        }

        public static void KillProcessByName(string ProcessName)
        {
            try
            {
                foreach (Process proc in Process.GetProcessesByName(ProcessName))
                {
                    proc.Kill();
                }
            }
            catch (Exception ex)
            {
               
            }

        }
    }
  
}


    

