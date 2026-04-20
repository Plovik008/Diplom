using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class IssueExhibitPage : Page
    {
        private DatabaseService dbService;

        public IssueExhibitPage(DatabaseService dbService)
        {
            InitializeComponent();
            this.dbService = dbService;
            dpPlannedReturn.SelectedDate = DateTime.Now.AddDays(7);
            dpPlannedReturn.SelectedDateChanged += DpPlannedReturn_SelectedDateChanged;
            LoadData();

            cmbExhibit.SelectionChanged += CmbExhibit_SelectionChanged;
        }

        private void LoadData()
        {
            try
            {
                List<Exhibit> allExhibits = dbService.GetAllExhibits();
                List<Exhibit> available = new List<Exhibit>();

                if (allExhibits != null)
                {
                    available = allExhibits.Where(x => x.CurrentStatus == "В наличии").ToList();
                }

                cmbExhibit.ItemsSource = null;
                cmbExhibit.ItemsSource = available;
                cmbExhibit.DisplayMemberPath = "Name";
                cmbExhibit.SelectedValuePath = "Id";

                List<Teacher> teachers = dbService.GetAllTeachers();

                cmbTeacher.ItemsSource = null;
                cmbTeacher.ItemsSource = teachers;
                cmbTeacher.DisplayMemberPath = "FullName";
                cmbTeacher.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void CmbExhibit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbExhibit.SelectedItem is Exhibit exhibit)
            {
                txtExhibitInfo.Text = $"Инв. номер: {exhibit.InventoryNumber}\nКатегория: {exhibit.Category ?? "Не указана"}\nМатериал: {exhibit.Material ?? "Не указан"}\nСостояние: {exhibit.Condition ?? "Не указано"}\nМестоположение: {exhibit.Location ?? "Не указано"}";
            }
            else
            {
                txtExhibitInfo.Text = "Выберите экспонат из списка";
            }
        }

        private void DpPlannedReturn_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpPlannedReturn.SelectedDate < DateTime.Now.Date)
            {
                txtDateWarning.Text = "Дата возврата не может быть раньше сегодняшнего дня";
                txtDateWarning.Visibility = Visibility.Visible;
            }
            else if (dpPlannedReturn.SelectedDate > DateTime.Now.AddMonths(3))
            {
                txtDateWarning.Text = "Рекомендуется указывать дату возврата не более чем на 3 месяца";
                txtDateWarning.Visibility = Visibility.Visible;
            }
            else
            {
                txtDateWarning.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnIssue_Click(object sender, RoutedEventArgs e)
        {
            if (cmbExhibit.SelectedItem == null)
            {
                MessageBox.Show("Выберите экспонат");
                return;
            }

            if (cmbTeacher.SelectedItem == null)
            {
                MessageBox.Show("Выберите преподавателя");
                return;
            }

            if (dpPlannedReturn.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату возврата");
                return;
            }

            if (dpPlannedReturn.SelectedDate < DateTime.Now.Date)
            {
                MessageBox.Show("Дата возврата не может быть раньше сегодняшнего дня");
                return;
            }

            int exhibitId = (int)cmbExhibit.SelectedValue;
            int teacherId = (int)cmbTeacher.SelectedValue;
            DateTime returnDate = dpPlannedReturn.SelectedDate.Value;
            string purpose = txtPurposeDetail.Text;

            MessageBoxResult confirm = MessageBox.Show("Выдать экспонат?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            btnIssue.IsEnabled = false;
            btnIssue.Content = "Оформление...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                dbService.IssueExhibit(exhibitId, teacherId, returnDate, purpose);
            });

            MessageBox.Show("Экспонат выдан");

            btnIssue.IsEnabled = true;
            btnIssue.Content = "ВЫДАТЬ";

            LoadData();

            cmbExhibit.SelectedItem = null;
            txtPurposeDetail.Text = "Подробное описание цели";
            dpPlannedReturn.SelectedDate = DateTime.Now.AddDays(7);
        }

        private void BtnAddTeacher_Click(object sender, RoutedEventArgs e)
        {
            TeacherEditPage page = new TeacherEditPage(dbService);
            page.TeacherSaved += (s, args) => LoadData();
            NavigationService.Navigate(page);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}