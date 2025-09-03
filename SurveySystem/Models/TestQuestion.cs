using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

[PrimaryKey("TestId", "QuestionId")]
public partial class TestQuestion
{
    [Key]
    [Column("TestID")]
    public int TestId { get; set; }

    [Key]
    [Column("QuestionID")]
    public int QuestionId { get; set; }

    public int OrderNo { get; set; }

    [ForeignKey("QuestionId")]
    [InverseProperty("TestQuestions")]
    public virtual Question? Question { get; set; }

    [ForeignKey("TestId")]
    [InverseProperty("TestQuestions")]
    public virtual Test? Test { get; set; }
}
