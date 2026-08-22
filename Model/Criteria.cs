using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    public enum RoleType
    {
        Admin,
        Manager,
        OfficeStaff,
        FieldTechnician
    }

    // How a criterion's star rating gets filled in on a Performance
    // Evaluation. WorkQuality (the default) is the original behavior - the
    // manager rates it by hand. JobCompletion and Timeliness are only ever
    // offered for Field Technician criteria (see Admin/Manager > Criteria):
    // Manager/PerformanceEvaluation/Create.cshtml auto-fills their stars from
    // that technician's actual job ticket record (completion rate / on-time
    // rate) when the technician is selected, and locks them read-only (see
    // _CriteriaStarRows.cshtml) - only WorkQuality is ever rated by hand.
    public enum CriteriaMetricType
    {
        WorkQuality,
        JobCompletion,
        Timeliness
    }

    [Table("tbl_criteria")]
    public class Criteria
    {
        [Key]
        public int CriteriaId { get; set; }

        [Required, StringLength(150)]
        public string CriteriaName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public RoleType RoleType { get; set; }

        // How much this criterion counts toward the Performance Evaluation's
        // Overall Score, in percentage points (e.g. 25 for "Work Quality — 25%").
        // Also doubles as the max points an evaluator can award for this
        // criterion, so weights for a given RoleType should add up to 100.
        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Weight { get; set; } = 0;

        // Only meaningful (and only editable in the UI) for RoleType ==
        // FieldTechnician - an Office Staff criterion is always WorkQuality.
        [Column(TypeName = "nvarchar(50)")]
        public CriteriaMetricType MetricType { get; set; } = CriteriaMetricType.WorkQuality;

        public bool IsActive { get; set; } = true;
    }
}