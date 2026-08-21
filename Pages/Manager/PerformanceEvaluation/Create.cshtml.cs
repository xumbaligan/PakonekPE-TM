using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.PerformanceEvaluation
{
    // Performance Evaluation and its appraisal recommendations are saved
    // together here. One Evaluation Date doubles as the appraisal date, one
    // Status doubles as the appraisal status, and the manager rates each
    // criterion with stars rather than typing raw points.
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Model.PerformanceEvaluation Evaluation { get; set; } = new()
        {
            EvaluationDate = DateTime.Now,
            EvaluationStatus = EvaluationStatus.Draft
        };

        // Posted using the "Results.Index" hidden-input form of collection
        // binding rather than plain 0..n indices, because only the criteria
        // block for the selected role type is enabled - plain indices would
        // break the moment the posted ones weren't contiguous from zero.
        [BindProperty]
        public List<ResultInput> Results { get; set; } = new();

        // Same technique: recommendation rows are added/removed client-side, so
        // their indices are stable keys instead of positions.
        [BindProperty]
        public List<RecommendationInput> Recommendations { get; set; } = new();

        // Only Field Technicians / Office Staff can be evaluated - Admin and
        // Manager accounts are excluded from the picker.
        public List<Employee> EmployeeList { get; set; } = new();

        // Criteria are pulled straight from the database (Manager > Criteria),
        // never hardcoded here - each RoleType gets its own weighted set.
        public List<TM_PE.Model.Criteria> FieldTechnicianCriteria { get; set; } = new();
        public List<TM_PE.Model.Criteria> OfficeStaffCriteria { get; set; } = new();

        // Completed / on-time / rescheduled / cancelled counts shown at the top
        // of the page, so the manager can see actual performance while rating.
        public Dictionary<int, EmployeePerformanceStats> Stats { get; set; } = new();

        public string CurrentManagerName { get; set; } = "Manager";

        // Merges the criteria list with anything already posted, so a
        // validation failure doesn't wipe out the stars/feedback already
        // entered.
        public List<CriteriaStarRow> RowsFor(List<TM_PE.Model.Criteria> criteria) =>
            criteria.Select(c =>
            {
                var posted = Results.FirstOrDefault(r => r.CriteriaID == c.CriteriaId);
                return new CriteriaStarRow
                {
                    Criteria = c,
                    StarRating = posted?.StarRating ?? 0,
                    Feedback = posted?.Feedback
                };
            }).ToList();

        public class ResultInput
        {
            public int CriteriaID { get; set; }
            public int StarRating { get; set; }
            public string? Feedback { get; set; }
        }

        public class RecommendationInput
        {
            public AppraisalRecommendation Recommendation { get; set; }
            public string? Details { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadReferenceDataAsync();
        }

        public Task<IActionResult> OnPostSaveDraftAsync() => SaveAsync(EvaluationStatus.Draft);

        public Task<IActionResult> OnPostFinalizeAsync() => SaveAsync(EvaluationStatus.Finalized);

        private async Task<IActionResult> SaveAsync(EvaluationStatus status)
        {
            var employee = await _context.Employees.FindAsync(Evaluation.EmployeeID);
            if (employee == null || employee.RoleType is RoleType.Admin or RoleType.Manager)
            {
                ModelState.AddModelError("Evaluation.EmployeeID", "Please select a valid employee.");
                employee = null;
            }

            ModelState.Remove("Evaluation.Employee");
            ModelState.Remove("Evaluation.OverallScore");
            ModelState.Remove("Evaluation.OverallRating");
            ModelState.Remove("Evaluation.EvaluatorName");
            ModelState.Remove("Evaluation.EvaluationStatus");

            if (!ModelState.IsValid || employee == null)
            {
                await LoadReferenceDataAsync();
                return Page();
            }

            // Only accept scores for criteria that actually belong to this
            // employee's role type and are still active - never trust the
            // posted criteria list blindly.
            var allowedCriteria = await _context.Criteria
                .Where(c => c.IsActive && c.RoleType == employee.RoleType)
                .ToListAsync();
            var allowedIds = allowedCriteria.ToDictionary(c => c.CriteriaId);

            var evaluation = new Model.PerformanceEvaluation
            {
                EmployeeID = employee.EmployeeId,
                // The evaluator is always the manager who's currently logged
                // in - never a free-text field a person could misattribute.
                EvaluatorName = GetCurrentManagerName(),
                EvaluationDate = Evaluation.EvaluationDate,
                EvaluationPeriod = Evaluation.EvaluationPeriod.Trim(),
                GeneralFeedback = string.IsNullOrWhiteSpace(Evaluation.GeneralFeedback) ? null : Evaluation.GeneralFeedback.Trim(),
                // Status comes from which button was pressed, not a dropdown.
                EvaluationStatus = status,
                DateCreated = DateTime.Now
            };

            decimal overallScore = 0;
            foreach (var r in Results)
            {
                if (!allowedIds.TryGetValue(r.CriteriaID, out var criteria)) continue;

                // Clamp server-side: stars can only ever be 0-5, and the points
                // they translate into are capped by that criterion's weight.
                var stars = Math.Clamp(r.StarRating, 0, EvaluationScoring.MaxStars);
                var score = EvaluationScoring.ScoreFor(stars, criteria.Weight);
                overallScore += score;

                evaluation.Results.Add(new EvaluationResult
                {
                    CriteriaID = criteria.CriteriaId,
                    StarRating = stars,
                    Score = score,
                    Feedback = string.IsNullOrWhiteSpace(r.Feedback) ? null : r.Feedback.Trim()
                });
            }

            foreach (var rec in Recommendations)
            {
                evaluation.Recommendations.Add(new EvaluationRecommendation
                {
                    Recommendation = rec.Recommendation,
                    Details = string.IsNullOrWhiteSpace(rec.Details) ? null : rec.Details.Trim(),
                    DateCreated = DateTime.Now
                });
            }

            evaluation.OverallScore = overallScore;
            evaluation.OverallRating = EvaluationScoring.RatingFor(overallScore);

            _context.PerformanceEvaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            return RedirectToPage("Details", new { id = evaluation.EvaluationID });
        }

        private string GetCurrentManagerName() =>
            HttpContext.Session.GetString("AuthEmployeeName") ?? "Manager";

        private async Task LoadReferenceDataAsync()
        {
            CurrentManagerName = GetCurrentManagerName();

            EmployeeList = await _context.Employees
                .Where(e => e.IsActive && e.RoleType != RoleType.Admin && e.RoleType != RoleType.Manager)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            FieldTechnicianCriteria = await _context.Criteria
                .Where(c => c.IsActive && c.RoleType == RoleType.FieldTechnician)
                .OrderByDescending(c => c.Weight)
                .ToListAsync();

            OfficeStaffCriteria = await _context.Criteria
                .Where(c => c.IsActive && c.RoleType == RoleType.OfficeStaff)
                .OrderByDescending(c => c.Weight)
                .ToListAsync();

            Stats = await EmployeePerformanceStatsBuilder.BuildAsync(
                _context, EmployeeList.Select(e => e.EmployeeId));
        }
    }
}