using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TM_PE.Data;
using CriteriaModel = TM_PE.Model.Criteria;

namespace TM_PE.Pages.Admin.Criteria;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public CriteriaModel Item { get; set; } = new() { IsActive = true };

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        _db.Criteria.Add(Item);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
