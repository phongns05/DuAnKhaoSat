using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Difficulty
{
    [Key]
    [Column("DifficultyID")]
    public int DifficultyId { get; set; }

    [StringLength(50)]
    public string LevelName { get; set; } = null!;

    [InverseProperty("Difficulty")]
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
