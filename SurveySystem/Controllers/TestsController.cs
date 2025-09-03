using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SurveySystem.Models;
using System.Security.Claims;

namespace SurveySystem.Controllers
{
    [Authorize]
    public class TestsController : Controller
    {
        private readonly AppDbContext _context;

        public TestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Tests
        public async Task<IActionResult> Index()
        {
            var tests = await _context.Tests
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.TestQuestions)
                .ThenInclude(tq => tq.Question)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
            return View(tests);
        }

        // GET: Tests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var test = await _context.Tests
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.TestQuestions)
                .ThenInclude(tq => tq.Question)
                .ThenInclude(q => q.QuestionOptions)
                .FirstOrDefaultAsync(m => m.TestId == id);
            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        // GET: Tests/Create
        public async Task<IActionResult> Create()
        {
            // Get available questions for selection
            var questions = await _context.Questions
                .Include(q => q.Skill)
                .Include(q => q.Difficulty)
                .Include(q => q.QuestionOptions)
                .OrderBy(q => q.Skill.SkillName)
                .ThenBy(q => q.Difficulty.LevelName)
                .ToListAsync();

            ViewBag.Questions = questions;
            return View();
        }

        // POST: Tests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Duration,PassScore")] Test test, 
            int[] selectedQuestions)
        {
            // Debug: Log selected questions
            System.Diagnostics.Debug.WriteLine($"DEBUG - Selected Questions Count: {selectedQuestions?.Length ?? 0}");
            if (selectedQuestions != null && selectedQuestions.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG - Selected Questions: {string.Join(", ", selectedQuestions)}");
            }
            
            // Get first user
            var firstUser = await _context.Users.FirstOrDefaultAsync();
            if (firstUser == null)
            {
                ModelState.AddModelError("", "Không có user nào trong hệ thống");
                var questions = await _context.Questions
                    .Include(q => q.Skill)
                    .Include(q => q.Difficulty)
                    .Include(q => q.QuestionOptions)
                    .OrderBy(q => q.Skill.SkillName)
                    .ThenBy(q => q.Difficulty.LevelName)
                    .ToListAsync();
                ViewBag.Questions = questions;
                return View(test);
            }

            // Set test properties
            test.CreatedBy = firstUser.UserId;
            test.CreatedDate = DateTime.UtcNow;

            // Clear ModelState errors for properties we're setting manually
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");

            // Validate model
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(test);
                    await _context.SaveChangesAsync();

                    // Add selected questions to test
                    if (selectedQuestions != null && selectedQuestions.Length > 0)
                    {
                        foreach (var questionId in selectedQuestions)
                        {
                            var testQuestion = new TestQuestion
                            {
                                TestId = test.TestId,
                                QuestionId = questionId
                            };
                            _context.TestQuestions.Add(testQuestion);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["Message"] = "Tạo bài test thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi lưu: {ex.Message}");
                }
            }

            // Reload questions for view
            var reloadQuestions = await _context.Questions
                .Include(q => q.Skill)
                .Include(q => q.Difficulty)
                .Include(q => q.QuestionOptions)
                .OrderBy(q => q.Skill.SkillName)
                .ThenBy(q => q.Difficulty.LevelName)
                .ToListAsync();

            ViewBag.Questions = reloadQuestions;
            return View(test);
        }

        // GET: Tests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var test = await _context.Tests
                .Include(t => t.TestQuestions)
                .FirstOrDefaultAsync(t => t.TestId == id);
            if (test == null)
            {
                return NotFound();
            }

            // Get available questions for selection
            var questions = await _context.Questions
                .Include(q => q.Skill)
                .Include(q => q.Difficulty)
                .Include(q => q.QuestionOptions)
                .OrderBy(q => q.Skill.SkillName)
                .ThenBy(q => q.Difficulty.LevelName)
                .ToListAsync();

            ViewBag.Questions = questions;
            ViewBag.SelectedQuestions = test.TestQuestions.Select(tq => tq.QuestionId).ToList();

            return View(test);
        }

        // POST: Tests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TestId,Title,Description,Duration,PassScore,CreatedBy,CreatedDate")] Test test,
            int[] selectedQuestions)
        {
            if (id != test.TestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(test);
                    await _context.SaveChangesAsync();

                    // Update test questions
                    var existingTestQuestions = await _context.TestQuestions
                        .Where(tq => tq.TestId == id)
                        .ToListAsync();
                    _context.TestQuestions.RemoveRange(existingTestQuestions);

                    if (selectedQuestions != null && selectedQuestions.Length > 0)
                    {
                        foreach (var questionId in selectedQuestions)
                        {
                            var testQuestion = new TestQuestion
                            {
                                TestId = test.TestId,
                                QuestionId = questionId
                            };
                            _context.TestQuestions.Add(testQuestion);
                        }
                    }
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Cập nhật bài test thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestExists(test.TestId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Reload questions for view
            var questions = await _context.Questions
                .Include(q => q.Skill)
                .Include(q => q.Difficulty)
                .Include(q => q.QuestionOptions)
                .OrderBy(q => q.Skill.SkillName)
                .ThenBy(q => q.Difficulty.LevelName)
                .ToListAsync();

            ViewBag.Questions = questions;
            ViewBag.SelectedQuestions = selectedQuestions ?? new int[0];

            return View(test);
        }

        // GET: Tests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var test = await _context.Tests
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.TestQuestions)
                .ThenInclude(tq => tq.Question)
                .FirstOrDefaultAsync(m => m.TestId == id);
            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        // POST: Tests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var test = await _context.Tests.FindAsync(id);
            if (test != null)
            {
                _context.Tests.Remove(test);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Xóa bài test thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool TestExists(int id)
        {
            return _context.Tests.Any(e => e.TestId == id);
        }

        // Test action để kiểm tra
        [AllowAnonymous]
        public async Task<IActionResult> TestCreate()
        {
            try
            {
                // Lấy user đầu tiên
                var user = await _context.Users.FirstOrDefaultAsync();
                if (user == null)
                {
                    return Json(new { success = false, error = "Không có user nào" });
                }

                // Tạo test đơn giản
                var test = new Test
                {
                    Title = "Test Bài Test",
                    Description = "Mô tả test",
                    Duration = 30,
                    PassScore = 70,
                    CreatedBy = user.UserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Add(test);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    testId = test.TestId, 
                    message = "Tạo test thành công",
                    user = new { user.UserId, user.Email, user.FullName }
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace 
                });
            }
        }

    }
}
