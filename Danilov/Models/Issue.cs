using System;

namespace MuseumAccountingSystem.Models
{
    public class Issue
    {
        public int Id { get; set; }
        public int ExhibitId { get; set; }
        public int TeacherId { get; set; }
        public string ExhibitName { get; set; }
        public string ExhibitInventoryNumber { get; set; }
        public string TeacherName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime PlannedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Purpose { get; set; }
        public string Status { get; set; }
    }
}