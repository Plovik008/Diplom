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

        public ExhibitsListPage(DatabaseService dbService, User currentUser)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            LoadExhibits();
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectionChanged += CmbStatusFilter_SelectionChanged;
        }

        private void LoadExhibits()
        {
            allExhibits = dbService.GetAllExhibits();
            dgvExhibits.ItemsSource = allExhibits;
        }

        private void ApplyFilters()
        {
            if (allExhibits == null) return;

            var filtered = allExhibits.AsEnumerable();

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(e =>
                    e.InventoryNumber.ToLower().Contains(searchText) ||
                    e.Name.ToLower().Contains(searchText) ||
                    (e.Category?.ToLower().Contains(searchText) ?? false));
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

            dgvExhibits.ItemsSource = filtered.ToList();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbStatusFilter.SelectedIndex = 0;
            LoadExhibits();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ExhibitEditPage page = new ExhibitEditPage(dbService);
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
            ExhibitEditPage page = new ExhibitEditPage(dbService, exhibit.Id);
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
                await System.Threading.Tasks.Task.Run(() => dbService.DeleteExhibit(exhibit.Id));
                LoadExhibits();
                MessageBox.Show("Экспонат удален");
            }
        }
    }
}