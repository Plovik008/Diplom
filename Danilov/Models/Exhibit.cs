using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

        public int? LocationId { get; set; }

        public string Location { get; set; }

        [JsonIgnore]
        public string PhotoPath
        {
            get => PhotoPaths != null && PhotoPaths.Count > 0
                ? PhotoPaths[0]
                : null;

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    PhotoPaths = new List<string>();
                }
                else
                {
                    PhotoPaths = new List<string> { value };
                }
            }
        }

        [JsonPropertyName("photoPaths")]
        public List<string> PhotoPaths { get; set; } = new List<string>();

        public decimal Cost { get; set; }

        public DateTime? LastRestorationDate { get; set; }

        public string ResponsiblePerson { get; set; }

        public string Source { get; set; }

        public int? YearOfOrigin { get; set; }

        public DateTime CreatedDate { get; set; }

        [JsonIgnore]
        public string CurrentStatus { get; set; }

        [JsonIgnore]
        public string DisplayName => $"{InventoryNumber} - {Name}";

        [JsonIgnore]
        public int DataVersion { get; set; }
    }
}