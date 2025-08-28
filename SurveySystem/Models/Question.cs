using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Question
{
    [Key]
    [Column("QuestionID")]
    public int QuestionId { get; set; }

    public string Content { get; set; } = null!;

    [StringLength(50)]
    public string QuestionType { get; set; } = null!;

    [Column("SkillID")]
    public int? SkillId { get; set; }

    [Column("DifficultyID")]
    public int? DifficultyId { get; set; }

    public int? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [InverseProperty("Question")]
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Questions")]
    public virtual User? CreatedByNavigation { get; set; }

    [ForeignKey("DifficultyId")]
    [InverseProperty("Questions")]
    public virtual Difficulty? Difficulty { get; set; }

    [InverseProperty("Question")]
    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();

    [ForeignKey("SkillId")]
    [InverseProperty("Questions")]
    public virtual Skill? Skill { get; set; }

    [InverseProperty("Question")]
    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();
}
