using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurManual.Models
{
    public class Lecture
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название лекции")]
        public required string Title { get; set; }

        [Required]
        public required string FilePath { get; set; }

        public string? OriginalFileName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public Admin? CreatedBy { get; set; }

        public int? UpdatedById { get; set; }

        [ForeignKey("UpdatedById")]
        public Admin? UpdatedBy { get; set; }
    }
}
