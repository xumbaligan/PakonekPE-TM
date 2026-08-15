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
