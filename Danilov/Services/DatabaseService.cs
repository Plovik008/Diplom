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
                Condition = "Хорошее",
                Location = "Витрина №1",
                CreatedDate = DateTime.Now
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
                    CurrentStatus = isIssued ? "Выдан" : "В наличии"
                });
            }

            return exhibits.OrderBy(e => e.InventoryNumber).ToList();
        }

        public void AddExhibit(Exhibit exhibit)
        {
            exhibit.Id = GetNextId(appData.Exhibits);
            exhibit.CreatedDate = DateTime.Now;
            appData.Exhibits.Add(exhibit);
            SaveData();
        }

        public void UpdateExhibit(Exhibit exhibit)
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
                SaveData();
            }
        }

        public void DeleteExhibit(int id)
        {
            appData.Exhibits.RemoveAll(e => e.Id == id);
            appData.Issues.RemoveAll(i => i.ExhibitId == id);
            SaveData();
        }

        public List<Teacher> GetAllTeachers()
        {
            return appData.Teachers.OrderBy(t => t.FullName).ToList();
        }

        public void AddTeacher(Teacher teacher)
        {
            teacher.Id = GetNextId(appData.Teachers);
            appData.Teachers.Add(teacher);
            SaveData();
        }

        public void UpdateTeacher(Teacher teacher)
        {
            var existing = appData.Teachers.FirstOrDefault(t => t.Id == teacher.Id);
            if (existing != null)
            {
                existing.FullName = teacher.FullName;
                existing.Department = teacher.Department;
                existing.Email = teacher.Email;
                existing.Phone = teacher.Phone;
                SaveData();
            }
        }

        public void DeleteTeacher(int id)
        {
            appData.Teachers.RemoveAll(t => t.Id == id);
            SaveData();
        }

        public List<Issue> GetAllIssues(bool onlyActive = false)
        {
            var issues = new List<Issue>();

            foreach (var issue in appData.Issues)
            {
                var exhibit = appData.Exhibits.FirstOrDefault(e => e.Id == issue.ExhibitId);
                var teacher = appData.Teachers.FirstOrDefault(t => t.Id == issue.TeacherId);

                if (exhibit != null && teacher != null)
                {
                    issues.Add(new Issue
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
                    });
                }
            }

            if (onlyActive)
                issues = issues.Where(i => i.Status == "Выдан").ToList();

            return issues.OrderByDescending(i => i.IssueDate).ToList();
        }

        public void IssueExhibit(int exhibitId, int teacherId, DateTime plannedReturnDate, string purpose)
        {
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
        }

        public void ReturnExhibit(int issueId)
        {
            var issue = appData.Issues.FirstOrDefault(i => i.Id == issueId);
            if (issue != null)
            {
                issue.ActualReturnDate = DateTime.Now;
                issue.Status = "Возвращен";
                SaveData();
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            var user = appData.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
            return user;
        }
    }

    public class AppData
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Exhibit> Exhibits { get; set; } = new List<Exhibit>();
        public List<Teacher> Teachers { get; set; } = new List<Teacher>();
        public List<IssueData> Issues { get; set; } = new List<IssueData>();
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