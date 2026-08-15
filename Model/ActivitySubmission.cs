using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    [Table("tbl_activitysubmission")]
    public class ActivitySubmission
    {
        [Key]
        public int SubmissionID { get; set; }

        public int ActivityID { get; set; }

        [ForeignKey(nameof(ActivityID))]
        public TaskActivity? Activity { get; set; }

        public int EmployeeID { get; set; }

        [ForeignKey(nameof(EmployeeID))]
        public Employee? Employee { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending Review";
    }
}
