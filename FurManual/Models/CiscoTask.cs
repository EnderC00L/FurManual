using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurManual.Models
{
    public class CiscoTask
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string Chapter { get; set; }

        [Required]
        public string Description { get; set; }

        public int Difficulty { get; set; }

        public string? ValidationCriteria { get; set; }

        [NotMapped]
        public bool IsCompleted { get; set; }
    }
}