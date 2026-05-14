using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class TeachersListPage : Page
    {
        private DatabaseService dbService;
        private User currentUser;
        private List<Teacher> allTeachers;

        public TeachersListPage(DatabaseService dbService, User currentUser)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            dbService.DataChanged += OnDataChanged;
            LoadTeachers();

            if (!currentUser.IsAdmin)
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
                LoadTeachers();
            }));
        }

        private void LoadTeachers()
        {
            allTeachers = dbService.GetAllTeachers();
            ApplySearch();
        }

        private void ApplySearch()
        {
            string searchText = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                dgvTeachers.ItemsSource = allTeachers;
            }
            else
            {
                var filtered = allTeachers.Where(t =>
                    t.FullName.ToLower().Contains(searchText) ||
                    (t.Department?.ToLower().Contains(searchText) ?? false) ||
                    (t.Email?.ToLower().Contains(searchText) ?? false)
                ).ToList();

                dgvTeachers.ItemsSource = filtered;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadTeachers();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            TeacherEditPage page = new TeacherEditPage(dbService, currentUser);
            page.TeacherSaved += (s, args) => LoadTeachers();
            NavigationService.Navigate(page);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgvTeachers.SelectedItem == null)
            {
                MessageBox.Show("Выберите преподавателя для редактирования");
                return;
            }

            Teacher teacher = (Teacher)dgvTeachers.SelectedItem;
            TeacherEditPage page = new TeacherEditPage(dbService, currentUser, teacher.Id);
            page.TeacherSaved += (s, args) => LoadTeachers();
            NavigationService.Navigate(page);
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgvTeachers.SelectedItem == null)
            {
                MessageBox.Show("Выберите преподавателя для удаления");
                return;
            }

            Teacher teacher = (Teacher)dgvTeachers.SelectedItem;

            MessageBoxResult result = MessageBox.Show($"Удалить преподавателя \"{teacher.FullName}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await System.Threading.Tasks.Task.Run(() => dbService.DeleteTeacher(teacher.Id, currentUser));
                LoadTeachers();
                MessageBox.Show("Преподаватель удален");
            }
        }
    }
}