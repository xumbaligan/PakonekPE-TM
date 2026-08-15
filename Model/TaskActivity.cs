using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    [Table("tbl_taskactivity")]
    public class TaskActivity
    {
        [Key]
        public int ActivityID { get; set; }

        [Required]
        [StringLength(100)]
        public string ActivityName { get; set; } = string.Empty;

        [StringLength(500)]
        public string FeedBack { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public int? AssignedEmployeeID { get; set; }
        [ForeignKey(nameof(AssignedEmployeeID))]
        public Employee? AssignedEmployee { get; set; }

        public int OfficeTaskID { get; set; }

        [ForeignKey(nameof(OfficeTaskID))]
        public OfficeTask? OfficeTask { get; set; }
    }
}
