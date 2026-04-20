using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class IssuesJournalPage : Page
    {
        private DatabaseService dbService;
        private List<Issue> allIssues;
        private List<Issue> filteredIssues;

        public IssuesJournalPage(DatabaseService dbService)
        {
            InitializeComponent();
            this.dbService = dbService;
            LoadData();
        }

        private void LoadData()
        {
            allIssues = dbService.GetAllIssues();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            filteredIssues = new List<Issue>(allIssues);

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredIssues = filteredIssues.Where(i =>
                    i.ExhibitName.ToLower().Contains(searchText) ||
                    i.ExhibitInventoryNumber.ToLower().Contains(searchText) ||
                    i.TeacherName.ToLower().Contains(searchText) ||
                    (i.Purpose?.ToLower().Contains(searchText) ?? false)
                ).ToList();
            }

            ComboBoxItem selected = cmbStatusFilter.SelectedItem as ComboBoxItem;
            if (selected != null)
            {
                string statusFilter = selected.Content.ToString();
                if (statusFilter == "Выданы")
                    filteredIssues = filteredIssues.Where(i => i.Status == "Выдан").ToList();
                else if (statusFilter == "Возвращены")
                    filteredIssues = filteredIssues.Where(i => i.Status == "Возвращен").ToList();
            }

            dgvJournal.ItemsSource = filteredIssues;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbStatusFilter.SelectedIndex = 0;
            LoadData();
        }
    }
}