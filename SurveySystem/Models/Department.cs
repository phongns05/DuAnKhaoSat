using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Department
{
    [Key]
    [Column("DeptID")]
    public int DeptId { get; set; }

    [StringLength(100)]
    public string DeptName { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    [InverseProperty("Dept")]
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    [ForeignKey("DeptId")]
    [InverseProperty("Depts")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
