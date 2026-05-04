using FurManual.Data;
using FurManual.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Linq;

namespace FurManual.Pages.Tests
{
    public class RunModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RunModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Test Test { get; set; } = default!;
        public bool IsFinished { get; set; }
        public int CorrectCount { get; set; }
        public double Percentage { get; set; }
        public double TimeTakenSeconds { get; set; }
        public string FormattedTime { get; set; }

        [BindProperty]
        public long StartTicks { get; set; }

        [BindProperty]
        [RegularExpression(@"^[^0-9]*$", ErrorMessage = "ФИО не должно содержать цифры")]
        public string? FullName { get; set; }

        public Dictionary<int, List<int>> UserSelections { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Test = await _context.Tests
                .Include(t => t.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Test == null) return NotFound();

            StartTicks = DateTime.UtcNow.Ticks;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Test = await _context.Tests
                .Include(t => t.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Test == null) return NotFound();

            var endTicks = DateTime.UtcNow.Ticks;
            var elapsedSpan = new TimeSpan(endTicks - StartTicks);
            TimeTakenSeconds = elapsedSpan.TotalSeconds;
            if (TimeTakenSeconds < 0) TimeTakenSeconds = 0;
            FormattedTime = $"{(int)elapsedSpan.TotalMinutes} мин {elapsedSpan.Seconds} сек";

            CorrectCount = 0;

            foreach (var question in Test.Questions)
            {
                var formKey = $"Question_{question.Id}";

                if (Request.Form.ContainsKey(formKey))
                {
                    var selectedAnswerIds = Request.Form[formKey]
                        .Select(x => int.TryParse(x, out int val) ? val : 0)
                        .Where(x => x != 0)
                        .ToList();

                    UserSelections[question.Id] = selectedAnswerIds;

                    var correctAnswerIds = question.Answers
                        .Where(a => a.IsCorrect)
                        .Select(a => a.Id)
                        .ToList();


                    bool isCorrect = selectedAnswerIds.Count == correctAnswerIds.Count &&
                                     !selectedAnswerIds.Except(correctAnswerIds).Any();

                    if (isCorrect)
                    {
                        CorrectCount++;
                    }
                }
            }

            if (Test.Questions.Count > 0)
                Percentage = Math.Round((double)CorrectCount / Test.Questions.Count * 100, 1);
            else
                Percentage = 0;

            SaveResultToCookie(id, Percentage, TimeTakenSeconds);

            IsFinished = true;
            return Page();
        }

        private void SaveResultToCookie(int testId, double score, double time)
        {
            var cookieKey = ".FurManual.TestResults";
            var dict = new Dictionary<int, object>();
            var oldCookie = Request.Cookies[cookieKey];
            if (!string.IsNullOrEmpty(oldCookie))
            {
                try { dict = JsonSerializer.Deserialize<Dictionary<int, object>>(oldCookie); } catch { }
            }
            if (dict == null) dict = new Dictionary<int, object>();

            bool shouldUpdate = true;
            if (dict.ContainsKey(testId))
            {
                var oldJson = JsonSerializer.Serialize(dict[testId]);
                try
                {
                    var oldRes = JsonSerializer.Deserialize<IndexModel.TestResultCookie>(oldJson);
                    if (oldRes != null && score < oldRes.Score) shouldUpdate = false;
                    else if (oldRes != null && score == oldRes.Score && time >= oldRes.TimeSeconds) shouldUpdate = false;
                }
                catch { }
            }

            if (shouldUpdate)
            {
                dict[testId] = new { Score = score, TimeSeconds = time };
                var options = new CookieOptions { Expires = DateTime.Now.AddYears(1) };
                Response.Cookies.Append(cookieKey, JsonSerializer.Serialize(dict), options);
            }
        }
    }
}