using System;
using System.Windows;
using System.Windows.Input;
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
            try
            {
                InitializeComponent();
                currentUser = user;
                dbService = new DatabaseService();

                dbService.CleanupOrphanedPhotos();

            string roleText = "";
            if (user.Role == "Admin") roleText = "Администратор";
            else if (user.Role == "Employee") roleText = "Сотрудник музея";
            else if (user.Role == "Teacher") roleText = "Преподаватель";

            txtUserInfo.Text = $"{roleText} | {user.FullName}";
            txtStatus.Text = "Система готова к работе";

            if (user.IsTeacher)
            {
                btnIssue.Visibility = Visibility.Collapsed;
                btnUsers.Visibility = Visibility.Collapsed;
                usersSeparator.Visibility = Visibility.Collapsed;
                btnTeachers.Visibility = Visibility.Collapsed;
                btnStatistics.Visibility = Visibility.Collapsed;
            }

            if (user.IsAdmin)
            {
                usersSeparator.Visibility = Visibility.Visible;
                btnUsers.Visibility = Visibility.Visible;
                logsSeparator.Visibility = Visibility.Visible;
                btnLogs.Visibility = Visibility.Visible;
                backupSeparator.Visibility = Visibility.Visible;
                btnBackup.Visibility = Visibility.Visible;
            }
            else if (user.IsEmployee)
            {
                usersSeparator.Visibility = Visibility.Visible;
                btnUsers.Visibility = Visibility.Visible;
                logsSeparator.Visibility = Visibility.Visible;
                btnLogs.Visibility = Visibility.Visible;
            }

            MainFrame.Navigate(new ExhibitsListPage(dbService, currentUser));

            this.ContentRendered += MainWindow_ContentRendered;
                this.KeyDown += MainWindow_KeyDown;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка в конструкторе MainWindow:\n" + ex.Message + "\n\nStackTrace:\n" + ex.StackTrace,
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_ContentRendered(object sender, System.EventArgs e)
        {
            ShowOverdueNotifications();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                ShowHelp();
            }
            else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (currentUser.IsAdmin)
                {
                    MainFrame.Navigate(new ExhibitEditPage(dbService, currentUser));
                    txtStatus.Text = "Добавление нового экспоната";
                }
            }
            else if (e.Key == Key.F5)
            {
                RefreshCurrentPage();
                txtStatus.Text = "Данные обновлены";
            }
        }

        private void ShowHelp()
        {
            string helpText = "Горячие клавиши:\n\nF1 - Справка\nCtrl+N - Новый экспонат\nF5 - Обновить\n\n";
            helpText += $"Ваша роль: ";
            if (currentUser.IsAdmin) helpText += "Администратор (полный доступ)";
            else if (currentUser.IsEmployee) helpText += "Сотрудник музея (выдача и возврат)";
            else helpText += "Преподаватель (только просмотр)";

            MessageBox.Show(helpText, "Помощь", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshCurrentPage()
        {
            var currentPage = MainFrame.Content;
            if (currentPage is ExhibitsListPage)
            {
                MainFrame.Navigate(new ExhibitsListPage(dbService, currentUser));
            }
            else if (currentPage is TeachersListPage)
            {
                MainFrame.Navigate(new TeachersListPage(dbService, currentUser));
            }
            else if (currentPage is UsersListPage)
            {
                MainFrame.Navigate(new UsersListPage(dbService, currentUser));
            }
            else if (currentPage is IssuesJournalPage)
            {
                MainFrame.Navigate(new IssuesJournalPage(dbService, currentUser));
            }
            else if (currentPage is StatisticsPage)
            {
                MainFrame.Navigate(new StatisticsPage(dbService, currentUser));
            }
            else if (currentPage is CalendarPage)
            {
                MainFrame.Navigate(new CalendarPage(dbService));
            }
            else if (currentPage is IssueExhibitPage)
            {
                MainFrame.Navigate(new IssueExhibitPage(dbService, currentUser));
            }
            else if (currentPage is ReturnExhibitPage)
            {
                MainFrame.Navigate(new ReturnExhibitPage(dbService, currentUser));
            }
        }

        private void ShowOverdueNotifications()
        {
            try
            {
                if (!currentUser.IsTeacher)
                    return;

                var currentTeacherId = dbService.GetTeacherIdByUser(currentUser);
                if (!currentTeacherId.HasValue)
                    return;

                var activeIssues = dbService.GetAllIssues(true, currentTeacherId);
                var overdueCount = 0;

                foreach (var issue in activeIssues)
                {
                    if (issue.IsOverdue)
                    {
                        overdueCount++;
                    }
                }

                if (overdueCount > 0)
                {
                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        var result = MessageBox.Show(this, $"Обнаружено {overdueCount} просроченных экспонатов.\nПоказать подробности?",
                            "Внимание! Просроченные экспонаты",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            NotificationsWindow notifications = new NotificationsWindow(dbService, this, currentUser);
                            notifications.Owner = this;
                            notifications.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            notifications.ShowDialog();
                        }
                    }));
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this,
                    $"Ошибка при загрузке просроченных экспонатов: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void NavigateToReturnPage()
        {
            MainFrame.Navigate(new ReturnExhibitPage(dbService, currentUser));
            txtStatus.Text = "Возврат просроченных экспонатов";
        }

        private void MenuExhibits_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ExhibitsListPage(dbService, currentUser));
            txtStatus.Text = currentUser.IsTeacher ? "Просмотр страницы \"Мои выдачи\"" : "Просмотр списка экспонатов";
        }

        private void MenuIssue_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new IssueExhibitPage(dbService, currentUser));
            txtStatus.Text = "Выдача экспоната преподавателю";
        }

        private void MenuReturn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReturnExhibitPage(dbService, currentUser));
            txtStatus.Text = "Возврат экспоната в музей";
        }

        private void MenuJournal_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new IssuesJournalPage(dbService, currentUser));
            txtStatus.Text = currentUser.IsTeacher ? "Просмотр своего журнала выдачи" : "Просмотр журнала выдачи";
        }

        private void MenuTeachers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new TeachersListPage(dbService, currentUser));
            txtStatus.Text = "Управление списком преподавателей";
        }

        private void MenuUsers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UsersListPage(dbService, currentUser));
            txtStatus.Text = "Управление пользователями-преподавателями";
        }

        private void MenuStatistics_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new StatisticsPage(dbService, currentUser));
            txtStatus.Text = currentUser.IsTeacher ? "Просмотр своей статистики" : "Просмотр статистики и отчетов";
        }

        private void MenuCalendar_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CalendarPage(dbService));
            txtStatus.Text = "Просмотр календаря возвратов";
        }

        private void MenuLogs_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new LogsPage(dbService));
            txtStatus.Text = "Просмотр журнала действий";
        }

        private void MenuBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Выберите папку для сохранения резервной копии";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var result = MessageBox.Show("Создать резервную копию базы данных?", 
                        "Резервная копия", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        string backupFile = dbService.BackupDatabase(dialog.SelectedPath);
                        if (backupFile != null)
                        {
                            MessageBox.Show($"Резервная копия создана:\n{backupFile}", 
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании резервной копии: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
