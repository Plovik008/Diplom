using System.Windows;
using MuseumAccountingSystem.Views;

namespace MuseumAccountingSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}