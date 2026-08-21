using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    public enum EvaluationStatus
    {
        Draft,
        Finalized
    }

    // A Performance Evaluation is its own historical record. Creating, editing,
    // or finalizing an evaluation never writes back to JobTicket or OfficeTask.
    //
    // Scoring is star-based: the manager awards 1-5 stars per criterion and the
    // system converts that into weighted points (see EvaluationScoring), so the
    // Overall Score still lands out of 100.
    //
    // The appraisal decision lives directly on the evaluation instead of being a
    // separate module: one Evaluation Date (also the appraisal date), one Status
    // (also the appraisal status), one Feedback field, and a list of
    // manager-added Recommendations.
    [Table("tbl_performanceevaluation")]
    public class PerformanceEvaluation
    {
        [Key]
        public int EvaluationID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [ForeignKey(nameof(EmployeeID))]
        public Employee? Employee { get; set; }

        // Never entered by hand - set automatically to the logged-in manager
        // at the time the evaluation is created (see Create.cshtml.cs).
        [Required]
        [StringLength(100)]
        public string EvaluatorName { get; set; } = "Manager";

        // Free text on purpose (e.g. "August 2026") to match how the business
        // actually names an evaluation period - not every period lines up with
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

        // Renamed from GeneralRemarks - the manager's written feedback for this
        // evaluation period.
        [StringLength(1000)]
        public string? GeneralFeedback { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public EvaluationStatus EvaluationStatus { get; set; } = EvaluationStatus.Draft;

        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Performance Evaluation -> Evaluation Results -> Criteria
        public ICollection<EvaluationResult> Results { get; set; } = new List<EvaluationResult>();

        // Manager-added appraisal recommendations. Replaces the old fixed
        // Salary Adjustment / Promotion / Training checkboxes: the manager now
        // adds as many (or as few) recommendations as the situation calls for.
        public ICollection<EvaluationRecommendation> Recommendations { get; set; } = new List<EvaluationRecommendation>();

        // Overall Score expressed back as stars out of 5, for star displays.
        [NotMapped]
        public decimal OverallStars => EvaluationScoring.StarsFor(OverallScore);

        [NotMapped]
        public bool IsFinalized => EvaluationStatus == EvaluationStatus.Finalized;

        // Short one-line summary used in tables and modals.
        public string RecommendationSummary()
        {
            if (Recommendations == null || Recommendations.Count == 0) return "No recommendation";
            return string.Join(", ", Recommendations
                .OrderBy(r => r.EvaluationRecommendationID)
                .Select(r => EvaluationRecommendation.RecommendationLabel(r.Recommendation)));
        }
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

        // What the manager actually clicked: 0-5 stars for this criterion.
        [Range(0, EvaluationScoring.MaxStars)]
        public int StarRating { get; set; }

        // Points earned for this criterion, derived from StarRating and the
        // criterion's Weight (weights for a RoleType add up to 100, so the
        // Overall Score ends up out of 100 automatically). Stored rather than
        // recomputed so past evaluations stay stable if a weight changes.
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; }

        // Renamed from Remarks.
        [StringLength(500)]
        public string? Feedback { get; set; }
    }

    // Single place that turns stars into points and a weighted Overall Score
    // into a rating label - configured here once instead of hardcoded inside a
    // Razor page.
    public static class EvaluationScoring
    {
        public const int MaxStars = 5;

        // A criterion worth 25% rated 4/5 stars earns 20 of its 25 points.
        public static decimal ScoreFor(int stars, decimal weight)
        {
            var clamped = Math.Clamp(stars, 0, MaxStars);
            return Math.Round(weight * clamped / MaxStars, 2, MidpointRounding.AwayFromZero);
        }

        // Turns an out-of-100 score back into stars out of 5.
        public static decimal StarsFor(decimal scoreOutOf100) =>
            Math.Round(Math.Clamp(scoreOutOf100, 0, 100) / 20m, 2, MidpointRounding.AwayFromZero);

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

        // Bootstrap colour suffix used for rating badges across the pages.
        public static string RatingBadgeClass(string rating) => rating switch
        {
            "Excellent" => "success",
            "Very Good" => "primary",
            "Good" => "info",
            "Needs Improvement" => "warning",
            "Poor" => "danger",
            _ => "secondary"
        };
    }
}

namespace TM_PE.Model
{
    // View-model row used by the shared _CriteriaStarRows partial, so the
    // Create and Edit pages render an identical star-rating table (Create just
    // starts every row at zero stars).
    public class CriteriaStarRow
    {
        public Criteria Criteria { get; set; } = new();
        public int StarRating { get; set; }
        public string? Feedback { get; set; }
    }
}