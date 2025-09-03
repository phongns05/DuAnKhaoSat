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

    [Required(ErrorMessage = "Tiêu đề bài test là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Thời gian làm bài là bắt buộc")]
    [Range(1, 1000, ErrorMessage = "Thời gian làm bài phải từ 1 đến 1000 phút")]
    public int Duration { get; set; }

    [Required(ErrorMessage = "Điểm đạt là bắt buộc")]
    [Range(0, 100, ErrorMessage = "Điểm đạt phải từ 0 đến 100")]
    [Column(TypeName = "decimal(5, 2)")]
    public decimal PassScore { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [InverseProperty("Test")]
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Tests")]
    public virtual User? CreatedByNavigation { get; set; }

    [InverseProperty("Test")]
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [InverseProperty("Test")]
    public virtual ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();

    [InverseProperty("Test")]
    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();
}
