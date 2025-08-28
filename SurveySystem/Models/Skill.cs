using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Skill
{
    [Key]
    [Column("SkillID")]
    public int SkillId { get; set; }

    [StringLength(100)]
    public string SkillName { get; set; } = null!;

    [InverseProperty("Skill")]
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
