using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class ExhibitsListPage : Page
    {
        private DatabaseService dbService;
        private User currentUser;
        private List<Exhibit> allExhibits;
        private List<Exhibit> filteredExhibits;
        private int currentPage = 1;
        private int pageSize = 20;
        private int totalPages = 1;
        private CsvExportService csvExport;
        private int? currentTeacherId;

        public ExhibitsListPage(DatabaseService dbService, User currentUser)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            csvExport = new CsvExportService();
            currentTeacherId = dbService.GetTeacherIdByUser(currentUser);

            if (!currentUser.IsAdmin && !currentUser.IsEmployee)
            {
                btnAdd.Visibility = Visibility.Collapsed;
                btnEdit.Visibility = Visibility.Collapsed;
                btnDelete.Visibility = Visibility.Collapsed;
                txtPageTitle.Text = "Мои выдачи";
            }

            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectionChanged += CmbStatusFilter_SelectionChanged;

            LoadExhibits();
        }

        private void LoadExhibits()
        {
            allExhibits = dbService.GetAllExhibits();
            if (allExhibits == null)
            {
                allExhibits = new List<Exhibit>();
            }

            if (currentUser.IsTeacher)
            {
                if (!currentTeacherId.HasValue)
                {
                    allExhibits = new List<Exhibit>();
                }
                else
                {
                    var myIssueExhibitIds = dbService.GetAllIssues(true, currentTeacherId.Value)
                    .Select(i => i.ExhibitId)
                    .Distinct()
                    .ToHashSet();

                    allExhibits = allExhibits
                        .Where(e => myIssueExhibitIds.Contains(e.Id))
                        .ToList();
                }
            }

            filteredExhibits = new List<Exhibit>(allExhibits);
            currentPage = 1;
            UpdatePagination();
        }

        private void ApplyFilters()
        {
            if (allExhibits == null)
            {
                allExhibits = new List<Exhibit>();
            }

            var filtered = allExhibits.AsEnumerable();

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(e =>
                    e.InventoryNumber.ToLower().Contains(searchText) ||
                    e.Name.ToLower().Contains(searchText) ||
                    (e.Category?.ToLower().Contains(searchText) ?? false) ||
                    (e.Material?.ToLower().Contains(searchText) ?? false) ||
                    (e.ResponsiblePerson?.ToLower().Contains(searchText) ?? false) ||
                    (e.Source?.ToLower().Contains(searchText) ?? false));
            }

            ComboBoxItem selected = cmbStatusFilter.SelectedItem as ComboBoxItem;
            if (selected != null)
            {
                string statusFilter = selected.Content.ToString();
                if (statusFilter == "В наличии")
                    filtered = filtered.Where(e => e.CurrentStatus == "В наличии");
                else if (statusFilter == "Выданы")
                    filtered = filtered.Where(e => e.CurrentStatus == "Выдан");
            }

            filteredExhibits = filtered.ToList();
            currentPage = 1;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            if (filteredExhibits == null)
            {
                filteredExhibits = new List<Exhibit>();
            }

            if (filteredExhibits.Count == 0)
            {
                totalPages = 1;
                dgvExhibits.ItemsSource = filteredExhibits;
                txtPageInfo.Text = "Страница 0 из 0 (всего 0 записей)";
                return;
            }

            totalPages = (int)Math.Ceiling((double)filteredExhibits.Count / pageSize);
            if (totalPages == 0) totalPages = 1;

            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            var pagedData = filteredExhibits.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
            dgvExhibits.ItemsSource = pagedData;

            txtPageInfo.Text = $"Страница {currentPage} из {totalPages} (всего {filteredExhibits.Count} записей)";
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            List<Exhibit> allCurrentExhibits = currentUser.IsTeacher
                ? filteredExhibits
                : dbService.GetAllExhibits();
            csvExport.ExportExhibitsToCsv(allCurrentExhibits);
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            UpdatePagination();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                UpdatePagination();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                UpdatePagination();
            }
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            currentPage = totalPages;
            UpdatePagination();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbStatusFilter.SelectedIndex = 0;
            currentPage = 1;
            LoadExhibits();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ExhibitEditPage page = new ExhibitEditPage(dbService, currentUser);
            page.ExhibitSaved += (s, args) => LoadExhibits();
            NavigationService.Navigate(page);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgvExhibits.SelectedItem == null)
            {
                MessageBox.Show("Выберите экспонат для редактирования");
                return;
            }

            Exhibit exhibit = (Exhibit)dgvExhibits.SelectedItem;
            ExhibitEditPage page = new ExhibitEditPage(dbService, currentUser, exhibit.Id);
            page.ExhibitSaved += (s, args) => LoadExhibits();
            NavigationService.Navigate(page);
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgvExhibits.SelectedItem == null)
            {
                MessageBox.Show("Выберите экспонат для удаления");
                return;
            }

            Exhibit exhibit = (Exhibit)dgvExhibits.SelectedItem;
            MessageBoxResult result = MessageBox.Show($"Удалить экспонат \"{exhibit.Name}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await System.Threading.Tasks.Task.Run(() => dbService.DeleteExhibit(exhibit.Id, currentUser));
                LoadExhibits();
                MessageBox.Show("Экспонат удален");
            }
        }
    }
}
