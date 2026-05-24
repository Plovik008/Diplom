using System;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class UserEditPage : Page
    {
        private readonly DatabaseService dbService;
        private readonly User currentUser;
        private readonly int editUserId;
        public event EventHandler UserSaved;

        public UserEditPage(DatabaseService dbService, User currentUser, int userId = -1)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            editUserId = userId;

            if (userId == -1)
            {
                lblTitle.Text = "Создание пользователя-преподавателя";
            }
            else
            {
                lblTitle.Text = "Редактирование пользователя-преподавателя";
                LoadUser();
            }
        }

        private void LoadUser()
        {
            var user = dbService.GetTeacherUserById(editUserId);
            if (user == null)
                return;

            txtFullName.Text = user.FullName;
            txtUsername.Text = user.Username;
            txtPassword.Text = user.Password;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var user = new User
            {
                Id = editUserId,
                FullName = txtFullName.Text,
                Username = txtUsername.Text,
                Password = txtPassword.Text,
                Role = "Teacher"
            };

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    if (editUserId == -1)
                        dbService.AddTeacherUser(user, currentUser);
                    else
                        dbService.UpdateTeacherUser(user, currentUser);
                });

                MessageBox.Show("Пользователь сохранен");
                UserSaved?.Invoke(this, EventArgs.Empty);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
