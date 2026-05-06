using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class StatisticsPage : Page
    {
        private DatabaseService dbService;
        private User currentUser;
        private List<ExhibitStatistics> popularExhibits;
        private List<TeacherStatistics> teacherStats;
        private CsvExportService csvExport;
        private int? currentTeacherId;

        public StatisticsPage(DatabaseService dbService, User currentUser)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            csvExport = new CsvExportService();
            currentTeacherId = dbService.GetTeacherIdByUser(currentUser);

            if (currentUser.IsTeacher)
            {
                txtPageTitle.Text = "Моя статистика";
                txtPopularExhibitsTitle.Text = "Мои экспонаты";
                txtTeacherStatsTitle.Text = "Моя активность";
            }

            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                if (currentUser.IsTeacher && !currentTeacherId.HasValue)
                {
                    txtTotalExhibits.Text = "0";
                    txtActiveIssues.Text = "0";
                    txtOverdueIssues.Text = "0";
                    popularExhibits = new List<ExhibitStatistics>();
                    teacherStats = new List<TeacherStatistics>();
                    dgvPopularExhibits.ItemsSource = popularExhibits;
                    dgvTeacherStats.ItemsSource = teacherStats;
                    return;
                }

                var allExhibits = dbService.GetAllExhibits();
                var activeIssues = dbService.GetAllIssues(true, currentUser.IsTeacher ? currentTeacherId : null);

                if (currentUser.IsTeacher && currentTeacherId.HasValue)
                {
                    var myIssuedExhibitIds = dbService.GetAllIssues(false, currentTeacherId.Value)
                        .Select(i => i.ExhibitId)
                        .Distinct()
                        .ToHashSet();

                    allExhibits = allExhibits
                        .Where(e => myIssuedExhibitIds.Contains(e.Id))
                        .ToList();
                }

                txtTotalExhibits.Text = allExhibits.Count.ToString();
                txtActiveIssues.Text = activeIssues.Count.ToString();

                int overdueCount = 0;
                foreach (var issue in activeIssues)
                {
                    if (issue.IsOverdue)
                    {
                        overdueCount++;
                    }
                }
                txtOverdueIssues.Text = overdueCount.ToString();

                popularExhibits = dbService.GetPopularExhibits(12, currentUser.IsTeacher ? currentTeacherId : null);
                dgvPopularExhibits.ItemsSource = popularExhibits;

                teacherStats = dbService.GetTeacherStatistics(currentUser.IsTeacher ? currentTeacherId : null);
                dgvTeacherStats.ItemsSource = teacherStats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportTeacherStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<TeacherStatistics> allTeacherStats = currentUser.IsTeacher && !currentTeacherId.HasValue
                    ? new List<TeacherStatistics>()
                    : dbService.GetTeacherStatistics(currentUser.IsTeacher ? currentTeacherId : null);
                csvExport.ExportTeachersToCsv(allTeacherStats);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
