using System.ComponentModel.DataAnnotations;

namespace FurManual.Models
{
    public class Test
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Question> Questions { get; set; } = new();
    }
}