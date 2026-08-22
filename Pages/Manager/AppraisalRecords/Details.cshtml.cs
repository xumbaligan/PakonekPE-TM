using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.AppraisalRecords
{
    // One employee's complete appraisal record: who they are, how they've been
    // rated across every evaluation, and the feedback that came with it. Purely
    // read-only - this page never writes anything.
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public Employee Employee { get; set; } = default!;

        // Every evaluation for this employee, newest first. The year filter is
        // applied client-side so switching years doesn't cost a round trip.
        public List<Model.PerformanceEvaluation> Evaluations { get; set; } = new();

        public List<Model.Feedback> FeedbackEntries { get; set; } = new();

        public EmployeePerformanceStats Stats { get; set; } = new();

        public int EvaluationsCompleted => Evaluations.Count(e => e.EvaluationStatus == EvaluationStatus.Finalized);

        // Overall Rating = the total of all evaluation scores, averaged out of 100.
        public decimal OverallScore => Evaluations.Any()
            ? Math.Round(Evaluations.Average(e => e.OverallScore), 2, MidpointRounding.AwayFromZero)
            : 0;

        public decimal OverallStars => EvaluationScoring.StarsFor(OverallScore);

        public string OverallRating => Evaluations.Any()
            ? EvaluationScoring.RatingFor(OverallScore)
            : "Not yet evaluated";

        // The most recent evaluation by date, whatever its status.
        public Model.PerformanceEvaluation? LastEvaluation => Evaluations.FirstOrDefault();

        // Years present in the history, newest first, for the filter dropdown.
        public List<int> Years => Evaluations
            .Select(e => e.EvaluationDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null) return NotFound();
            Employee = employee;

            Evaluations = await _context.PerformanceEvaluations
                .Include(e => e.Results).ThenInclude(r => r.Criteria)
                .Where(e => e.EmployeeID == id)
                .OrderByDescending(e => e.EvaluationDate)
                .ThenByDescending(e => e.EvaluationID)
                .ToListAsync();

            FeedbackEntries = await _context.Feedbacks
                .Include(f => f.Evaluation)
                .Where(f => f.EmployeeID == id)
                .OrderByDescending(f => f.DateCreated)
                .Take(10)
                .ToListAsync();

            Stats = await EmployeePerformanceStatsBuilder.BuildForAsync(_context, id);

            return Page();
        }
    }
}
