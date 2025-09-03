using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveySystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace SurveySystem.Controllers
{
    [AllowAnonymous]
    public class DataController : Controller
    {
        private readonly AppDbContext _db;

        public DataController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Seed()
        {
            try
            {
                var result = "<h3>Seeding Sample Data...</h3>";

                // Seed Skills
                if (!await _db.Skills.AnyAsync())
                {
                    var skills = new List<Skill>
                    {
                        new Skill { SkillName = "Programming" },
                        new Skill { SkillName = "Database" },
                        new Skill { SkillName = "Testing" },
                        new Skill { SkillName = "Business Analysis" },
                        new Skill { SkillName = "Project Management" },
                        new Skill { SkillName = "UI/UX Design" },
                        new Skill { SkillName = "DevOps" },
                        new Skill { SkillName = "Mobile Development" }
                    };
                    _db.Skills.AddRange(skills);
                    await _db.SaveChangesAsync();
                    result += "Created Skills<br/>";
                }

                // Seed Difficulties
                if (!await _db.Difficulties.AnyAsync())
                {
                    var difficulties = new List<Difficulty>
                    {
                        new Difficulty { LevelName = "Junior" },
                        new Difficulty { LevelName = "Middle" },
                        new Difficulty { LevelName = "Senior" }
                    };
                    _db.Difficulties.AddRange(difficulties);
                    await _db.SaveChangesAsync();
                    result += "Created Difficulties<br/>";
                }

                result += "<br/>Sample data created successfully!<br/>";
                result += "<a href='/Questions/Create'>Create Questions</a><br/>";
                result += "<a href='/Tests/Create'>Create Tests</a>";

                return Content(result, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}", "text/html");
            }
        }
    }
}
