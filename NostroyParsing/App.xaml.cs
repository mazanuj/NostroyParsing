using System;
using System.Windows;
using NostroyParsing.Properties;

namespace NostroyParsing
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (DateTime.Now >= new DateTime(2016, 07, 13, 19, 00, 00))
            {
                MessageBox.Show("Parsing error: Signature 0x0230923742984");
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}