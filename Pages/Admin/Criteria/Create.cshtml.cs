using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;
using CriteriaModel = TM_PE.Model.Criteria;

namespace TM_PE.Pages.Admin.Criteria;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public CriteriaModel Item { get; set; } = new() { IsActive = true };

    // Scoring Source values already claimed by an active Field Technician
    // criterion - the Scoring Source dropdown disables these so a manager
    // can't pick one that CriteriaValidation would just reject on Save.
    public List<string> UsedFieldTechnicianMetricTypes { get; set; } = new();

    public async Task OnGetAsync()
    {
        UsedFieldTechnicianMetricTypes = await _db.Criteria
            .Where(c => c.RoleType == RoleType.FieldTechnician && c.IsActive)
            .Select(c => c.MetricType.ToString())
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var error = await CriteriaValidation.ValidateFieldTechnicianAsync(_db, Item, excludingId: null);
        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            await OnGetAsync();
            return Page();
        }

        _db.Criteria.Add(Item);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
