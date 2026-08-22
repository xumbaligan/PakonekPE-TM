using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    // Validation rules for a recommendation's text, kept next to the entity so
    // the page model and the client-side check in the modal stay in step.
    public static class RecommendationRules
    {
        // Letters, numbers, spaces, and a small set of punctuation that reads
        // naturally in a recommendation (e.g. "Salary Adjustment - Tier 2").
        public const string AllowedPattern = @"^[A-Za-z0-9À-ÖØ-öø-ÿ.,\-\/#&()\s]+$";
        public const int MaxLength = 100;
    }

    // A manager-maintained appraisal recommendation, offered in the
    // "Recommendation" dropdown on the Performance Evaluation Create/Edit
    // pages. Managers add or remove entries at runtime through the
    // "Recommendations" modal, exactly like the Fiber Plans list on Job Ticket
    // Create. PerformanceEvaluation.Recommendation stores the chosen entry's
    // text directly (not a foreign key), so removing one here never affects
    // evaluations that already used it.
    [Table("tbl_recommendation")]
    public class Recommendation
    {
        [Key]
        public int RecommendationID { get; set; }

        [Required(ErrorMessage = "Please enter a recommendation.")]
        [StringLength(RecommendationRules.MaxLength, ErrorMessage = "Recommendation is too long (max 100 characters).")]
        [RegularExpression(RecommendationRules.AllowedPattern,
            ErrorMessage = "Only letters, numbers, spaces, and . , - / # & ( ) are allowed.")]
        public string RecommendationName { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
