using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Model;
using TM_PE.Data;

namespace TM_PE.Pages.Manager.OfficeTask
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Model.OfficeTask> OfficeTask { get; set; } = default!;

        // Filters are applied client-side (see Index.cshtml script), these just
        // keep the form fields populated when the page reloads.
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(Name = "status", SupportsGet = true)] public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            OfficeTask = await _context.OfficeTasks
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                .Include(t => t.Activities)
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();

            await RefreshOverdueStatusesAsync();
        }

        // A task becomes Overdue purely because time has passed, not because someone edited
        // it, so re-check on every page load and persist the change if the status flipped.
        private async Task RefreshOverdueStatusesAsync()
        {
            var today = DateTime.Now.Date;
            bool changed = false;

            foreach (var task in OfficeTask)
            {
                if (task.Status != "Completed" && task.DueDate.Date < today)
                {
                    if (task.Status != "Overdue")
                    {
                        task.Status = "Overdue";
                        changed = true;
                    }
                }
                else if (task.Status == "Overdue" && task.DueDate.Date >= today)
                {
                    // Due date was pushed back (e.g. via Edit) so it's no longer overdue;
                    // fall back to Pending and let the next recalculation refine it further.
                    task.Status = "Pending";
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var task = await _context.OfficeTasks.FindAsync(id);

            if (task != null)
            {
                _context.OfficeTasks.Remove(task);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
