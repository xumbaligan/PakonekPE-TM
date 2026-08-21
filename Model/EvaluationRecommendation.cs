using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    // A single appraisal recommendation the manager attached to a Performance
    // Evaluation using the "Add Recommendation" button. Replaces the old fixed
    // Salary Adjustment / Promotion / Training checkboxes - a manager can now
    // add none, one, or several, each with its own optional details.
    [Table("tbl_evaluationrecommendation")]
    public class EvaluationRecommendation
    {
        [Key]
        public int EvaluationRecommendationID { get; set; }

        [Required]
        public int EvaluationID { get; set; }

        [ForeignKey(nameof(EvaluationID))]
        public PerformanceEvaluation? Evaluation { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public AppraisalRecommendation Recommendation { get; set; } = AppraisalRecommendation.Recognition;

        [StringLength(500)]
        public string? Details { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public static string RecommendationLabel(AppraisalRecommendation r) => r switch
        {
            AppraisalRecommendation.NoAction => "No Action",
            AppraisalRecommendation.Recognition => "Recognition",
            AppraisalRecommendation.TrainingRequired => "Training Required",
            AppraisalRecommendation.PerformanceImprovementPlan => "Performance Improvement Plan",
            AppraisalRecommendation.PromotionRecommended => "Promotion Recommended",
            AppraisalRecommendation.SalaryAdjustmentRecommended => "Salary Adjustment Recommended",
            _ => r.ToString()
        };

        // Bootstrap colour suffix so recommendation badges read consistently.
        public static string RecommendationBadgeClass(AppraisalRecommendation r) => r switch
        {
            AppraisalRecommendation.Recognition => "success",
            AppraisalRecommendation.PromotionRecommended => "primary",
            AppraisalRecommendation.SalaryAdjustmentRecommended => "info",
            AppraisalRecommendation.TrainingRequired => "warning",
            AppraisalRecommendation.PerformanceImprovementPlan => "danger",
            _ => "secondary"
        };
    }
}