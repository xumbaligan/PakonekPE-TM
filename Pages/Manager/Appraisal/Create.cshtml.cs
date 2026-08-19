using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.Appraisal
{
    // Appraisal is the management decision recorded after a Performance
    // Evaluation (and its Feedback) already exist: Evaluation + Feedback ->
    // Appraisal. Creating one here never edits the underlying Evaluation.
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Model.Appraisal Item { get; set; } = new()
        {
            AppraisalDate = DateTime.Now,
            AppraisalStatus = AppraisalStatus.Draft
        };

        public List<Employee> EmployeeList { get; set; } = new();
        public List<Model.PerformanceEvaluation> EvaluationList { get; set; } = new();

        // Read-only supporting context for the selected evaluation — the
        // per-criteria scores plus any Feedback already on file — shown so the
        // manager doesn't have to leave this page to look them up.
        public List<Model.Feedback> RelatedFeedback { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadReferenceDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var evaluation = await _context.PerformanceEvaluations
                .Include(e => e.Employee)
                .FirstOrDefaultAsync(e => e.EvaluationID == Item.EvaluationID);

            if (evaluation == null)
            {
                ModelState.AddModelError("Item.EvaluationID", "Please select a valid Performance Evaluation.");
            }
            else if (evaluation.EmployeeID != Item.EmployeeID)
            {
                ModelState.AddModelError("Item.EvaluationID", "That evaluation doesn't belong to the selected employee.");
            }

            ModelState.Remove("Item.Employee");
            ModelState.Remove("Item.Evaluation");
            ModelState.Remove("Item.OverallRating");

            if (!ModelState.IsValid || evaluation == null)
            {
                await LoadReferenceDataAsync();
                return Page();
            }

            Item.OverallRating = evaluation.OverallRating;
            Item.ManagerRemarks = string.IsNullOrWhiteSpace(Item.ManagerRemarks) ? null : Item.ManagerRemarks.Trim();
            Item.DateCreated = DateTime.Now;

            _context.Appraisals.Add(Item);
            await _context.SaveChangesAsync();

            return RedirectToPage("Details", new { id = Item.AppraisalID });
        }

        private async Task LoadReferenceDataAsync()
        {
            EmployeeList = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            EvaluationList = await _context.PerformanceEvaluations
                .OrderByDescending(e => e.EvaluationDate)
                .ToListAsync();

            if (Item.EvaluationID > 0)
            {
                RelatedFeedback = await _context.Feedbacks
                    .Where(f => f.EvaluationID == Item.EvaluationID)
                    .OrderByDescending(f => f.DateCreated)
                    .ToListAsync();
            }
        }
    }
}