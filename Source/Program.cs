using System;
using System.Runtime.InteropServices;

namespace eft_dma_radar
{
    internal static class Program
    {
        private static Mutex _mutex;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode; // allow russian chars
            try
            {
                _mutex = new Mutex(true, "9A19103F-16F7-4668-BE54-9A1E7A4F7556", out bool singleton);
                if (singleton)
                {
                    TarkovMarketManager.Startup = true;
                    ApplicationConfiguration.Initialize();
					Application.Run(new MainForm());
                }
                else
                {
                    throw new Exception("The Application Is Already Running!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "EFT Radar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();
    }
}
