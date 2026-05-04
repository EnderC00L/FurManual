using System.ComponentModel.DataAnnotations.Schema;

namespace FurManual.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public required string Text { get; set; }
        public bool IsMultipleChoice { get; set; }

        [ForeignKey("TestId")]
        public Test? Test { get; set; }

        public List<Answer> Answers { get; set; } = new();
    }
}