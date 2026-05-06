using Newtonsoft.Json;

namespace MuseumAccountingSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public int? TeacherId { get; set; }

        [JsonIgnore]
        public bool IsAdmin => Role == "Admin";

        [JsonIgnore]
        public bool IsEmployee => Role == "Employee";

        [JsonIgnore]
        public bool IsTeacher => Role == "Teacher";
    }
}
