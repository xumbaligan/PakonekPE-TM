using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    // Admin can view this dashboard too (see Program.cs RBAC middleware), but
    // gets the Admin sidebar (Dashboard + Performance Reports only) instead of
    // the full Manager nav, since Admin isn't meant to manage tickets/tasks/etc.
    public string LayoutName { get; set; } = "_Layout";

    public int DepartmentCount { get; set; }
    public int EmployeeCount { get; set; }
    public int CriteriaCount { get; set; }

    // ---- Office Task summary counts (detailed workload views live under
    // Manager/WorkLoadMonitoring now). Mirrors the same five-tile breakdown
    // as the Job Ticket workload below, so Office Staff and Field Technician
    // workload read the same way at a glance.
    public int ActiveOfficeTaskCount { get; set; }
    public int PendingOfficeTaskCount { get; set; }
    public int InProgressOfficeTaskCount { get; set; }
    public int OverdueOfficeTaskCount { get; set; }
    public int CompletedOfficeTaskCount { get; set; }

    // ---- Job Ticket workload ----
    public int TotalActiveJobs { get; set; }
    public int PendingJobs { get; set; }
    public int InProgressJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int OverdueJobs { get; set; }

    // ---- Job performance ----
    public decimal JobCompletionRate { get; set; }
    public decimal OnTimeCompletionRate { get; set; }
    public int RescheduledJobsCount { get; set; }
    public int CancelledJobsCount { get; set; }

    // ---- Employee performance (based on each employee's most recent
    // Performance Evaluation) ----
    public decimal AveragePerformanceScore { get; set; }
    public decimal AverageOfficeStaffScore { get; set; }
    public decimal AverageFieldTechnicianScore { get; set; }
    public List<(string Name, decimal Score, string Rating)> TopPerformers { get; set; } = new();
    public List<(string Name, decimal Score, string Rating)> NeedsImprovement { get; set; } = new();

    public async Task OnGetAsync()
    {
        LayoutName = HttpContext.Session.GetString("AuthRoleType") == "Admin" ? "_Admin" : "_Layout";

        DepartmentCount = _db.Departments.Count();
        EmployeeCount = _db.Employees.Count();
        CriteriaCount = _db.Criteria.Count(c => c.IsActive);

        var tasks = await _db.OfficeTasks.ToListAsync();

        // Mirrors the overdue check used on the Office Task Index page so the
        // dashboard reflects the same live status, even if no one has opened
        // Office Tasks yet today.
        await RefreshOverdueStatusesAsync(tasks);

        PendingOfficeTaskCount = tasks.Count(t => t.Status == "Pending");
        InProgressOfficeTaskCount = tasks.Count(t => t.Status == "In Progress");
        OverdueOfficeTaskCount = tasks.Count(t => t.Status == "Overdue");
        ActiveOfficeTaskCount = PendingOfficeTaskCount + InProgressOfficeTaskCount + OverdueOfficeTaskCount;
        CompletedOfficeTaskCount = tasks.Count(t => t.Status == "Completed");

        await LoadJobTicketMetricsAsync();
        await LoadEmployeePerformanceMetricsAsync();
    }

    private async Task LoadJobTicketMetricsAsync()
    {
        var tickets = await _db.JobTickets.ToListAsync();

        bool IsOverdue(Model.JobTicket t) => JobTicketOverdueChecker.IsOverdue(t) || t.Status == JobTicketStatuses.Overdue;

        PendingJobs = tickets.Count(t => t.Status == JobTicketStatuses.Pending && !IsOverdue(t));
        InProgressJobs = tickets.Count(t => t.Status == JobTicketStatuses.InProgress && !IsOverdue(t));
        OverdueJobs = tickets.Count(IsOverdue);
        CompletedJobs = tickets.Count(t => t.Status == JobTicketStatuses.Completed);
        TotalActiveJobs = PendingJobs + InProgressJobs + OverdueJobs;

        var eligibleForRate = tickets.Count(t => t.Status != JobTicketStatuses.Cancelled);
        JobCompletionRate = eligibleForRate == 0 ? 0 : Math.Round(CompletedJobs * 100m / eligibleForRate, 0);

        CancelledJobsCount = tickets.Count(t => t.Status == JobTicketStatuses.Cancelled);

        // "On time" = the most recent submission that set a ticket to
        // Completed happened on or before that ticket's due date.
        var completedTicketIds = tickets
            .Where(t => t.Status == JobTicketStatuses.Completed && t.DateOfCompletion.HasValue)
            .Select(t => t.JobTicketID)
            .ToHashSet();

        var completionEvents = await _db.JobTicketSubmissionHistories
            .Where(s => completedTicketIds.Contains(s.JobTicketID) && s.Status == JobTicketStatuses.Completed)
            .ToListAsync();

        var latestCompletionDateByTicket = completionEvents
            .GroupBy(s => s.JobTicketID)
            .ToDictionary(g => g.Key, g => g.Max(s => s.DateChanged));

        var ticketsWithKnownCompletion = tickets
            .Where(t => latestCompletionDateByTicket.ContainsKey(t.JobTicketID))
            .ToList();

        var onTimeCount = ticketsWithKnownCompletion.Count(t =>
            latestCompletionDateByTicket[t.JobTicketID].Date <= t.DateOfCompletion!.Value.Date);

        OnTimeCompletionRate = ticketsWithKnownCompletion.Count == 0
            ? 0
            : Math.Round(onTimeCount * 100m / ticketsWithKnownCompletion.Count, 0);

        // Distinct tickets that have at least one reschedule on record.
        RescheduledJobsCount = await _db.JobTicketRescheduleHistories
            .Select(r => r.JobTicketID)
            .Distinct()
            .CountAsync();
    }

    private async Task LoadEmployeePerformanceMetricsAsync()
    {
        // Only Finalized evaluations count here - same rule Manager >
        // Performance Report uses - so a manager's in-progress Draft never
        // shows up as if it were an official score.
        var finalizedEvaluations = await _db.PerformanceEvaluations
            .Include(e => e.Employee)
            .Where(e => e.EvaluationStatus == EvaluationStatus.Finalized && e.Employee != null)
            .ToListAsync();

        // Average Performance Score must match what the Performance Report
        // shows for All Periods/All Departments: each employee contributes
        // the average of every Finalized evaluation they have (not just
        // their latest one), and every employee counts equally regardless of
        // how many evaluations they've accumulated.
        var perEmployeeAverage = finalizedEvaluations
            .GroupBy(e => e.EmployeeID)
            .Select(g => new
            {
                Employee = g.First().Employee!,
                AverageScore = Math.Round(g.Average(e => e.OverallScore), 2, MidpointRounding.AwayFromZero)
            })
            .ToList();

        if (perEmployeeAverage.Any())
        {
            AveragePerformanceScore = Math.Round(perEmployeeAverage.Average(e => e.AverageScore), 1);

            var officeStaff = perEmployeeAverage.Where(e => e.Employee.RoleType == RoleType.OfficeStaff).ToList();
            var fieldTechs = perEmployeeAverage.Where(e => e.Employee.RoleType == RoleType.FieldTechnician).ToList();

            AverageOfficeStaffScore = officeStaff.Any() ? Math.Round(officeStaff.Average(e => e.AverageScore), 1) : 0;
            AverageFieldTechnicianScore = fieldTechs.Any() ? Math.Round(fieldTechs.Average(e => e.AverageScore), 1) : 0;
        }

        // Top Performers / Needs Improvement still reflect each employee's
        // most recent Finalized evaluation - "how are they doing right now",
        // a current snapshot rather than their all-time average.
        var latestPerEmployee = finalizedEvaluations
            .GroupBy(e => e.EmployeeID)
            .Select(g => g.OrderByDescending(e => e.EvaluationDate).First())
            .ToList();

        TopPerformers = latestPerEmployee
            .OrderByDescending(e => e.OverallScore)
            .Take(3)
            .Select(e => (e.Employee!.FullName, e.OverallScore, e.OverallRating))
            .ToList();

        NeedsImprovement = latestPerEmployee
            .Where(e => e.OverallRating is "Needs Improvement" or "Poor")
            .OrderBy(e => e.OverallScore)
            .Take(5)
            .Select(e => (e.Employee!.FullName, e.OverallScore, e.OverallRating))
            .ToList();
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