using System;
using Newtonsoft.Json;

namespace MuseumAccountingSystem.Models
{
    public class Exhibit
    {
        public int Id { get; set; }
        public string InventoryNumber { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Material { get; set; }
        public string Condition { get; set; }
        public string Location { get; set; }
        public string PhotoPath { get; set; }
        public DateTime CreatedDate { get; set; }

        public decimal Cost { get; set; }
        public DateTime? LastRestorationDate { get; set; }
        public string ResponsiblePerson { get; set; }
        public string Source { get; set; }
        public int? YearOfOrigin { get; set; }

        [JsonIgnore]
        public string CurrentStatus { get; set; }

        [JsonIgnore]
        public string DisplayName => $"{InventoryNumber} - {Name}";
    }
}