using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;

namespace TM_PE.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public int DepartmentCount { get; set; }
    public int EmployeeCount { get; set; }
    public int CriteriaCount { get; set; }

    // ---- Office Task summary counts (detailed workload views live under
    // Manager/WorkLoadMonitoring now) ----
    public int ActiveOfficeTaskCount { get; set; }
    public int OverdueOfficeTaskCount { get; set; }
    public int CompletedOfficeTaskCount { get; set; }

    public async Task OnGetAsync()
    {
        DepartmentCount = _db.Departments.Count();
        EmployeeCount = _db.Employees.Count();
        CriteriaCount = _db.Criteria.Count(c => c.IsActive);

        var tasks = await _db.OfficeTasks.ToListAsync();

        // Mirrors the overdue check used on the Office Task Index page so the
        // dashboard reflects the same live status, even if no one has opened
        // Office Tasks yet today.
        await RefreshOverdueStatusesAsync(tasks);

        ActiveOfficeTaskCount = tasks.Count(t => t.Status is "Pending" or "In Progress" or "Overdue");
        OverdueOfficeTaskCount = tasks.Count(t => t.Status == "Overdue");
        CompletedOfficeTaskCount = tasks.Count(t => t.Status == "Completed");
    }

    private async Task RefreshOverdueStatusesAsync(List<Model.OfficeTask> tasks)
    {
        var today = DateTime.Now.Date;
        bool changed = false;

        foreach (var task in tasks)
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
                task.Status = "Pending";
                changed = true;
            }
        }

        if (changed)
        {
            await _db.SaveChangesAsync();
        }
    }
}
