using FurManual.Data;
using FurManual.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FurManual.Pages
{
    public class CiscoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CiscoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<CiscoTask> Tasks { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Tasks = await _context.CiscoTasks
                .OrderBy(t => t.Difficulty)
                .ThenBy(t => t.Id)
                .ToListAsync();

            var completedIds = new HashSet<int>();
            if (Request.Cookies.TryGetValue("CiscoProgress", out string? cookieValue) && !string.IsNullOrEmpty(cookieValue))
            {
                var ids = cookieValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var idStr in ids)
                {
                    if (int.TryParse(idStr, out int id)) completedIds.Add(id);
                }
            }

            foreach (var task in Tasks)
            {
                if (completedIds.Contains(task.Id)) task.IsCompleted = true;
            }
        }
    }
}