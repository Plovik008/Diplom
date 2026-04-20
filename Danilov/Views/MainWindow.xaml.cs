using System.Windows;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;
using MuseumAccountingSystem.Views.Pages;

namespace MuseumAccountingSystem.Views
{
    public partial class MainWindow : Window
    {
        private DatabaseService dbService;
        private User currentUser;

        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            dbService = new DatabaseService();

            string roleText = user.Role == "Admin" ? "👑 Администратор" : "👤 Пользователь";
            txtUserInfo.Text = $"{roleText} | {user.FullName}";
            txtStatus.Text = "✅ Система готова к работе";

            MainFrame.Navigate(new ExhibitsListPage(dbService, currentUser));
        }

        private void MenuExhibits_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ExhibitsListPage(dbService, currentUser));
            txtStatus.Text = "📋 Просмотр списка экспонатов";
        }

        private void MenuIssue_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new IssueExhibitPage(dbService));
            txtStatus.Text = "📤 Выдача экспоната преподавателю";
        }

        private void MenuReturn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReturnExhibitPage(dbService));
            txtStatus.Text = "📥 Возврат экспоната в музей";
        }

        private void MenuJournal_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new IssuesJournalPage(dbService));
            txtStatus.Text = "📊 Просмотр журнала выдачи";
        }

        private void MenuTeachers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new TeachersListPage(dbService, currentUser));
            txtStatus.Text = "👨‍🏫 Управление списком преподавателей";
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }
    }
}