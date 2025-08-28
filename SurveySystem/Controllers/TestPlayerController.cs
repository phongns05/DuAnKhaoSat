using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveySystem.Models;

namespace SurveySystem.Controllers
{
    [Authorize]
    public class TestPlayerController : Controller
    {
        private readonly AppDbContext _db;

        public TestPlayerController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            var now = DateTime.UtcNow;
            var assigned = await _db.Assignments
                .Include(a => a.Test)
                .Where(a => (a.UserId == userId || a.UserId == null) && (a.Deadline == null || a.Deadline > now))
                .Select(a => a.Test)
                .Distinct()
                .ToListAsync();

            var attempts = await _db.TestAttempts
                .Where(t => t.UserId == userId)
                .ToListAsync();

            ViewData["Attempts"] = attempts;
            return View(assigned);
        }

        [HttpGet]
        public async Task<IActionResult> Take(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            var test = await _db.Tests
                .Include(t => t.TestQuestions)
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .FirstOrDefaultAsync(t => t.TestId == id);
            if (test == null) return NotFound();

            var attempt = await _db.TestAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TestId == id && a.Status == "InProgress");

            if (attempt == null)
            {
                attempt = new TestAttempt
                {
                    UserId = userId,
                    TestId = id,
                    StartTime = DateTime.UtcNow,
                    Status = "InProgress"
                };
                _db.TestAttempts.Add(attempt);
                await _db.SaveChangesAsync();
                await _db.Entry(attempt).Collection(a => a.Answers).LoadAsync();
            }

            ViewData["AttemptId"] = attempt.AttemptId;
            return View((test, attempt));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(int attemptId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _db.TestAttempts.Include(a => a.Test)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId);
            if (attempt == null) return NotFound();

            await PersistAnswersFromForm(attemptId);
            TempData["Message"] = "Đã lưu nháp";
            return RedirectToAction("Take", new { id = attempt.TestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int attemptId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _db.TestAttempts
                .Include(a => a.Test)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId);
            if (attempt == null) return NotFound();

            await PersistAnswersFromForm(attemptId);

            // Auto-grade MCQ/TrueFalse by options IsCorrect
            var answers = await _db.Answers
                .Include(ans => ans.Question)
                .Include(ans => ans.Option)
                .Where(ans => ans.AttemptId == attemptId)
                .ToListAsync();

            int totalGraded = 0;
            int correctCount = 0;
            foreach (var ans in answers)
            {
                var type = ans.Question.QuestionType?.ToLowerInvariant() ?? "";
                if (type.Contains("mcq") || type.Contains("true") || type.Contains("false") || type.Contains("tf"))
                {
                    totalGraded++;
                    ans.IsCorrect = ans.Option != null && (ans.Option.IsCorrect ?? false);
                    if (ans.IsCorrect == true) correctCount++;
                }
            }
            decimal score = 0;
            if (totalGraded > 0)
            {
                score = Math.Round((decimal)correctCount / totalGraded * 100m, 2);
            }

            attempt.Score = score;
            attempt.EndTime = DateTime.UtcNow;
            attempt.Status = "Submitted";
            await _db.SaveChangesAsync();

            return RedirectToAction("Result", new { id = attempt.AttemptId });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _db.TestAttempts
                .Include(a => a.Test)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.Question)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.Option)
                .FirstOrDefaultAsync(a => a.AttemptId == id && a.UserId == userId);
            if (attempt == null) return NotFound();
            return View(attempt);
        }

        private async Task PersistAnswersFromForm(int attemptId)
        {
            // Expect inputs: q_{questionId} for MCQ/TrueFalse (value=optionId); qtext_{questionId} for essay
            var attempt = await _db.TestAttempts.FirstAsync(a => a.AttemptId == attemptId);

            // Load existing answers
            var existing = await _db.Answers.Where(a => a.AttemptId == attemptId).ToListAsync();
            var existingByQuestion = existing.ToDictionary(a => a.QuestionId, a => a);

            // Parse form
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("q_"))
                {
                    if (int.TryParse(key.AsSpan(2), out var qid))
                    {
                        var value = Request.Form[key].ToString();
                        int.TryParse(value, out var optionId);
                        if (!existingByQuestion.TryGetValue(qid, out var ans))
                        {
                            ans = new Answer { AttemptId = attempt.AttemptId, QuestionId = qid };
                            _db.Answers.Add(ans);
                            existingByQuestion[qid] = ans;
                        }
                        ans.OptionId = optionId == 0 ? null : optionId;
                        ans.AnswerText = null;
                    }
                }
                else if (key.StartsWith("qtext_"))
                {
                    if (int.TryParse(key.AsSpan(6), out var qid))
                    {
                        var value = Request.Form[key].ToString();
                        if (!existingByQuestion.TryGetValue(qid, out var ans))
                        {
                            ans = new Answer { AttemptId = attempt.AttemptId, QuestionId = qid };
                            _db.Answers.Add(ans);
                            existingByQuestion[qid] = ans;
                        }
                        ans.OptionId = null;
                        ans.AnswerText = value;
                    }
                }
            }
            await _db.SaveChangesAsync();
        }
    }
}


