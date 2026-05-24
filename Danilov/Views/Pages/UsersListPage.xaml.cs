using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class UsersListPage : Page
    {
        private readonly DatabaseService dbService;
        private readonly User currentUser;
        private List<User> allUsers;

        public UsersListPage(DatabaseService dbService, User currentUser)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            dbService.DataChanged += OnDataChanged;
            LoadUsers();

            if (!currentUser.IsAdmin && !currentUser.IsEmployee)
            {
                btnAdd.Visibility = Visibility.Collapsed;
                btnEdit.Visibility = Visibility.Collapsed;
                btnDelete.Visibility = Visibility.Collapsed;
            }
        }

        private void OnDataChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtSearch.Text = "";
                LoadUsers();
            }));
        }

        private void LoadUsers()
        {
            allUsers = dbService.GetTeacherUsers();
            ApplySearch();
        }

        private void ApplySearch()
        {
            string searchText = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgvUsers.ItemsSource = allUsers;
                return;
            }

            dgvUsers.ItemsSource = allUsers.Where(user =>
                user.FullName.ToLower().Contains(searchText) ||
                user.Username.ToLower().Contains(searchText)).ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadUsers();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var page = new UserEditPage(dbService, currentUser);
            page.UserSaved += (s, args) => LoadUsers();
            NavigationService.Navigate(page);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgvUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования");
                return;
            }

            var user = (User)dgvUsers.SelectedItem;
            var page = new UserEditPage(dbService, currentUser, user.Id);
            page.UserSaved += (s, args) => LoadUsers();
            NavigationService.Navigate(page);
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgvUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для удаления");
                return;
            }

            var user = (User)dgvUsers.SelectedItem;
            var result = MessageBox.Show($"Удалить пользователя \"{user.FullName}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            await System.Threading.Tasks.Task.Run(() => dbService.DeleteTeacherUser(user.Id, currentUser));
            LoadUsers();
            MessageBox.Show("Пользователь удален");
        }
    }
}
