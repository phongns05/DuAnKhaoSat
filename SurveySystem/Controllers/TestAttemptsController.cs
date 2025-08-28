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
    public class TestAttemptsController : Controller
    {
        private readonly AppDbContext _context;

        public TestAttemptsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TestAttempts
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.TestAttempts.Include(t => t.Test).Include(t => t.User);
            return View(await appDbContext.ToListAsync());
        }

        // GET: TestAttempts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testAttempt = await _context.TestAttempts
                .Include(t => t.Test)
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.AttemptId == id);
            if (testAttempt == null)
            {
                return NotFound();
            }

            return View(testAttempt);
        }

        // GET: TestAttempts/Create
        public IActionResult Create()
        {
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: TestAttempts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AttemptId,UserId,TestId,StartTime,EndTime,Score,Status")] TestAttempt testAttempt)
        {
            if (ModelState.IsValid)
            {
                _context.Add(testAttempt);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testAttempt.TestId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", testAttempt.UserId);
            return View(testAttempt);
        }

        // GET: TestAttempts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testAttempt = await _context.TestAttempts.FindAsync(id);
            if (testAttempt == null)
            {
                return NotFound();
            }
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testAttempt.TestId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", testAttempt.UserId);
            return View(testAttempt);
        }

        // POST: TestAttempts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AttemptId,UserId,TestId,StartTime,EndTime,Score,Status")] TestAttempt testAttempt)
        {
            if (id != testAttempt.AttemptId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(testAttempt);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestAttemptExists(testAttempt.AttemptId))
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
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testAttempt.TestId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", testAttempt.UserId);
            return View(testAttempt);
        }

        // GET: TestAttempts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testAttempt = await _context.TestAttempts
                .Include(t => t.Test)
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.AttemptId == id);
            if (testAttempt == null)
            {
                return NotFound();
            }

            return View(testAttempt);
        }

        // POST: TestAttempts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var testAttempt = await _context.TestAttempts.FindAsync(id);
            if (testAttempt != null)
            {
                _context.TestAttempts.Remove(testAttempt);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TestAttemptExists(int id)
        {
            return _context.TestAttempts.Any(e => e.AttemptId == id);
        }
    }
}
