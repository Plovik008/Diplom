using System;

namespace MuseumAccountingSystem.Models
{
    public class UserActionLog
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string UserRole { get; set; }
        public string Action { get; set; }
        public string TargetType { get; set; }
        public string TargetName { get; set; }
        public DateTime ActionTime { get; set; }
        public string Details { get; set; }
    }
}