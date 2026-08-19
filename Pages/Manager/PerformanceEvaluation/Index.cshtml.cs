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
                    e.EvaluationPeriod.Contains(term) ||
                    e.EvaluatorName.Contains(term));
            }

            Evaluations = await query
                .OrderByDescending(e => e.EvaluationDate)
                .ThenByDescending(e => e.EvaluationID)
                .ToListAsync();
        }
    }
}
