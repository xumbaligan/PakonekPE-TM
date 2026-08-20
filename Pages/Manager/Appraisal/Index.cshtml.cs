//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.EntityFrameworkCore;
//using TM_PE.Data;
//using TM_PE.Model;

//namespace TM_PE.Pages.Manager.Appraisal
//{
//    public class IndexModel : PageModel
//    {
//        private readonly AppDbContext _context;
//        public IndexModel(AppDbContext context) => _context = context;

//        public List<Model.Appraisal> Appraisals { get; set; } = new();

//        // Feedback linked to each appraisal's related evaluation, keyed by
//        // EvaluationID. Loaded in one query for the whole list (instead of a
//        // per-row query) so the Details modal has what it needs without any
//        // extra round trips.
//        public Dictionary<int, List<Model.Feedback>> FeedbackByEvaluation { get; set; } = new();

//        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
//        public string? Search { get; set; }

//        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
//        public string? StatusFilter { get; set; }

//        public async Task OnGetAsync()
//        {
//            var query = _context.Appraisals
//                .Include(a => a.Employee)
//                .Include(a => a.Evaluation)
//                .AsQueryable();

//            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
//                Enum.TryParse<AppraisalStatus>(StatusFilter, true, out var status))
//            {
//                query = query.Where(a => a.AppraisalStatus == status);
//            }

//            if (!string.IsNullOrWhiteSpace(Search))
//            {
//                var term = Search.Trim();
//                query = query.Where(a => a.Employee != null && a.Employee.FullName.Contains(term));
//            }

//            Appraisals = await query
//                .OrderByDescending(a => a.AppraisalDate)
//                .ToListAsync();

//            var evaluationIds = Appraisals.Select(a => a.EvaluationID).Distinct().ToList();

//            FeedbackByEvaluation = (await _context.Feedbacks
//                    .Where(f => f.EvaluationID != null && evaluationIds.Contains(f.EvaluationID.Value))
//                    .OrderByDescending(f => f.DateCreated)
//                    .ToListAsync())
//                .GroupBy(f => f.EvaluationID!.Value)
//                .ToDictionary(g => g.Key, g => g.ToList());
//        }
//    }
//}