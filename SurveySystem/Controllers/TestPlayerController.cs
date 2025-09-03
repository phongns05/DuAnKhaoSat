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
            
            // Get assigned tests
            var assignments = await _db.Assignments
                .Include(a => a.Test)
                .ThenInclude(t => t.TestQuestions)
                .Where(a => a.UserId == userId && (a.Deadline == null || a.Deadline > now))
                .ToListAsync();

            // Get test attempts
            var attempts = await _db.TestAttempts
                .Include(t => t.Test)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.StartTime)
                .ToListAsync();

            ViewData["Attempts"] = attempts;
            return View(assignments);
        }

        [HttpGet]
        public async Task<IActionResult> Take(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            // Check if user is assigned to this test
            var assignment = await _db.Assignments
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TestId == id);
            if (assignment == null)
            {
                TempData["Error"] = "Bạn không được phân công bài test này.";
                return RedirectToAction("Index");
            }

            var test = await _db.Tests
                .Include(t => t.TestQuestions)
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .FirstOrDefaultAsync(t => t.TestId == id);
            if (test == null) return NotFound();

            // Check if test is expired
            if (assignment.Deadline.HasValue && assignment.Deadline.Value < DateTime.UtcNow)
            {
                TempData["Error"] = "Bài test đã hết hạn.";
                return RedirectToAction("Index");
            }

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
            ViewData["Assignment"] = assignment;
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
            TempData["Success"] = "Đã lưu nháp thành công! Bạn có thể tiếp tục làm bài sau.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int attemptId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _db.TestAttempts
                .Include(a => a.Test)
                .ThenInclude(t => t.TestQuestions)
                .ThenInclude(tq => tq.Question)
                .ThenInclude(q => q.QuestionOptions)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId);
            if (attempt == null) return NotFound();

            // Save answers
            await PersistAnswersFromForm(attemptId);

            // Auto-grade MCQ and TrueFalse questions
            var totalScore = 0;
            var totalQuestions = attempt.Test.TestQuestions.Count;
            var autoGradedQuestions = 0;


            System.Diagnostics.Debug.WriteLine($"=== GRADING ATTEMPT {attempt.AttemptId} ===");
            System.Diagnostics.Debug.WriteLine($"Total questions: {totalQuestions}");

            foreach (var testQuestion in attempt.Test.TestQuestions)
            {
                var question = testQuestion.Question;
                var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);

                System.Diagnostics.Debug.WriteLine($"\nQuestion {question.QuestionId}: {question.Content}");
                System.Diagnostics.Debug.WriteLine($"Question Type: {question.QuestionType}");
                System.Diagnostics.Debug.WriteLine($"Answer: {answer?.AnswerText ?? "No answer"}");
                System.Diagnostics.Debug.WriteLine($"Question Options Count: {question.QuestionOptions.Count}");

                if (question.QuestionType == "MCQ" || question.QuestionType == "TrueFalse")
                {
                    autoGradedQuestions++;
                    var isCorrect = answer != null && IsAnswerCorrect(question, answer.AnswerText);
                    if (isCorrect)
                    {
                        totalScore++;
                        System.Diagnostics.Debug.WriteLine("✓ CORRECT");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✗ INCORRECT");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Manual grading required");
                }
            }

            System.Diagnostics.Debug.WriteLine($"\n=== FINAL SCORE ===");
            System.Diagnostics.Debug.WriteLine($"Total Score: {totalScore}/{totalQuestions}");
            System.Diagnostics.Debug.WriteLine($"Auto-graded: {autoGradedQuestions}/{totalQuestions}");

            // Calculate percentage score
            var percentageScore = totalQuestions > 0 ? (decimal)((double)totalScore / totalQuestions * 100) : 0;

            // Update attempt
            attempt.EndTime = DateTime.UtcNow;
            attempt.Score = percentageScore;
            attempt.Status = autoGradedQuestions == totalQuestions ? "Completed" : "PendingReview";

            await _db.SaveChangesAsync();

            TempData["Success"] = "Nộp bài thành công!";
            return RedirectToAction("Result", new { attemptId = attempt.AttemptId });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int attemptId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Account");

            var attempt = await _db.TestAttempts
                .Include(a => a.Test)
                .ThenInclude(t => t.TestQuestions)
                .ThenInclude(tq => tq.Question)
                .ThenInclude(q => q.QuestionOptions)
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId);
            if (attempt == null) return NotFound();

            return View(attempt);
        }

        private async Task PersistAnswersFromForm(int attemptId)
        {
            var attempt = await _db.TestAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
            if (attempt == null) return;

            var form = Request.Form;
            var questionAnswers = new Dictionary<int, List<string>>();

            // Collect all form values grouped by question ID
            foreach (var key in form.Keys)
            {
                if (key.StartsWith("answer_"))
                {
                    // Extract question ID from key (handle both "answer_X" format)
                    var questionIdStr = key.Substring(7); // Remove "answer_" prefix
                    
                    if (int.TryParse(questionIdStr, out var questionId))
                    {
                        if (!questionAnswers.ContainsKey(questionId))
                        {
                            questionAnswers[questionId] = new List<string>();
                        }

                        // Get all values for this key (for MCQ multiple selections)
                        var values = form[key];
                        foreach (var value in values)
                        {
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                questionAnswers[questionId].Add(value);
                            }
                        }
                    }
                }
            }

            // Save answers to database
            foreach (var kvp in questionAnswers)
            {
                var questionId = kvp.Key;
                var answerText = string.Join(",", kvp.Value); // Join multiple values with comma

                var existingAnswer = attempt.Answers.FirstOrDefault(a => a.QuestionId == questionId);
                if (existingAnswer != null)
                {
                    existingAnswer.AnswerText = answerText;
                }
                else
                {
                    attempt.Answers.Add(new Answer
                    {
                        QuestionId = questionId,
                        AnswerText = answerText
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        private bool IsAnswerCorrect(Question question, string userAnswer)
        {
            if (string.IsNullOrEmpty(userAnswer))
                return false;

            if (question.QuestionType == "MCQ")
            {
                // For MCQ, check if user selected all correct options
                var correctOptions = question.QuestionOptions
                    .Where(o => o.IsCorrect == true)
                    .Select(o => o.Content?.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();
                
                var userAnswers = userAnswer.Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();
                
                // Debug logging
                System.Diagnostics.Debug.WriteLine($"Question: {question.Content}");
                System.Diagnostics.Debug.WriteLine($"Correct options: {string.Join(", ", correctOptions)}");
                System.Diagnostics.Debug.WriteLine($"User answers: {string.Join(", ", userAnswers)}");
                System.Diagnostics.Debug.WriteLine($"Correct count: {correctOptions.Count}, User count: {userAnswers.Count}");
                
                if (correctOptions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No correct options found!");
                    return false;
                }
                
                var isCorrect = correctOptions.Count == userAnswers.Count && 
                               correctOptions.All(o => userAnswers.Contains(o));
                
                System.Diagnostics.Debug.WriteLine($"Is correct: {isCorrect}");
                return isCorrect;
            }
            else if (question.QuestionType == "TrueFalse")
            {
                // For TrueFalse, check if user selected the correct option
                var correctOption = question.QuestionOptions.FirstOrDefault(o => o.IsCorrect == true);
                var isCorrect = correctOption != null && 
                               !string.IsNullOrEmpty(correctOption.Content) && 
                               correctOption.Content.Trim() == userAnswer.Trim();
                
                System.Diagnostics.Debug.WriteLine($"TrueFalse - Correct: {correctOption?.Content}, User: {userAnswer}, IsCorrect: {isCorrect}");
                return isCorrect;
            }

            return false;
        }
    }
}


