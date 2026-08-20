//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.EntityFrameworkCore;
//using TM_PE.Data;

//namespace TM_PE.Pages.Manager.Appraisal
//{
//    public class DetailsModel : PageModel
//    {
//        private readonly AppDbContext _context;
//        public DetailsModel(AppDbContext context) => _context = context;

//        public Model.Appraisal Item { get; set; } = default!;
//        public List<Model.Feedback> RelatedFeedback { get; set; } = new();

//        public async Task<IActionResult> OnGetAsync(int id)
//        {
//            var item = await _context.Appraisals
//                .Include(a => a.Employee)
//                .Include(a => a.Evaluation)
//                .FirstOrDefaultAsync(a => a.AppraisalID == id);

//            if (item == null) return NotFound();

//            Item = item;
//            RelatedFeedback = await _context.Feedbacks
//                .Where(f => f.EvaluationID == item.EvaluationID)
//                .OrderByDescending(f => f.DateCreated)
//                .ToListAsync();

//            return Page();
//        }
//    }
//}