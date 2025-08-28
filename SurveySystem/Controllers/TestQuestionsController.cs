using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SurveySystem.Models;

namespace SurveySystem.Controllers
{
    public class TestQuestionsController : Controller
    {
        private readonly AppDbContext _context;

        public TestQuestionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TestQuestions
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.TestQuestions.Include(t => t.Question).Include(t => t.Test);
            return View(await appDbContext.ToListAsync());
        }

        // GET: TestQuestions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testQuestion = await _context.TestQuestions
                .Include(t => t.Question)
                .Include(t => t.Test)
                .FirstOrDefaultAsync(m => m.TestId == id);
            if (testQuestion == null)
            {
                return NotFound();
            }

            return View(testQuestion);
        }

        // GET: TestQuestions/Create
        public IActionResult Create()
        {
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId");
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId");
            return View();
        }

        // POST: TestQuestions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TestId,QuestionId,OrderNo")] TestQuestion testQuestion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(testQuestion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId", testQuestion.QuestionId);
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testQuestion.TestId);
            return View(testQuestion);
        }

        // GET: TestQuestions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testQuestion = await _context.TestQuestions.FindAsync(id);
            if (testQuestion == null)
            {
                return NotFound();
            }
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId", testQuestion.QuestionId);
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testQuestion.TestId);
            return View(testQuestion);
        }

        // POST: TestQuestions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TestId,QuestionId,OrderNo")] TestQuestion testQuestion)
        {
            if (id != testQuestion.TestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(testQuestion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestQuestionExists(testQuestion.TestId))
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
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId", testQuestion.QuestionId);
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testQuestion.TestId);
            return View(testQuestion);
        }

        // GET: TestQuestions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testQuestion = await _context.TestQuestions
                .Include(t => t.Question)
                .Include(t => t.Test)
                .FirstOrDefaultAsync(m => m.TestId == id);
            if (testQuestion == null)
            {
                return NotFound();
            }

            return View(testQuestion);
        }

        // POST: TestQuestions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var testQuestion = await _context.TestQuestions.FindAsync(id);
            if (testQuestion != null)
            {
                _context.TestQuestions.Remove(testQuestion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TestQuestionExists(int id)
        {
            return _context.TestQuestions.Any(e => e.TestId == id);
        }
    }
}
