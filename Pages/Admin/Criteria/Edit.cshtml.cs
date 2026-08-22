
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;
using CriteriaModel = TM_PE.Model.Criteria;

namespace TM_PE.Pages.Admin.Criteria;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public CriteriaModel Item { get; set; } = new();

    // Scoring Source values already claimed by another active Field
    // Technician criterion (excluding this one) - the dropdown disables
    // these so a manager can't pick one CriteriaValidation would reject.
    public List<string> UsedFieldTechnicianMetricTypes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var c = await _db.Criteria.FindAsync(id);
        if (c == null) return NotFound();
        Item = c;
        await LoadUsedMetricTypesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUsedMetricTypesAsync();
            return Page();
        }

        var error = await CriteriaValidation.ValidateFieldTechnicianAsync(_db, Item, excludingId: Item.CriteriaId);
        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            await LoadUsedMetricTypesAsync();
            return Page();
        }

        _db.Attach(Item).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadUsedMetricTypesAsync()
    {
        UsedFieldTechnicianMetricTypes = await _db.Criteria
            .Where(c => c.RoleType == RoleType.FieldTechnician && c.IsActive && c.CriteriaId != Item.CriteriaId)
            .Select(c => c.MetricType.ToString())
            .ToListAsync();
    }
}
