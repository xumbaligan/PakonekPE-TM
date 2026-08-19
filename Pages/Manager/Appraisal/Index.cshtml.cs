using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.Appraisal
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public List<Model.Appraisal> Appraisals { get; set; } = new();

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Appraisals
                .Include(a => a.Employee)
                .Include(a => a.Evaluation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                Enum.TryParse<AppraisalStatus>(StatusFilter, true, out var status))
            {
                query = query.Where(a => a.AppraisalStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                query = query.Where(a => a.Employee != null && a.Employee.FullName.Contains(term));
            }

            Appraisals = await query
                .OrderByDescending(a => a.AppraisalDate)
                .ToListAsync();
        }
    }
}