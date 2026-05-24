namespace MuseumAccountingSystem.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool HasLogin => !string.IsNullOrWhiteSpace(Username);
    }
}
