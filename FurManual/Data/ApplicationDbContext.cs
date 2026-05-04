using Microsoft.EntityFrameworkCore;
using FurManual.Models;

namespace FurManual.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<CiscoTask> CiscoTasks { get; set; }

        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<PracticalWork> PracticalWorks { get; set; }
    }
}
