using System;
using System.Windows.Forms;

namespace VirtualFitter
{
    static class Program
    {
        /// <summary>
        /// Virtual Fitter - Automatic Dimension Creation Tool
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
