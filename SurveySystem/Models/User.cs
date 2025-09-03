using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

[Index("Email", Name = "UQ__Users__A9D105348D0D91F8", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Column("RoleID")]
    public int RoleId { get; set; }

    [StringLength(50)]
    public string? Level { get; set; }

    public bool? Status { get; set; }

    [StringLength(100)]
    public string? ResetToken { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ResetTokenExpiry { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("User")]
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role? Role { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<Department> Depts { get; set; } = new List<Department>();
}
