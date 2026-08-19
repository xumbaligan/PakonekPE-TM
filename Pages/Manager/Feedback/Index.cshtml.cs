using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.Feedback
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public List<Model.Feedback> FeedbackList { get; set; } = new();

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Feedbacks
                .Include(f => f.Employee)
                .Include(f => f.Evaluation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(TypeFilter) &&
                Enum.TryParse<FeedbackType>(TypeFilter, true, out var type))
            {
                query = query.Where(f => f.FeedbackType == type);
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                query = query.Where(f =>
                    (f.Employee != null && f.Employee.FullName.Contains(term)) ||
                    f.Comment.Contains(term) ||
                    f.SubmittedBy.Contains(term));
            }

            FeedbackList = await query
                .OrderByDescending(f => f.DateCreated)
                .ToListAsync();
        }
    }
}