using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Test
{
    [Key]
    [Column("TestID")]
    public int TestId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public int Duration { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PassScore { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [InverseProperty("Test")]
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Tests")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Test")]
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [InverseProperty("Test")]
    public virtual ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();

    [InverseProperty("Test")]
    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();
}
