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
    [Authorize(Roles = "Admin")]
    public class AssignmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AssignmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Assignments
        public async Task<IActionResult> Index()
        {
            var assignments = await _context.Assignments
                .Include(a => a.Dept)
                .Include(a => a.Test)
                .Include(a => a.User)
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();
            return View(assignments);
        }

        // GET: Assignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.Assignments
                .Include(a => a.Dept)
                .Include(a => a.Test)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AssignId == id);
            if (assignment == null)
            {
                return NotFound();
            }

            return View(assignment);
        }

        // GET: Assignments/Create
        public async Task<IActionResult> Create()
        {
            var tests = await _context.Tests
                .Include(t => t.TestQuestions)
                .OrderBy(t => t.Title)
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Depts)
                .Where(u => u.Status == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var departments = await _context.Departments
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            ViewBag.Tests = tests;
            ViewBag.Users = users;
            ViewBag.Departments = departments;
            return View();
        }

        // POST: Assignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TestId,DeptId,Deadline")] Assignment assignment, 
            List<int> selectedUsers)
        {
            if (ModelState.IsValid)
            {
                var assignedCount = 0;
                var errors = new List<string>();

                if (selectedUsers != null && selectedUsers.Any())
                {
                    foreach (var userId in selectedUsers)
                    {
                        // Check if assignment already exists
                        var existingAssignment = await _context.Assignments
                            .FirstOrDefaultAsync(a => a.TestId == assignment.TestId && a.UserId == userId);

                        if (existingAssignment == null)
                        {
                            var newAssignment = new Assignment
                            {
                                TestId = assignment.TestId,
                                UserId = userId,
                                DeptId = assignment.DeptId,
                                AssignedDate = DateTime.UtcNow,
                                Deadline = assignment.Deadline
                            };
                            _context.Assignments.Add(newAssignment);
                            assignedCount++;
                        }
                        else
                        {
                            errors.Add($"User {userId} đã được phân công bài test này");
                        }
                    }

                    if (assignedCount > 0)
                    {
                        await _context.SaveChangesAsync();
                        TempData["Message"] = $"Phân công thành công {assignedCount} bài test!";
                        
                        if (errors.Any())
                        {
                            TempData["Warning"] = string.Join(", ", errors);
                        }
                    }
                    else
                    {
                        TempData["Error"] = "Không có bài test nào được phân công mới.";
                    }
                }
                else
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất một user.";
                }

                return RedirectToAction(nameof(Index));
            }

            // Reload data for view
            var tests = await _context.Tests
                .Include(t => t.TestQuestions)
                .OrderBy(t => t.Title)
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Depts)
                .Where(u => u.Status == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var departments = await _context.Departments
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            ViewBag.Tests = tests;
            ViewBag.Users = users;
            ViewBag.Departments = departments;
            return View(assignment);
        }

        // GET: Assignments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.Assignments
                .Include(a => a.Test)
                .Include(a => a.User)
                .Include(a => a.Dept)
                .FirstOrDefaultAsync(a => a.AssignId == id);
            if (assignment == null)
            {
                return NotFound();
            }

            var tests = await _context.Tests
                .Include(t => t.TestQuestions)
                .OrderBy(t => t.Title)
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Depts)
                .Where(u => u.Status == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var departments = await _context.Departments
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            ViewBag.Tests = tests;
            ViewBag.Users = users;
            ViewBag.Departments = departments;
            return View(assignment);
        }

        // POST: Assignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AssignId,TestId,UserId,DeptId,AssignedDate,Deadline")] Assignment assignment)
        {
            if (id != assignment.AssignId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(assignment);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Cập nhật phân công thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssignmentExists(assignment.AssignId))
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

            var tests = await _context.Tests
                .Include(t => t.TestQuestions)
                .OrderBy(t => t.Title)
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Depts)
                .Where(u => u.Status == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var departments = await _context.Departments
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            ViewBag.Tests = tests;
            ViewBag.Users = users;
            ViewBag.Departments = departments;
            return View(assignment);
        }

        // GET: Assignments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.Assignments
                .Include(a => a.Dept)
                .Include(a => a.Test)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AssignId == id);
            if (assignment == null)
            {
                return NotFound();
            }

            return View(assignment);
        }

        // POST: Assignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment != null)
            {
                _context.Assignments.Remove(assignment);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Xóa phân công thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool AssignmentExists(int id)
        {
            return _context.Assignments.Any(e => e.AssignId == id);
        }
    }
}
