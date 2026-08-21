using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.AppraisalRecords
{
    // Feedback & Appraisal Records is employee-centric rather than
    // evaluation-centric: one row per employee, summarising every Performance
    // Evaluation ever written about them. Nothing here writes to the database -
    // it's a read-only roll-up of records that already exist.
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public List<EmployeeAppraisalRow> Rows { get; set; } = new();

        public class EmployeeAppraisalRow
        {
            public int EmployeeId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public RoleType RoleType { get; set; }
            public string DepartmentName { get; set; } = "-";
            public int EvaluationCount { get; set; }

            // Average of every evaluation's Overall Score, out of 100.
            public decimal OverallScore { get; set; }

            // The same figure expressed as stars out of 5.
            public decimal OverallStars => EvaluationScoring.StarsFor(OverallScore);

            public string OverallRating => EvaluationCount == 0
                ? "Not yet evaluated"
                : EvaluationScoring.RatingFor(OverallScore);
        }

        public async Task OnGetAsync()
        {
            // Only Field Technicians and Office Staff are ever evaluated, so
            // Admin/Manager accounts don't belong in this list.
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.RoleType != RoleType.Admin && e.RoleType != RoleType.Manager)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            var employeeIds = employees.Select(e => e.EmployeeId).ToList();

            // Overall Rating is the total of all of this employee's evaluation
            // scores averaged out, so one strong month doesn't hide a weak one.
            var summaries = await _context.PerformanceEvaluations
                .Where(e => employeeIds.Contains(e.EmployeeID))
                .GroupBy(e => e.EmployeeID)
                .Select(g => new
                {
                    EmployeeID = g.Key,
                    Count = g.Count(),
                    Average = g.Average(x => x.OverallScore)
                })
                .ToDictionaryAsync(x => x.EmployeeID, x => x);

            Rows = employees.Select(e =>
            {
                summaries.TryGetValue(e.EmployeeId, out var s);
                return new EmployeeAppraisalRow
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    RoleType = e.RoleType,
                    DepartmentName = e.Department?.DepartmentName ?? "-",
                    EvaluationCount = s?.Count ?? 0,
                    OverallScore = s == null ? 0 : Math.Round(s.Average, 2, MidpointRounding.AwayFromZero)
                };
            }).ToList();
        }
    }
}