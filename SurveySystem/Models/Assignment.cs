using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Assignment
{
    [Key]
    [Column("AssignID")]
    public int AssignId { get; set; }

    [Column("TestID")]
    public int TestId { get; set; }

    [Column("UserID")]
    public int? UserId { get; set; }

    [Column("DeptID")]
    public int? DeptId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? AssignedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Deadline { get; set; }

    [ForeignKey("DeptId")]
    [InverseProperty("Assignments")]
    public virtual Department? Dept { get; set; }

    [ForeignKey("TestId")]
    [InverseProperty("Assignments")]
    public virtual Test Test { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Assignments")]
    public virtual User? User { get; set; }
}
