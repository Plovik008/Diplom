using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MuseumAccountingSystem.Models;
using Newtonsoft.Json;

namespace MuseumAccountingSystem.Services
{
    public class DatabaseService
    {
        private string dataFilePath;
        private AppData appData;

        public DatabaseService()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            dataFilePath = Path.Combine(dataFolder, "museum_data.json");
            LoadData();
        }

        private void LoadData()
        {
            if (File.Exists(dataFilePath))
            {
                string json = File.ReadAllText(dataFilePath);
                appData = JsonConvert.DeserializeObject<AppData>(json);
            }
            else
            {
                appData = new AppData();
                InsertDefaultData();
                SaveData();
            }
        }

        private void SaveData()
        {
            string json = JsonConvert.SerializeObject(appData, Formatting.Indented);
            File.WriteAllText(dataFilePath, json);
        }

        private void InsertDefaultData()
        {
            appData.Users.Add(new User
            {
                Id = 1,
                Username = "admin",
                Password = "admin123",
                Role = "Admin",
                FullName = "Администратор системы"
            });

            appData.Users.Add(new User
            {
                Id = 2,
                Username = "employee",
                Password = "employee123",
                Role = "Employee",
                FullName = "Сотрудник музея"
            });

            appData.Users.Add(new User
            {
                Id = 3,
                Username = "teacher",
                Password = "teacher123",
                Role = "Teacher",
                FullName = "Иванов Иван Иванович",
                TeacherId = 1
            });

            appData.Teachers.Add(new Teacher
            {
                Id = 1,
                FullName = "Иванов Иван Иванович",
                Department = "Исторический факультет",
                Email = "ivanov@university.ru",
                Phone = "+7-999-123-45-67"
            });

            appData.Exhibits.Add(new Exhibit
            {
                Id = 1,
                InventoryNumber = "ЭКС-001",
                Name = "Старинная ваза",
                Category = "Керамика",
                Condition = "В наличии",
                Location = "Музей",
                CreatedDate = DateTime.Now,
                Cost = 50000,
                LastRestorationDate = new DateTime(2023, 5, 15),
                ResponsiblePerson = "Петрова А.А.",
                Source = "Дар",
                YearOfOrigin = 1850
            });
        }

        private int GetNextId<T>(List<T> items) where T : class
        {
            if (items == null || items.Count == 0)
                return 1;

            var property = typeof(T).GetProperty("Id");
            if (property != null)
            {
                var maxId = items.Max(i => (int)property.GetValue(i));
                return maxId + 1;
            }
            return items.Count + 1;
        }

        public List<Exhibit> GetAllExhibits()
        {
            var exhibits = new List<Exhibit>();

            foreach (var e in appData.Exhibits)
            {
                bool isIssued = appData.Issues.Any(i => i.ExhibitId == e.Id && i.Status == "Выдан");

                exhibits.Add(new Exhibit
                {
                    Id = e.Id,
                    InventoryNumber = e.InventoryNumber,
                    Name = e.Name,
                    Category = e.Category,
                    Material = e.Material,
                    Condition = e.Condition,
                    Location = e.Location,
                    PhotoPath = e.PhotoPath,
                    CreatedDate = e.CreatedDate,
                    CurrentStatus = isIssued ? "Выдан" : "В наличии",
                    Cost = e.Cost,
                    LastRestorationDate = e.LastRestorationDate,
                    ResponsiblePerson = e.ResponsiblePerson,
                    Source = e.Source,
                    YearOfOrigin = e.YearOfOrigin
                });
            }

            return exhibits.OrderBy(e => e.InventoryNumber).ToList();
        }

        public void AddExhibit(Exhibit exhibit, User currentUser = null)
        {
            exhibit.Id = GetNextId(appData.Exhibits);
            exhibit.CreatedDate = DateTime.Now;
            appData.Exhibits.Add(exhibit);
            SaveData();

            if (currentUser != null)
            {
                AddLog(currentUser, "Добавление", "Экспонат", exhibit.Name, $"Инв. номер: {exhibit.InventoryNumber}");
            }
        }

        public void UpdateExhibit(Exhibit exhibit, User currentUser = null)
        {
            var existing = appData.Exhibits.FirstOrDefault(e => e.Id == exhibit.Id);
            if (existing != null)
            {
                existing.InventoryNumber = exhibit.InventoryNumber;
                existing.Name = exhibit.Name;
                existing.Category = exhibit.Category;
                existing.Material = exhibit.Material;
                existing.Condition = exhibit.Condition;
                existing.Location = exhibit.Location;
                existing.PhotoPath = exhibit.PhotoPath;
                existing.Cost = exhibit.Cost;
                existing.LastRestorationDate = exhibit.LastRestorationDate;
                existing.ResponsiblePerson = exhibit.ResponsiblePerson;
                existing.Source = exhibit.Source;
                existing.YearOfOrigin = exhibit.YearOfOrigin;
                SaveData();

                if (currentUser != null)
                {
                    AddLog(currentUser, "Редактирование", "Экспонат", exhibit.Name, $"Инв. номер: {exhibit.InventoryNumber}");
                }
            }
        }

        public void DeleteExhibit(int id, User currentUser = null)
        {
            var exhibit = appData.Exhibits.FirstOrDefault(e => e.Id == id);
            if (exhibit != null && currentUser != null)
            {
                AddLog(currentUser, "Удаление", "Экспонат", exhibit.Name, $"Инв. номер: {exhibit.InventoryNumber}");
            }

            appData.Exhibits.RemoveAll(e => e.Id == id);
            appData.Issues.RemoveAll(i => i.ExhibitId == id);
            SaveData();
        }

        public List<Teacher> GetAllTeachers()
        {
            return appData.Teachers.OrderBy(t => t.FullName).ToList();
        }

        public void AddTeacher(Teacher teacher, User currentUser = null)
        {
            teacher.Id = GetNextId(appData.Teachers);
            appData.Teachers.Add(teacher);
            SaveData();

            if (currentUser != null)
            {
                AddLog(currentUser, "Добавление", "Преподаватель", teacher.FullName, $"Кафедра: {teacher.Department}");
            }
        }

        public void UpdateTeacher(Teacher teacher, User currentUser = null)
        {
            var existing = appData.Teachers.FirstOrDefault(t => t.Id == teacher.Id);
            if (existing != null)
            {
                existing.FullName = teacher.FullName;
                existing.Department = teacher.Department;
                existing.Email = teacher.Email;
                existing.Phone = teacher.Phone;
                SaveData();

                if (currentUser != null)
                {
                    AddLog(currentUser, "Редактирование", "Преподаватель", teacher.FullName, $"Кафедра: {teacher.Department}");
                }
            }
        }

        public void DeleteTeacher(int id, User currentUser = null)
        {
            var teacher = appData.Teachers.FirstOrDefault(t => t.Id == id);
            if (teacher != null && currentUser != null)
            {
                AddLog(currentUser, "Удаление", "Преподаватель", teacher.FullName, "");
            }

            appData.Teachers.RemoveAll(t => t.Id == id);
            SaveData();
        }

        public Teacher GetTeacherByUser(User user)
        {
            if (user == null || !user.IsTeacher)
                return null;

            if (user.TeacherId.HasValue)
            {
                var teacherById = appData.Teachers.FirstOrDefault(t => t.Id == user.TeacherId.Value);
                if (teacherById != null)
                    return teacherById;
            }

            var teacherByName = appData.Teachers.FirstOrDefault(t =>
                string.Equals(t.FullName, user.FullName, StringComparison.OrdinalIgnoreCase));

            if (teacherByName != null)
                return teacherByName;

            if (appData.Teachers.Count == 1)
                return appData.Teachers[0];

            return null;
        }

        public int? GetTeacherIdByUser(User user)
        {
            return GetTeacherByUser(user)?.Id;
        }

        public List<Issue> GetAllIssues(bool onlyActive = false, int? teacherId = null)
        {
            var issues = new List<Issue>();

            foreach (var issue in appData.Issues)
            {
                var exhibit = appData.Exhibits.FirstOrDefault(e => e.Id == issue.ExhibitId);
                var teacher = appData.Teachers.FirstOrDefault(t => t.Id == issue.TeacherId);

                if (exhibit != null && teacher != null)
                {
                    var newIssue = new Issue
                    {
                        Id = issue.Id,
                        ExhibitId = issue.ExhibitId,
                        TeacherId = issue.TeacherId,
                        ExhibitName = exhibit.Name,
                        ExhibitInventoryNumber = exhibit.InventoryNumber,
                        TeacherName = teacher.FullName,
                        IssueDate = issue.IssueDate,
                        PlannedReturnDate = issue.PlannedReturnDate,
                        ActualReturnDate = issue.ActualReturnDate,
                        Purpose = issue.Purpose,
                        Status = issue.Status
                    };
                    issues.Add(newIssue);
                }
            }

            if (teacherId.HasValue)
                issues = issues.Where(i => i.TeacherId == teacherId.Value).ToList();

            if (onlyActive)
                issues = issues.Where(i => i.Status == "Выдан").ToList();

            return issues.OrderByDescending(i => i.IssueDate).ToList();
        }

        public void IssueExhibit(int exhibitId, int teacherId, DateTime plannedReturnDate, string purpose, User currentUser = null)
        {
            var exhibit = appData.Exhibits.FirstOrDefault(e => e.Id == exhibitId);
            var teacher = appData.Teachers.FirstOrDefault(t => t.Id == teacherId);

            var issue = new IssueData
            {
                Id = GetNextId(appData.Issues),
                ExhibitId = exhibitId,
                TeacherId = teacherId,
                IssueDate = DateTime.Now,
                PlannedReturnDate = plannedReturnDate,
                Purpose = purpose,
                Status = "Выдан"
            };
            appData.Issues.Add(issue);
            SaveData();

            if (currentUser != null && exhibit != null && teacher != null)
            {
                AddLog(currentUser, "Выдача", "Экспонат", exhibit.Name, $"Преподаватель: {teacher.FullName}, дата возврата: {plannedReturnDate:dd.MM.yyyy}");
            }
        }

        public void ReturnExhibit(int issueId, User currentUser = null)
        {
            var issue = appData.Issues.FirstOrDefault(i => i.Id == issueId);
            if (issue != null)
            {
                var exhibit = appData.Exhibits.FirstOrDefault(e => e.Id == issue.ExhibitId);
                var teacher = appData.Teachers.FirstOrDefault(t => t.Id == issue.TeacherId);

                issue.ActualReturnDate = DateTime.Now;
                issue.Status = "Возвращен";
                SaveData();

                if (currentUser != null && exhibit != null && teacher != null)
                {
                    AddLog(currentUser, "Возврат", "Экспонат", exhibit.Name, $"Преподаватель: {teacher.FullName}");
                }
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            var user = appData.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
            return user;
        }

        public List<UserActionLog> GetAllLogs()
        {
            return appData.Logs.OrderByDescending(l => l.ActionTime).ToList();
        }

        private void AddLog(User user, string action, string targetType, string targetName, string details)
        {
            var log = new UserActionLog
            {
                Id = GetNextId(appData.Logs),
                Username = user.Username,
                UserRole = user.Role,
                Action = action,
                TargetType = targetType,
                TargetName = targetName,
                ActionTime = DateTime.Now,
                Details = details
            };
            appData.Logs.Add(log);
            SaveData();
        }

        public List<ExhibitStatistics> GetPopularExhibits(int months = 12, int? teacherId = null)
        {
            var startDate = DateTime.Now.AddMonths(-months);
            var issues = appData.Issues.Where(i => i.IssueDate >= startDate);

            if (teacherId.HasValue)
                issues = issues.Where(i => i.TeacherId == teacherId.Value);

            var stats = issues.ToList()
                              .GroupBy(i => i.ExhibitId)
                              .Select(g => new ExhibitStatistics
                              {
                                  ExhibitId = g.Key,
                                  IssueCount = g.Count()
                              })
                              .OrderByDescending(x => x.IssueCount)
                              .Take(10)
                              .ToList();

            foreach (var stat in stats)
            {
                var exhibit = appData.Exhibits.FirstOrDefault(e => e.Id == stat.ExhibitId);
                if (exhibit != null)
                {
                    stat.ExhibitName = exhibit.Name;
                    stat.InventoryNumber = exhibit.InventoryNumber;
                }
            }

            return stats.OrderByDescending(x => x.IssueCount).ToList();
        }

        public List<TeacherStatistics> GetTeacherStatistics(int? teacherId = null)
        {
            var issues = appData.Issues.ToList();
            var teachers = appData.Teachers.ToList();

            if (teacherId.HasValue)
                teachers = teachers.Where(t => t.Id == teacherId.Value).ToList();

            var stats = new List<TeacherStatistics>();

            foreach (var teacher in teachers)
            {
                var teacherIssues = issues.Where(i => i.TeacherId == teacher.Id).ToList();

                stats.Add(new TeacherStatistics
                {
                    TeacherId = teacher.Id,
                    TeacherName = teacher.FullName,
                    Department = teacher.Department,
                    TotalIssues = teacherIssues.Count,
                    OverdueCount = teacherIssues.Count(i => i.Status == "Выдан" && i.PlannedReturnDate.Date < DateTime.Now.Date),
                    ReturnedCount = teacherIssues.Count(i => i.Status == "Возвращен")
                });
            }

            return stats.OrderByDescending(x => x.TotalIssues).ToList();
        }

        public List<MonthlyStatistics> GetMonthlyStatistics(int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = startDate.AddYears(1);
            var issues = appData.Issues.Where(i => i.IssueDate >= startDate && i.IssueDate < endDate).ToList();

            var stats = new List<MonthlyStatistics>();

            for (int month = 1; month <= 12; month++)
            {
                var monthStart = new DateTime(year, month, 1);
                var monthEnd = monthStart.AddMonths(1);
                var monthIssues = issues.Where(i => i.IssueDate >= monthStart && i.IssueDate < monthEnd).ToList();

                stats.Add(new MonthlyStatistics
                {
                    Month = monthStart.ToString("MMMM"),
                    IssuesCount = monthIssues.Count,
                    ReturnsCount = monthIssues.Count(i => i.Status == "Возвращен")
                });
            }

            return stats;
        }
    }

    public class AppData
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Exhibit> Exhibits { get; set; } = new List<Exhibit>();
        public List<Teacher> Teachers { get; set; } = new List<Teacher>();
        public List<IssueData> Issues { get; set; } = new List<IssueData>();
        public List<UserActionLog> Logs { get; set; } = new List<UserActionLog>();
    }

    public class IssueData
    {
        public int Id { get; set; }
        public int ExhibitId { get; set; }
        public int TeacherId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime PlannedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Purpose { get; set; }
        public string Status { get; set; }
    }
}
