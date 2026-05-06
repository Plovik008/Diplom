using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class ReturnExhibitPage : Page
    {
        private DatabaseService dbService;
        private User currentUser;
        private List<Issue> activeIssues;
        private List<Issue> filteredIssues;
        private int? currentTeacherId;

        public ReturnExhibitPage(DatabaseService dbService, User currentUser)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            currentTeacherId = dbService.GetTeacherIdByUser(currentUser);

            if (currentUser.IsTeacher)
            {
                btnReturn.Visibility = Visibility.Collapsed;
            }

            LoadData();
        }

        private void LoadData()
        {
            if (currentUser.IsTeacher && !currentTeacherId.HasValue)
            {
                activeIssues = new List<Issue>();
            }
            else
            {
                activeIssues = dbService.GetAllIssues(true, currentUser.IsTeacher ? currentTeacherId : null);
            }
            filteredIssues = new List<Issue>(activeIssues);
            dgvIssuedExhibits.ItemsSource = filteredIssues;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                filteredIssues = new List<Issue>(activeIssues);
            }
            else
            {
                filteredIssues = activeIssues.Where(i =>
                    i.ExhibitName.ToLower().Contains(searchText) ||
                    i.ExhibitInventoryNumber.ToLower().Contains(searchText) ||
                    i.TeacherName.ToLower().Contains(searchText)
                ).ToList();
            }

            dgvIssuedExhibits.ItemsSource = filteredIssues;
        }

        private async void BtnReturn_Click(object sender, RoutedEventArgs e)
        {
            if (dgvIssuedExhibits.SelectedItem == null)
            {
                MessageBox.Show("Выберите экспонат для возврата");
                return;
            }

            Issue issue = (Issue)dgvIssuedExhibits.SelectedItem;

            MessageBoxResult confirm = MessageBox.Show($"Вернуть экспонат \"{issue.ExhibitName}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            btnReturn.IsEnabled = false;
            btnReturn.Content = "Оформление...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                dbService.ReturnExhibit(issue.Id, currentUser);
            });

            MessageBox.Show("Экспонат возвращен");

            btnReturn.IsEnabled = true;
            btnReturn.Content = "ВЕРНУТЬ";
            LoadData();
            txtSearch.Text = "";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            txtSearch.Text = "";
        }
    }
}
