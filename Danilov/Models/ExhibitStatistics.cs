using System;

namespace MuseumAccountingSystem.Models
{
    public class ExhibitStatistics
    {
        public int ExhibitId { get; set; }
        public string ExhibitName { get; set; }
        public string InventoryNumber { get; set; }
        public int IssueCount { get; set; }
    }

    public class TeacherStatistics
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string Department { get; set; }
        public int TotalIssues { get; set; }
        public int OverdueCount { get; set; }
        public int ReturnedCount { get; set; }
    }

    public class MonthlyStatistics
    {
        public string Month { get; set; }
        public int IssuesCount { get; set; }
        public int ReturnsCount { get; set; }
    }
}