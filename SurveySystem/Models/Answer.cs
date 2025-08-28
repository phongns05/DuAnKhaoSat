using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Answer
{
    [Key]
    [Column("AnswerID")]
    public int AnswerId { get; set; }

    [Column("AttemptID")]
    public int AttemptId { get; set; }

    [Column("QuestionID")]
    public int QuestionId { get; set; }

    [Column("OptionID")]
    public int? OptionId { get; set; }

    public string? AnswerText { get; set; }

    public bool? IsCorrect { get; set; }

    [ForeignKey("AttemptId")]
    [InverseProperty("Answers")]
    public virtual TestAttempt Attempt { get; set; } = null!;

    [ForeignKey("OptionId")]
    [InverseProperty("Answers")]
    public virtual QuestionOption? Option { get; set; }

    [ForeignKey("QuestionId")]
    [InverseProperty("Answers")]
    public virtual Question Question { get; set; } = null!;
}
