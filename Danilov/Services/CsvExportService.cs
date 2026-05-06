using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using MuseumAccountingSystem.Models;

namespace MuseumAccountingSystem.Services
{
    public class CsvExportService
    {
        public void ExportExhibitsToCsv(List<Exhibit> exhibits)
        {
            try
            {
                if (exhibits == null || exhibits.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Экспонаты_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                using (StreamWriter sw = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                {
                    sw.WriteLine("Инв.номер;Название;Категория;Материал;Состояние;Местоположение;Статус;Стоимость;Год создания;Ответственный;Источник;Дата создания");

                    foreach (var ex in exhibits)
                    {
                        string createdDate = ex.CreatedDate.ToString("dd.MM.yyyy HH:mm");
                        string yearOfOrigin = ex.YearOfOrigin.HasValue ? ex.YearOfOrigin.Value.ToString() : "";
                        string cost = ex.Cost.ToString("N0");

                        sw.WriteLine($"{ex.InventoryNumber};{ex.Name};{ex.Category};{ex.Material};{ex.Condition};{ex.Location};{ex.CurrentStatus};{cost};{yearOfOrigin};{ex.ResponsiblePerson};{ex.Source};{createdDate}");
                    }
                }

                MessageBox.Show($"Экспорт завершен! Экспортировано {exhibits.Count} записей.\nФайл сохранен: {saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportIssuesToCsv(List<Issue> issues)
        {
            try
            {
                if (issues == null || issues.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Журнал_выдачи_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                using (StreamWriter sw = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                {
                    sw.WriteLine("Инв.номер;Экспонат;Преподаватель;Дата выдачи;План.возврат;Факт.возврат;Цель;Статус");

                    foreach (var issue in issues)
                    {
                        string issueDate = issue.IssueDate.ToString("dd.MM.yyyy HH:mm");
                        string plannedDate = issue.PlannedReturnDate.ToString("dd.MM.yyyy");
                        string actualDate = issue.ActualReturnDate.HasValue ? issue.ActualReturnDate.Value.ToString("dd.MM.yyyy HH:mm") : "";

                        sw.WriteLine($"{issue.ExhibitInventoryNumber};{issue.ExhibitName};{issue.TeacherName};{issueDate};{plannedDate};{actualDate};{issue.Purpose};{issue.Status}");
                    }
                }

                MessageBox.Show($"Экспорт завершен! Экспортировано {issues.Count} записей.\nФайл сохранен: {saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportTeachersToCsv(List<TeacherStatistics> teacherStats)
        {
            try
            {
                if (teacherStats == null || teacherStats.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Отчет_по_преподавателям_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                using (StreamWriter sw = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                {
                    sw.WriteLine("Преподаватель;Кафедра;Всего выдач;Возвращено;Просрочено");

                    foreach (var stat in teacherStats)
                    {
                        sw.WriteLine($"{stat.TeacherName};{stat.Department};{stat.TotalIssues};{stat.ReturnedCount};{stat.OverdueCount}");
                    }
                }

                MessageBox.Show($"Экспорт завершен! Экспортировано {teacherStats.Count} записей.\nФайл сохранен: {saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}