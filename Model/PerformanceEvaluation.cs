using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    public enum EvaluationStatus
    {
        Draft,
        Finalized
    }

    // A Performance Evaluation is its own historical record. It is filled out
    // using completed Job Tickets / Office Tasks and the Workload Monitoring
    // statistics as *supporting information* only — creating, editing, or
    // finalizing an evaluation never writes back to JobTicket or OfficeTask.
    [Table("tbl_performanceevaluation")]
    public class PerformanceEvaluation
    {
        [Key]
        public int EvaluationID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [ForeignKey(nameof(EmployeeID))]
        public Employee? Employee { get; set; }

        [Required(ErrorMessage = "Please enter who is conducting this evaluation.")]
        [StringLength(100)]
        public string EvaluatorName { get; set; } = "Manager";

        // Free text on purpose (e.g. "August 2026") to match how the business
        // actually names an evaluation period — not every period lines up with
        // a calendar month.
        [Required(ErrorMessage = "Please enter the evaluation period.")]
        [StringLength(50)]
        public string EvaluationPeriod { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime EvaluationDate { get; set; } = DateTime.Now;

        // Sum of all EvaluationResults.Score at the time of saving. Kept as a
        // stored snapshot (not recalculated on the fly) so that changing a
        // Criteria's weight later doesn't silently rewrite past evaluations.
        [Column(TypeName = "decimal(5,2)")]
        public decimal OverallScore { get; set; }

        [StringLength(50)]
        public string OverallRating { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? GeneralRemarks { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public EvaluationStatus EvaluationStatus { get; set; } = EvaluationStatus.Draft;

        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Performance Evaluation -> Evaluation Results -> Criteria
        public ICollection<EvaluationResult> Results { get; set; } = new List<EvaluationResult>();
    }

    // One scored criterion within a Performance Evaluation.
    [Table("tbl_evaluationresult")]
    public class EvaluationResult
    {
        [Key]
        public int EvaluationResultID { get; set; }

        [Required]
        public int EvaluationID { get; set; }

        [ForeignKey(nameof(EvaluationID))]
        public PerformanceEvaluation? PerformanceEvaluation { get; set; }

        [Required]
        public int CriteriaID { get; set; }

        [ForeignKey(nameof(CriteriaID))]
        public Criteria? Criteria { get; set; }

        // Points earned for this criterion. Capped at Criteria.Weight, which
        // doubles as this criterion's max points (weights for a RoleType add
        // up to 100, so the Overall Score ends up out of 100 automatically).
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    // Single place that turns a weighted Overall Score into a rating label —
    // configured here once instead of hardcoded inside a Razor page.
    public static class EvaluationScoring
    {
        // Highest threshold first; the first band the score meets or exceeds wins.
        public static readonly (decimal MinScore, string Rating)[] Bands =
        {
            (90m, "Excellent"),
            (80m, "Very Good"),
            (70m, "Good"),
            (60m, "Needs Improvement"),
            (0m,  "Poor")
        };

        public static string RatingFor(decimal overallScoreOutOf100)
        {
            foreach (var band in Bands)
            {
                if (overallScoreOutOf100 >= band.MinScore) return band.Rating;
            }
            return "Poor";
        }
    }
}
