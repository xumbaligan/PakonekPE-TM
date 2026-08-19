using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.PerformanceEvaluation
{
    // Editing an evaluation only ever touches tbl_performanceevaluation /
    // tbl_evaluationresult — it never reads back into JobTicket or OfficeTask.
    // Once an evaluation is Finalized it's locked; only a Draft can be edited.
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Model.PerformanceEvaluation Evaluation { get; set; } = new();

        [BindProperty]
        public List<CreateModel.ResultInput> Results { get; set; } = new();

        public Employee? Employee { get; set; }
        public List<TM_PE.Model.Criteria> CriteriaList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var evaluation = await _context.PerformanceEvaluations
                .Include(e => e.Employee)
                .Include(e => e.Results)
                .FirstOrDefaultAsync(e => e.EvaluationID == id);

            if (evaluation == null) return NotFound();
            if (evaluation.EvaluationStatus == EvaluationStatus.Finalized)
                return RedirectToPage("Details", new { id });

            Evaluation = evaluation;
            Employee = evaluation.Employee;

            await LoadCriteriaAsync(evaluation);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var evaluation = await _context.PerformanceEvaluations
                .Include(e => e.Employee)
                .Include(e => e.Results)
                .FirstOrDefaultAsync(e => e.EvaluationID == id);

            if (evaluation == null) return NotFound();
            if (evaluation.EvaluationStatus == EvaluationStatus.Finalized)
                return RedirectToPage("Details", new { id });

            Employee = evaluation.Employee;

            ModelState.Remove("Evaluation.Employee");
            ModelState.Remove("Evaluation.EmployeeID");
            ModelState.Remove("Evaluation.OverallScore");
            ModelState.Remove("Evaluation.OverallRating");

            if (!ModelState.IsValid)
            {
                Evaluation.EvaluationID = id;
                await LoadCriteriaAsync(evaluation);
                return Page();
            }

            var allowedCriteria = await _context.Criteria
                .Where(c => c.RoleType == evaluation.Employee!.RoleType &&
                            (c.IsActive || evaluation.Results.Select(r => r.CriteriaID).Contains(c.CriteriaId)))
                .ToListAsync();
            var allowedIds = allowedCriteria.ToDictionary(c => c.CriteriaId);

            evaluation.EvaluatorName = Evaluation.EvaluatorName.Trim();
            evaluation.EvaluationPeriod = Evaluation.EvaluationPeriod.Trim();
            evaluation.EvaluationDate = Evaluation.EvaluationDate;
            evaluation.GeneralRemarks = string.IsNullOrWhiteSpace(Evaluation.GeneralRemarks) ? null : Evaluation.GeneralRemarks.Trim();
            evaluation.EvaluationStatus = Evaluation.EvaluationStatus;

            _context.EvaluationResults.RemoveRange(evaluation.Results);
            evaluation.Results.Clear();

            decimal overallScore = 0;
            foreach (var r in Results)
            {
                if (!allowedIds.TryGetValue(r.CriteriaID, out var criteria)) continue;

                var score = Math.Clamp(r.Score, 0, criteria.Weight);
                overallScore += score;

                evaluation.Results.Add(new EvaluationResult
                {
                    CriteriaID = criteria.CriteriaId,
                    Score = score,
                    Remarks = string.IsNullOrWhiteSpace(r.Remarks) ? null : r.Remarks.Trim()
                });
            }

            evaluation.OverallScore = overallScore;
            evaluation.OverallRating = EvaluationScoring.RatingFor(overallScore);

            await _context.SaveChangesAsync();
            return RedirectToPage("Details", new { id = evaluation.EvaluationID });
        }

        private async Task LoadCriteriaAsync(Model.PerformanceEvaluation evaluation)
        {
            if (evaluation.Employee == null) { CriteriaList = new(); return; }

            var existingIds = evaluation.Results.Select(r => r.CriteriaID).ToHashSet();

            // Active criteria for this role type, plus any criteria this
            // evaluation already scored even if since deactivated — so a past
            // score is never silently dropped from the edit screen.
            CriteriaList = await _context.Criteria
                .Where(c => c.RoleType == evaluation.Employee.RoleType &&
                            (c.IsActive || existingIds.Contains(c.CriteriaId)))
                .OrderByDescending(c => c.Weight)
                .ToListAsync();

            Results = CriteriaList.Select(c =>
            {
                var existing = evaluation.Results.FirstOrDefault(r => r.CriteriaID == c.CriteriaId);
                return new CreateModel.ResultInput
                {
                    CriteriaID = c.CriteriaId,
                    Score = existing?.Score ?? 0,
                    Remarks = existing?.Remarks
                };
            }).ToList();
        }
    }
}
