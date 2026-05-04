using FurManual.Data;
using FurManual.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FurManual.Pages.Tests
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class TestViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public int QuestionsCount { get; set; }
            public bool IsCompleted { get; set; }
            public double? BestScore { get; set; }
            public string? BestTime { get; set; }
            public string? StudentName { get; set; }
            public string StatusClass { get; set; } = "";
        }

        public IList<TestViewModel> Tests { get; set; } = new List<TestViewModel>();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public const int PageSize = 10;

        public class TestResultCookie
        {
            public double Score { get; set; }
            public double TimeSeconds { get; set; }
        }

        public async Task OnGetAsync()
        {
            // 1. Читаем куки результатов
            var cookies = Request.Cookies[".FurManual.TestResults"];
            var resultsDict = new Dictionary<int, TestResultCookie>();

            if (!string.IsNullOrEmpty(cookies))
            {
                try { resultsDict = JsonSerializer.Deserialize<Dictionary<int, TestResultCookie>>(cookies); }
                catch {}
            }

            // 2. Читаем имя студента
            var studentName = Request.Cookies["FurManual_StudentName"];
            if (!string.IsNullOrEmpty(studentName))
            {
                studentName = System.Web.HttpUtility.UrlDecode(studentName);
            }

            // 3. Пагинация
            var totalItems = await _context.Tests.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // 4. Загрузка из БД
            var dbTests = await _context.Tests
                .Include(t => t.Questions)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    Count = t.Questions.Count
                })
                .ToListAsync();

            // 5. Маппинг данных
            foreach (var t in dbTests)
            {
                var vm = new TestViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    QuestionsCount = t.Count
                };

                if (resultsDict != null && resultsDict.ContainsKey(t.Id))
                {
                    var res = resultsDict[t.Id];
                    vm.IsCompleted = true;
                    vm.BestScore = res.Score;
                    vm.StudentName = studentName;

                    var ts = TimeSpan.FromSeconds(res.TimeSeconds);
                    vm.BestTime = $"{(int)ts.TotalMinutes}м {ts.Seconds}с";

                    if (res.Score >= 80)
                        vm.StatusClass = "status-green";
                    else if (res.Score >= 50)
                        vm.StatusClass = "status-yellow";
                    else
                        vm.StatusClass = "status-red";
                }

                Tests.Add(vm);
            }
        }
    }
}