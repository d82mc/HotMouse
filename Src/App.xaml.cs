using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HotMouse_2020
{
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            if (e.Args.Count() > 0)
            {
                string filePath = e.Args[0];
                mainWindow.LoadFile(filePath);
            }
        }
    }
}
