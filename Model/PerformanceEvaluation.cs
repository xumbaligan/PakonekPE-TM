using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
    // Scoring is star-based: the manager awards 0.5-5 stars per criterion and
    // the system converts that into weighted points (see EvaluationScoring), so
    // the Overall Score still lands out of 100.
    //
    // The appraisal decision lives directly on the evaluation instead of being a
    // separate module: one Evaluation Date (also the appraisal date), one Status
    // (also the appraisal status), and one Feedback field.
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

        // The calendar month/year this evaluation covers, picked from a
        // dropdown rather than typed as free text (this used to be a plain
        // "August 2026" string) - so ticket/task filtering, duplicate
        // prevention, and historical reports can all key off a real,
        // structured date range instead of matching against typed strings.
        [Range(1, 12, ErrorMessage = "Please select a period month.")]
        public int EvaluationPeriodMonth { get; set; } = DateTime.Now.Month;

        [Range(2000, 2100, ErrorMessage = "Please select a period year.")]
        public int EvaluationPeriodYear { get; set; } = DateTime.Now.Year;

        // Kept under the old name/shape ("August 2026") so every page that only
        // ever displayed this string didn't need to change - only Create/Edit,
        // which actually write Month/Year, know about them directly.
        [NotMapped]
        public string EvaluationPeriod =>
            new DateTime(EvaluationPeriodYear, EvaluationPeriodMonth, 1).ToString("MMMM yyyy");

        [NotMapped]
        public DateTime EvaluationPeriodStart => new DateTime(EvaluationPeriodYear, EvaluationPeriodMonth, 1);

        [NotMapped]
        public DateTime EvaluationPeriodEnd => EvaluationPeriodStart.AddMonths(1).AddDays(-1);

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

        // The manager's written feedback for this evaluation period.
        [StringLength(1000)]
        public string? GeneralFeedback { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public EvaluationStatus EvaluationStatus { get; set; } = EvaluationStatus.Draft;

        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Performance Evaluation -> Evaluation Results -> Criteria
        public ICollection<EvaluationResult> Results { get; set; } = new List<EvaluationResult>();

        // Overall Score expressed back as stars out of 5, for star displays.
        [NotMapped]
        public decimal OverallStars => EvaluationScoring.StarsFor(OverallScore);

        [NotMapped]
        public bool IsFinalized => EvaluationStatus == EvaluationStatus.Finalized;
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

        // What the manager actually clicked: 0-5 stars in half-star steps, so
        // 3.5 is a real, storable rating. decimal(2,1) is exactly wide enough.
        [Range(typeof(decimal), "0", "5", ErrorMessage = "Rating must be between 0 and 5 stars.")]
        [Column(TypeName = "decimal(2,1)")]
        public decimal StarRating { get; set; }

        // Points earned for this criterion, derived from StarRating and the
        // criterion's Weight (weights for a RoleType add up to 100, so the
        // Overall Score ends up out of 100 automatically). Stored rather than
        // recomputed so past evaluations stay stable if a weight changes.
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; }

        [StringLength(500)]
        public string? Feedback { get; set; }
    }

    // Single place that turns stars into points and a weighted Overall Score
    // into a rating label - configured here once instead of hardcoded inside a
    // Razor page.
    public static class EvaluationScoring
    {
        public const int MaxStars = 5;

        // Ratings move in half-star steps.
        public const decimal StarStep = 0.5m;

        // Snaps any incoming value onto the nearest valid half-star in 0..5, so
        // a hand-crafted POST can never store 3.7 stars.
        public static decimal NormalizeStars(decimal stars)
        {
            var clamped = Math.Clamp(stars, 0m, MaxStars);
            return Math.Round(clamped / StarStep, 0, MidpointRounding.AwayFromZero) * StarStep;
        }

        // A criterion worth 25% rated 3.5/5 stars earns 17.50 of its 25 points.
        public static decimal ScoreFor(decimal stars, decimal weight) =>
            Math.Round(weight * NormalizeStars(stars) / MaxStars, 2, MidpointRounding.AwayFromZero);

        // Turns an out-of-100 score back into stars out of 5. Not snapped to
        // half-steps: this is an averaged display value, and the star widget
        // renders partial fills anyway.
        public static decimal StarsFor(decimal scoreOutOf100) =>
            Math.Round(Math.Clamp(scoreOutOf100, 0, 100) / 20m, 2, MidpointRounding.AwayFromZero);

        // How much of star `starIndex` (1-based) should be filled, as a
        // percentage. 3.5 stars gives 100/100/100/50/0 across the five stars.
        // Lives here so the Razor partials don't each need their own copy -
        // a @functions block in a partial gets emitted twice and collides.
        public static decimal StarFillPercent(decimal value, int starIndex)
        {
            var filled = Math.Clamp(value, 0m, MaxStars) - (starIndex - 1);
            if (filled >= 1m) return 100m;
            if (filled <= 0m) return 0m;
            return filled * 100m;
        }

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

        // Job Completion and Timeliness are auto-scored straight from a
        // technician's job ticket record and can be misleading on their own -
        // e.g. Timeliness can still read 100% while the technician currently
        // has an overdue ticket in progress, since that ticket hasn't finished
        // (late or otherwise) yet. Work Quality is the one criterion every role
        // type always rates by hand, so before an evaluation is finalized every
        // Work Quality criterion must carry both a star rating and written
        // feedback - the manager's own judgment call is always on record, never
        // just whatever the automated numbers happened to say.
        public static string? ValidateWorkQualityRequired(
            IEnumerable<Criteria> allowedCriteria,
            IEnumerable<(int CriteriaID, decimal StarRating, string? Feedback)> results)
        {
            var byId = results.ToDictionary(r => r.CriteriaID);

            foreach (var c in allowedCriteria.Where(c => c.MetricType == CriteriaMetricType.WorkQuality))
            {
                byId.TryGetValue(c.CriteriaId, out var r);

                if (NormalizeStars(r.StarRating) <= 0 || string.IsNullOrWhiteSpace(r.Feedback))
                {
                    return $"\"{c.CriteriaName}\" is a Work Quality criterion - please rate it and add feedback before finalizing.";
                }
            }

            return null;
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

    // View-model row used by the shared _CriteriaStarRows partial, so the
    // Create and Edit pages render an identical star-rating table (Create just
    // starts every row at zero stars).
    public class CriteriaStarRow
    {
        public Criteria Criteria { get; set; } = new();
        public decimal StarRating { get; set; }
        public string? Feedback { get; set; }
    }
}
