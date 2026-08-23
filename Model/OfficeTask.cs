using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    [Table("tbl_officetask")]
    public class OfficeTask
    {
        [Key]
        public int OfficeTaskID { get; set; }

        [Required]
        [StringLength(20)]
        public string TaskNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TaskName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime DueDate { get; set; } = DateTime.Now;

        // The date every activity got Approved, making the task Completed
        // (see RecalculateTaskAsync) - null until then. Distinct from DueDate
        // above, which is just the deadline.
        public DateTime? DateCompleted { get; set; }

        public string Status { get; set; } = "Pending";

        public decimal Progress { get; set; } = 0;

        public decimal Score { get; set; } = 0;

        // Navigation
        public ICollection<TaskActivity> Activities { get; set; }
            = new List<TaskActivity>();

        public ICollection<TaskAssignment> Assignments { get; set; }
            = new List<TaskAssignment>();
    }
}
