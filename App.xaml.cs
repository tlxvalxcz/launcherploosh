using System.Windows;

namespace PlooshLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            new LoginWindow().Show();   // gate first; it opens MainWindow on success
        }
    }
}