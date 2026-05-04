using FurManual.Data;
using FurManual.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FurManual.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Данные списков
        public List<Lecture> RecentLectures { get; set; } = new();
        public List<PracticalWork> RecentPracticals { get; set; } = new();

        public string? StudentName { get; set; }

        // --- Детальная статистика Тестов ---
        public int CompletedTestsCount { get; set; } = 0;
        public int TotalTestsCount { get; set; } = 0;
        public int GreenTestsCount { get; set; } = 0;  // >= 80%
        public int YellowTestsCount { get; set; } = 0; // 50 - 79%
        public int RedTestsCount { get; set; } = 0;    // < 50%

        // --- Детальная статистика Cisco ---
        public int CompletedCiscoTasksCount { get; set; } = 0;
        public int TotalCiscoTasksCount { get; set; } = 0;

        // Вспомогательный класс для парсинга куки результатов
        public class TestResultCookie
        {
            public double Score { get; set; }
            public double TimeSeconds { get; set; }
        }

        public async Task OnGetAsync()
        {
            // 1. Получаем 3 самые свежие лекции
            RecentLectures = await _context.Lectures
                .AsNoTracking()
                .OrderByDescending(l => l.CreatedAt)
                .Take(3)
                .ToListAsync();

            // 2. Получаем 3 самые свежие практические работы
            RecentPracticals = await _context.PracticalWorks
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();

            // 3. Читаем имя студента
            var studentCookie = Request.Cookies["FurManual_StudentName"];
            if (!string.IsNullOrEmpty(studentCookie))
            {
                StudentName = System.Web.HttpUtility.UrlDecode(studentCookie);
            }

            // 4. Подтягиваем общее количество заданий из базы
            TotalTestsCount = await _context.Tests.CountAsync();
            TotalCiscoTasksCount = await _context.CiscoTasks.CountAsync();

            // 5. Разбираем успеваемость по тестам
            var testsCookie = Request.Cookies[".FurManual.TestResults"];
            if (!string.IsNullOrEmpty(testsCookie))
            {
                try
                {
                    var resultsDict = JsonSerializer.Deserialize<Dictionary<int, TestResultCookie>>(testsCookie);
                    if (resultsDict != null)
                    {
                        CompletedTestsCount = resultsDict.Count;

                        foreach (var res in resultsDict.Values)
                        {
                            if (res.Score >= 80) GreenTestsCount++;
                            else if (res.Score >= 50) YellowTestsCount++;
                            else RedTestsCount++;
                        }
                    }
                }
                catch {}
            }

            // 6. Подсчитываем выполненные задания Cisco
            if (Request.Cookies.TryGetValue("CiscoProgress", out string? ciscoCookie) && !string.IsNullOrEmpty(ciscoCookie))
            {
                var ids = ciscoCookie.Split(',', StringSplitOptions.RemoveEmptyEntries);
                CompletedCiscoTasksCount = ids.Length;
            }
        }
    }
}