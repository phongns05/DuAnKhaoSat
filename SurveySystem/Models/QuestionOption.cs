using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class QuestionOption
{
    [Key]
    [Column("OptionID")]
    public int OptionId { get; set; }

    [Column("QuestionID")]
    public int QuestionId { get; set; }

    [StringLength(500)]
    public string Content { get; set; } = null!;

    public bool? IsCorrect { get; set; }

    [InverseProperty("Option")]
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    [ForeignKey("QuestionId")]
    [InverseProperty("QuestionOptions")]
    public virtual Question? Question { get; set; }
}
