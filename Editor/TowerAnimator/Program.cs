using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TowerAnimator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            if (args.Count() == 1)
            {
                MainForm mainForm = new MainForm();
                mainForm.OpenFile(args[0]);
                Application.Run(mainForm);
            }
            else
            {
                Application.Run(new MainForm());
            }
        }
    }
}
