using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;
using MuseumAccountingSystem.Views.Pages;

namespace MuseumAccountingSystem.Views
{
    public partial class NotificationsWindow : Window
    {
        private DatabaseService dbService;
        private List<Issue> overdueIssues;
        private MainWindow mainWindow;
        private User currentUser;
        private int? currentTeacherId;

public NotificationsWindow(DatabaseService dbService, MainWindow mainWindow, User currentUser)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.mainWindow = mainWindow;
            this.currentUser = currentUser;
            currentTeacherId = dbService.GetTeacherIdByUser(currentUser);

            btnGoToReturn.Visibility = Visibility.Collapsed;

            LoadOverdueIssues();
        }

        private void LoadOverdueIssues()
        {
            try
            {
                if (!currentUser.IsTeacher || !currentTeacherId.HasValue)
                {
                    overdueIssues = new List<Issue>();
                    dgvOverdueIssues.ItemsSource = new List<OverdueItem>();
                    txtSummary.Text = "Просроченных экспонатов не обнаружено";
                    return;
                }

                var activeIssues = dbService.GetAllIssues(true, currentTeacherId);
                overdueIssues = new List<Issue>();

                foreach (var issue in activeIssues)
                {
                    if (issue.IsOverdue)
                    {
                        overdueIssues.Add(issue);
                    }
                }

                var overdueList = new List<OverdueItem>();

                foreach (var issue in overdueIssues)
                {
                    var days = (DateTime.Now - issue.PlannedReturnDate).Days;
                    overdueList.Add(new OverdueItem
                    {
                        InventoryNumber = issue.ExhibitInventoryNumber,
                        ExhibitName = issue.ExhibitName,
                        TeacherName = issue.TeacherName,
                        OverdueDays = days
                    });
                }

                dgvOverdueIssues.ItemsSource = overdueList;
                txtSummary.Text = $"Обнаружено {overdueIssues.Count} просроченных экспонатов";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void BtnGoToReturn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mainWindow.NavigateToReturnPage();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                MessageBox.Show("Ошибка при переходе к возврату", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class OverdueItem
    {
        public string InventoryNumber { get; set; }
        public string ExhibitName { get; set; }
        public string TeacherName { get; set; }
        public int OverdueDays { get; set; }
    }
}
