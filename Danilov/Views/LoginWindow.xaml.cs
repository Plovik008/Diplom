using System;
using System.Windows;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views
{
    public partial class LoginWindow : Window
    {
        private DatabaseService dbService;

        public LoginWindow()
        {
            InitializeComponent();
            try
            {
                dbService = new DatabaseService();
            }
            catch (Exception ex)
            {
                string errorDetails = ex.Message;
                if (ex.InnerException != null)
                    errorDetails += "\n\nВнутренняя ошибка: " + ex.InnerException.Message;

                MessageBox.Show($"Ошибка подключения к базе данных:\n{errorDetails}\n\nПроверьте:\n1. PostgreSQL запущен\n2. Логин и пароль верны\n3. База данных museumdb существует",
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                dbService = null;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                txtError.Text = "Введите логин";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                txtError.Text = "Введите пароль";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            if (dbService == null)
            {
                txtError.Text = "Ошибка подключения к базе данных";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            btnLogin.IsEnabled = false;
            btnLogin.Content = "Проверка...";
            txtError.Visibility = Visibility.Collapsed;

            try
            {
                var user = dbService.AuthenticateUser(username, password);

                if (user != null)
                {
                    MainWindow mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    this.Hide();
                }
                else
                {
                    txtError.Text = "Неверный логин или пароль";
                    txtError.Visibility = Visibility.Visible;
                    btnLogin.IsEnabled = true;
                    btnLogin.Content = "ВОЙТИ";
                }
            }
            catch (Exception ex)
            {
                txtError.Text = "Ошибка: " + ex.Message;
                txtError.Visibility = Visibility.Visible;
                btnLogin.IsEnabled = true;
                btnLogin.Content = "ВОЙТИ";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}