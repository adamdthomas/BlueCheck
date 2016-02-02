using System;
using System.Collections.Generic;
using System.IO;

namespace BlueChecker
{
    class ConfigData
    {

        #region ConfigData

        public static Dictionary<string, string> dicConfig = new Dictionary<string, string>();
        public static string ConfigPath = @"C:\Logs\BCconfig.txt";
        private static string LogPath = @"C:\Logs\";
        private static string LogName = ("BCClientLog-" + DateTime.Now.ToString("D") + ".txt").Replace(@"/", ".").Replace(":", ".");
        public static string LogFullPath = LogPath + LogName;

        public Dictionary<string, string> GetConfigData()
        {
            //Set up config file with defualt values if it doesnt exist
            System.IO.Directory.CreateDirectory(LogPath);
            if (!File.Exists(ConfigPath))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(ConfigPath))
                {
                    sw.Close();
                }

                //Load defaults for new machine
                dicConfig["url"] = "https://bluesource.orasi.com";
                dicConfig["username"] = "usernamehere";
                dicConfig["password"] = "passwordhere";
                dicConfig["gmailusername"] = "gmailusernamehere";
                dicConfig["gmailpassword"] = "gmailpasswordhere";
                dicConfig["tolist"] = "CommaDelimitedToEmailList";
                dicConfig["cycletimeinseconds"] = "60";
                dicConfig["emailintervalinseconds"] = "600";
                SetConfigData();

            }


            //Pull all values from the config file. 
            System.Collections.Generic.IEnumerable<String> lines = File.ReadLines(ConfigPath);


            foreach (var item in lines)
            {
                string[] KeyPair = item.Split('~');
                if (dicConfig.ContainsKey(KeyPair[0]))
                {
                    dicConfig[KeyPair[0]] = KeyPair[1];
                }
                else
                {
                    dicConfig.Add(KeyPair[0], KeyPair[1]);
                }

            }
            return dicConfig;

        }



        public static void SetConfigData()
        {
            //Delete/erase old file
            File.WriteAllText(ConfigPath, String.Empty);

            using (StreamWriter configwriter = File.AppendText(ConfigPath))
            {
                foreach (var pair in dicConfig)
                {
                    configwriter.WriteLine(pair.Key + "~" + pair.Value);
                }
                configwriter.Close();
            }
        }

        public static void WriteToLog(string Message)
        {
            Message = DateTime.Now.ToString("MM/dd/yy HH:mm:ss") + " " + Message;

            try
            {
                using (StreamWriter sw = File.AppendText(LogFullPath))
                {
                    sw.WriteLine(Message);
                    sw.Close();
                }
            }
            catch (Exception l)
            {
                l.ToString();
            }


        }


        #endregion

    }

   
}
