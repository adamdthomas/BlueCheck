using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlueChecker
{
    public static class MainMethods
    {
        public static Thread thread = new Thread(new ThreadStart(Checker));
        public static bool isStarted = false;

        public static void Checker()
        {
            while (isStarted)
            {

                Thread.Sleep(1000);

                try
                {
                    
                    // do any background work
                }
                catch (Exception ex)
                {
                    // log errors
                }
            }

        }


    }
}
