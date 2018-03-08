using System;
using System.Windows.Forms;

namespace NLog.Targets.NetworkJSON.LoadTester
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new SimulatedLoggingLoadTeser());
            Application.Run(new ActualLoggingLoadTeser());
        }
    }
}
