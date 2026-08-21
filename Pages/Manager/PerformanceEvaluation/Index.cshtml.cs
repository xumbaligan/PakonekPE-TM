using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.PerformanceEvaluation
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public List<Model.PerformanceEvaluation> Evaluations { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.PerformanceEvaluations
                .Include(e => e.Employee)
                .Include(e => e.Results).ThenInclude(r => r.Criteria)
                .Include(e => e.Recommendations)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                Enum.TryParse<EvaluationStatus>(StatusFilter, true, out var status))
            {
                query = query.Where(e => e.EvaluationStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                query = query.Where(e =>
                    (e.Employee != null && e.Employee.FullName.Contains(term)) ||
                    e.EvaluatorName.Contains(term));
            }

            Evaluations = await query
                .OrderByDescending(e => e.EvaluationDate)
                .ThenByDescending(e => e.EvaluationID)
                .ToListAsync();
        }

        // Small DTOs for the View modal - only what the modal needs, serialized
        // straight into the button's data-* attributes.
        public class ResultView
        {
            public string CriteriaName { get; set; } = string.Empty;
            public decimal Weight { get; set; }
            public int Stars { get; set; }
            public decimal Score { get; set; }
            public string? Feedback { get; set; }
        }

        public class RecommendationView
        {
            public string Label { get; set; } = string.Empty;
            public string Badge { get; set; } = "secondary";
            public string? Details { get; set; }
        }

        // Shared by the Index modal and the Appraisal Records details modal so
        // both render an evaluation exactly the same way.
        public static List<ResultView> BuildResultViews(Model.PerformanceEvaluation e) =>
            e.Results
                .OrderByDescending(r => r.Criteria?.Weight ?? 0)
                .Select(r => new ResultView
                {
                    CriteriaName = r.Criteria?.CriteriaName ?? "-",
                    Weight = r.Criteria?.Weight ?? 0,
                    Stars = r.StarRating,
                    Score = r.Score,
                    Feedback = r.Feedback
                })
                .ToList();

        public static List<RecommendationView> BuildRecommendationViews(Model.PerformanceEvaluation e) =>
            e.Recommendations
                .OrderBy(r => r.EvaluationRecommendationID)
                .Select(r => new RecommendationView
                {
                    Label = EvaluationRecommendation.RecommendationLabel(r.Recommendation),
                    Badge = EvaluationRecommendation.RecommendationBadgeClass(r.Recommendation),
                    Details = r.Details
                })
                .ToList();
    }
}