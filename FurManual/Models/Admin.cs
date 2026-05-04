using System.ComponentModel.DataAnnotations;

namespace FurManual.Models
{
    public class Admin
    {
        public int Id { get; set; }

        public required string FullName { get; set; }

        public required string Login { get; set; }

        public required string PasswordHash { get; set; }
    }
}
