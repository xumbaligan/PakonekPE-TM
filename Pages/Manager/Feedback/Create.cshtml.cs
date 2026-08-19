using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.Feedback
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Model.Feedback Item { get; set; } = new() { SubmittedBy = "Manager" };

        public List<Employee> EmployeeList { get; set; } = new();
        public List<Model.PerformanceEvaluation> EvaluationList { get; set; } = new();

        public async Task OnGetAsync(int? employeeId)
        {
            await LoadReferenceDataAsync();
            if (employeeId.HasValue) Item.EmployeeID = employeeId.Value;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var employeeExists = await _context.Employees.AnyAsync(e => e.EmployeeId == Item.EmployeeID);
            if (!employeeExists)
                ModelState.AddModelError("Item.EmployeeID", "Please select a valid employee.");

            if (Item.EvaluationID.HasValue)
            {
                var evaluation = await _context.PerformanceEvaluations.FindAsync(Item.EvaluationID.Value);
                if (evaluation == null || evaluation.EmployeeID != Item.EmployeeID)
                    ModelState.AddModelError("Item.EvaluationID", "That evaluation doesn't belong to the selected employee.");
            }

            ModelState.Remove("Item.Employee");
            ModelState.Remove("Item.Evaluation");

            if (!ModelState.IsValid)
            {
                await LoadReferenceDataAsync();
                return Page();
            }

            Item.SubmittedBy = Item.SubmittedBy.Trim();
            Item.Comment = Item.Comment.Trim();
            Item.DateCreated = DateTime.Now;

            _context.Feedbacks.Add(Item);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
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
        }
    }
}