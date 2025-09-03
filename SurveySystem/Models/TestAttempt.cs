using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class TestAttempt
{
    [Key]
    [Column("AttemptID")]
    public int AttemptId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("TestID")]
    public int TestId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? StartTime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? EndTime { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? Score { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [InverseProperty("Attempt")]
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    [ForeignKey("TestId")]
    [InverseProperty("TestAttempts")]
    public virtual Test? Test { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("TestAttempts")]
    public virtual User? User { get; set; }
}
