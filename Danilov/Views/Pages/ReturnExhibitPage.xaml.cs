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
        private List<Issue> activeIssues;
        private List<Issue> filteredIssues;

        public ReturnExhibitPage(DatabaseService dbService)
        {
            InitializeComponent();
            this.dbService = dbService;
            LoadData();
        }

        private void LoadData()
        {
            activeIssues = dbService.GetAllIssues(true);
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
                dbService.ReturnExhibit(issue.Id);
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