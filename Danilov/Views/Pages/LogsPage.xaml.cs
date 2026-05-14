using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class LogsPage : Page
    {
        private DatabaseService dbService;
        private List<UserActionLog> allLogs;
        private List<UserActionLog> filteredLogs;

        public LogsPage(DatabaseService dbService)
        {
            InitializeComponent();
            this.dbService = dbService;
            dbService.DataChanged += OnDataChanged;
            LoadData();
        }

        private void OnDataChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtSearch.Text = "";
                LoadData();
            }));
        }

        private void LoadData()
        {
            allLogs = dbService.GetAllLogs();
            filteredLogs = new List<UserActionLog>(allLogs);
            dgvLogs.ItemsSource = filteredLogs;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                filteredLogs = new List<UserActionLog>(allLogs);
            }
            else
            {
                filteredLogs = allLogs.Where(l =>
                    l.Username.ToLower().Contains(searchText) ||
                    l.UserRole.ToLower().Contains(searchText) ||
                    l.Action.ToLower().Contains(searchText) ||
                    l.TargetType.ToLower().Contains(searchText) ||
                    l.TargetName.ToLower().Contains(searchText) ||
                    (l.Details?.ToLower().Contains(searchText) ?? false)
                ).ToList();
            }

            dgvLogs.ItemsSource = filteredLogs;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();
        }
    }
}