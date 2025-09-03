using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Models;

public partial class Feedback
{
    [Key]
    [Column("FeedbackID")]
    public int FeedbackId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("TestID")]
    public int TestId { get; set; }

    [StringLength(1000)]
    public string Content { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [ForeignKey("TestId")]
    [InverseProperty("Feedbacks")]
    public virtual Test? Test { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Feedbacks")]
    public virtual User? User { get; set; }
}
