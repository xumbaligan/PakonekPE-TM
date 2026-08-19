using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;

namespace TM_PE.Pages.Manager.Feedback
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        public DetailsModel(AppDbContext context) => _context = context;

        public Model.Feedback Item { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var item = await _context.Feedbacks
                .Include(f => f.Employee)
                .Include(f => f.Evaluation)
                .FirstOrDefaultAsync(f => f.FeedbackID == id);

            if (item == null) return NotFound();

            Item = item;
            return Page();
        }
    }
}