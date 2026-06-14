using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

                MessageBox.Show(
                    $"Ошибка подключения к базе данных:\n{errorDetails}",
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                dbService = null;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            ResetBorders();

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Введите логин");
                txtUsername.BorderBrush = Brushes.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                txtPassword.BorderBrush = Brushes.Red;
                return;
            }

            if (dbService == null)
            {
                ShowError("Ошибка подключения к базе данных");
                return;
            }

            btnLogin.IsEnabled = false;
            btnLogin.Content = "Проверка...";

            try
            {
                var user = dbService.AuthenticateUser(username, password);

                if (user != null)
                {
                    MainWindow mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    Hide();
                    return;
                }

                txtUsername.BorderBrush = Brushes.Red;
                txtPassword.BorderBrush = Brushes.Red;

                ShowError("Неверный логин или пароль");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }

            btnLogin.IsEnabled = true;
            btnLogin.Content = "ВОЙТИ";
        }

        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;
            ResetBorders();
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;
            ResetBorders();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void ResetBorders()
        {
            txtUsername.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
            txtPassword.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}