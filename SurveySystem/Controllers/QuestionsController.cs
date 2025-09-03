using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SurveySystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SurveySystem.Services;

namespace SurveySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QuestionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ExcelService _excelService;

        public QuestionsController(AppDbContext context, ExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        // GET: Questions
        public async Task<IActionResult> Index()
        {
            var questions = await _context.Questions
                .Include(q => q.CreatedByNavigation)
                .Include(q => q.Difficulty)
                .Include(q => q.Skill)
                .Include(q => q.QuestionOptions)
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();
            return View(questions);
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.CreatedByNavigation)
                .Include(q => q.Difficulty)
                .Include(q => q.Skill)
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(m => m.QuestionId == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // GET: Questions/Create
        public async Task<IActionResult> Create()
        {
            ViewData["DifficultyId"] = new SelectList(await _context.Difficulties.ToListAsync(), "DifficultyId", "LevelName");
            ViewData["SkillId"] = new SelectList(await _context.Skills.ToListAsync(), "SkillId", "SkillName");
            
            var questionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "MCQ", Text = "Trắc nghiệm (MCQ)" },
                new SelectListItem { Value = "TrueFalse", Text = "Đúng/Sai" },
                new SelectListItem { Value = "Essay", Text = "Tự luận" }
            };
            ViewData["QuestionTypes"] = questionTypes;
            
            return View();
        }

        // POST: Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Content,QuestionType,SkillId,DifficultyId")] Question question, 
            List<string> options, List<bool> isCorrect)
        {
            if (ModelState.IsValid)
            {
                // Set created by current user
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                question.CreatedBy = userId;
                question.CreatedDate = DateTime.UtcNow;

                _context.Add(question);
                await _context.SaveChangesAsync();

                // Add options for MCQ and TrueFalse questions
                if ((question.QuestionType == "MCQ" || question.QuestionType == "TrueFalse") && options != null)
                {
                    for (int i = 0; i < options.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(options[i]))
                        {
                            var option = new QuestionOption
                            {
                                QuestionId = question.QuestionId,
                                Content = options[i],
                                IsCorrect = isCorrect != null && i < isCorrect.Count ? isCorrect[i] : false
                            };
                            _context.QuestionOptions.Add(option);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Message"] = "Tạo câu hỏi thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["DifficultyId"] = new SelectList(await _context.Difficulties.ToListAsync(), "DifficultyId", "LevelName", question.DifficultyId);
            ViewData["SkillId"] = new SelectList(await _context.Skills.ToListAsync(), "SkillId", "SkillName", question.SkillId);
            
            var questionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "MCQ", Text = "Trắc nghiệm (MCQ)" },
                new SelectListItem { Value = "TrueFalse", Text = "Đúng/Sai" },
                new SelectListItem { Value = "Essay", Text = "Tự luận" }
            };
            ViewData["QuestionTypes"] = questionTypes;
            
            return View(question);
        }

        // GET: Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(q => q.QuestionId == id);
            if (question == null)
            {
                return NotFound();
            }

            ViewData["DifficultyId"] = new SelectList(await _context.Difficulties.ToListAsync(), "DifficultyId", "LevelName", question.DifficultyId);
            ViewData["SkillId"] = new SelectList(await _context.Skills.ToListAsync(), "SkillId", "SkillName", question.SkillId);
            
            var questionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "MCQ", Text = "Trắc nghiệm (MCQ)" },
                new SelectListItem { Value = "TrueFalse", Text = "Đúng/Sai" },
                new SelectListItem { Value = "Essay", Text = "Tự luận" }
            };
            ViewData["QuestionTypes"] = questionTypes;
            
            return View(question);
        }

        // POST: Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("QuestionId,Content,QuestionType,SkillId,DifficultyId,CreatedBy,CreatedDate")] Question question,
            List<string> options, List<bool> isCorrect)
        {
            if (id != question.QuestionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(question);
                    await _context.SaveChangesAsync();

                    // Update options
                    if ((question.QuestionType == "MCQ" || question.QuestionType == "TrueFalse") && options != null)
                    {
                        // Remove existing options
                        var existingOptions = await _context.QuestionOptions.Where(o => o.QuestionId == id).ToListAsync();
                        _context.QuestionOptions.RemoveRange(existingOptions);

                        // Add new options
                        for (int i = 0; i < options.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(options[i]))
                            {
                                var option = new QuestionOption
                                {
                                    QuestionId = question.QuestionId,
                                    Content = options[i],
                                    IsCorrect = isCorrect != null && i < isCorrect.Count ? isCorrect[i] : false
                                };
                                _context.QuestionOptions.Add(option);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["Message"] = "Cập nhật câu hỏi thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.QuestionId))
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

            ViewData["DifficultyId"] = new SelectList(await _context.Difficulties.ToListAsync(), "DifficultyId", "LevelName", question.DifficultyId);
            ViewData["SkillId"] = new SelectList(await _context.Skills.ToListAsync(), "SkillId", "SkillName", question.SkillId);
            
            var questionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "MCQ", Text = "Trắc nghiệm (MCQ)" },
                new SelectListItem { Value = "TrueFalse", Text = "Đúng/Sai" },
                new SelectListItem { Value = "Essay", Text = "Tự luận" }
            };
            ViewData["QuestionTypes"] = questionTypes;
            
            return View(question);
        }

        // GET: Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.CreatedByNavigation)
                .Include(q => q.Difficulty)
                .Include(q => q.Skill)
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(m => m.QuestionId == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Xóa câu hỏi thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.QuestionId == id);
        }

        // GET: Questions/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: Questions/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel để import.";
                return RedirectToAction(nameof(Import));
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _excelService.ImportQuestionsFromExcelAsync(file, userId);

            if (result.success)
            {
                TempData["Message"] = result.message;
            }
            else
            {
                TempData["Error"] = result.message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Questions/Export
        public IActionResult Export()
        {
            var excelBytes = _excelService.ExportQuestionsToExcel();
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Questions_Template.xlsx");
        }
    }
}
