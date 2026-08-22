using Microsoft.EntityFrameworkCore;
using TM_PE.Data;

namespace TM_PE.Model
{
    // Read-only performance snapshot shown at the top of the Performance
    // Evaluation Create/Edit pages and on the Appraisal Records details page,
    // so the manager can see how an employee has actually been performing
    // before awarding stars. Nothing here is ever written back to JobTicket or
    // OfficeTask - it is purely derived from records that already exist.
    public class EmployeePerformanceStats
    {
        public int CompletedJobsTasks { get; set; }
        public int OnTimeCount { get; set; }
        public int OnTimeEligible { get; set; }

        // Job ticket (Field Technician) specific.
        public int Rescheduled { get; set; }
        public int Cancelled { get; set; }

        // Every job ticket ever assigned to this employee (any status), and
        // how many of those they actually completed - the basis for the
        // automated "Job Completion" evaluation criterion (see
        // Model.Criteria.MetricType). Tracked separately from
        // CompletedJobsTasks/OnTimeEligible above because those also count
        // completed Office Tasks, which would understate a mixed-role total.
        public int JobTicketsAssigned { get; set; }
        public int JobTicketsCompleted { get; set; }

        // Office task (Office Staff) specific.
        public int RejectedActivities { get; set; }
        public int OverdueTasks { get; set; }

        public decimal OnTimeRate => OnTimeEligible == 0
            ? 0
            : Math.Round(OnTimeCount * 100m / OnTimeEligible, 0, MidpointRounding.AwayFromZero);

        public decimal JobCompletionRate => JobTicketsAssigned == 0
            ? 0
            : Math.Round(JobTicketsCompleted * 100m / JobTicketsAssigned, 0, MidpointRounding.AwayFromZero);
    }

    public static class EmployeePerformanceStatsBuilder
    {
        // Builds the stats for every employee in one pass, so a page rendering a
        // whole employee dropdown doesn't fire a query per person.
        public static async Task<Dictionary<int, EmployeePerformanceStats>> BuildAsync(
            AppDbContext context, IEnumerable<int> employeeIds)
        {
            var ids = employeeIds.Distinct().ToList();
            var stats = ids.ToDictionary(id => id, _ => new EmployeePerformanceStats());
            if (ids.Count == 0) return stats;

            // ---------- Job tickets ----------
            var ticketAssignments = await context.JobTicketAssignments
                .Include(a => a.JobTicket)
                .Where(a => ids.Contains(a.EmployeeID) && a.JobTicket != null)
                .ToListAsync();

            var ticketIds = ticketAssignments.Select(a => a.JobTicketID).Distinct().ToList();

            // When a ticket was actually finished: the latest "Completed" entry
            // the field technician leader logged for it.
            var completionDates = await context.JobTicketSubmissionHistories
                .Where(h => ticketIds.Contains(h.JobTicketID) && h.Status == JobTicketStatuses.Completed)
                .GroupBy(h => h.JobTicketID)
                .Select(g => new { JobTicketID = g.Key, FinishedOn = g.Max(h => h.DateChanged) })
                .ToDictionaryAsync(x => x.JobTicketID, x => x.FinishedOn);

            var rescheduleCounts = await context.JobTicketRescheduleHistories
                .Where(h => ticketIds.Contains(h.JobTicketID))
                .GroupBy(h => h.JobTicketID)
                .Select(g => new { JobTicketID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.JobTicketID, x => x.Count);

            // Distinct tickets per employee (an employee only ever has one
            // assignment row per ticket, but de-dupe defensively anyway) -
            // the denominator for JobCompletionRate.
            var assignedTicketIdsByEmployee = new Dictionary<int, HashSet<int>>();

            foreach (var assignment in ticketAssignments)
            {
                var ticket = assignment.JobTicket!;
                var s = stats[assignment.EmployeeID];

                if (!assignedTicketIdsByEmployee.TryGetValue(assignment.EmployeeID, out var assignedIds))
                {
                    assignedIds = new HashSet<int>();
                    assignedTicketIdsByEmployee[assignment.EmployeeID] = assignedIds;
                }
                assignedIds.Add(ticket.JobTicketID);

                if (ticket.Status is JobTicketStatuses.Completed or JobTicketStatuses.Closed)
                {
                    s.CompletedJobsTasks++;
                    s.JobTicketsCompleted++;

                    if (ticket.DateOfCompletion.HasValue)
                    {
                        s.OnTimeEligible++;
                        // No logged completion entry (older tickets) counts as
                        // on time rather than punishing the employee for a gap
                        // in the history trail.
                        var finishedOn = completionDates.TryGetValue(ticket.JobTicketID, out var d)
                            ? d.Date
                            : ticket.DateOfCompletion.Value.Date;
                        if (finishedOn <= ticket.DateOfCompletion.Value.Date) s.OnTimeCount++;
                    }
                }
                else if (ticket.Status == JobTicketStatuses.Cancelled)
                {
                    s.Cancelled++;
                }

                if (ticket.Status is JobTicketStatuses.Rescheduled or JobTicketStatuses.RescheduleRequest)
                {
                    s.Rescheduled++;
                }
                else if (rescheduleCounts.TryGetValue(ticket.JobTicketID, out var moved))
                {
                    s.Rescheduled += moved;
                }
            }

            foreach (var (employeeId, assignedIds) in assignedTicketIdsByEmployee)
            {
                stats[employeeId].JobTicketsAssigned = assignedIds.Count;
            }

            // ---------- Office tasks ----------
            var taskAssignments = await context.TaskAssignments
                .Include(a => a.OfficeTask)
                .Where(a => ids.Contains(a.EmployeeID) && a.OfficeTask != null)
                .ToListAsync();

            var taskIds = taskAssignments.Select(a => a.OfficeTaskID).Distinct().ToList();

            // Rejected activities are counted against whoever actually claimed
            // them (AssignedEmployeeID), not every employee assigned to the
            // parent task, since only the claiming employee's submission got rejected.
            var rejectedCounts = await context.TaskActivities
                .Where(a => a.AssignedEmployeeID.HasValue
                    && ids.Contains(a.AssignedEmployeeID.Value)
                    && a.Status == "Rejected")
                .GroupBy(a => a.AssignedEmployeeID!.Value)
                .Select(g => new { EmployeeID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EmployeeID, x => x.Count);

            foreach (var (employeeId, count) in rejectedCounts)
            {
                stats[employeeId].RejectedActivities = count;
            }

            // When an office task was actually finished: the latest file the
            // assigned staff submitted against any of its activities.
            var taskFinishDates = await context.ActivitySubmissions
                .Where(s => s.Activity != null && taskIds.Contains(s.Activity.OfficeTaskID))
                .GroupBy(s => s.Activity!.OfficeTaskID)
                .Select(g => new { OfficeTaskID = g.Key, FinishedOn = g.Max(s => s.DateSubmitted) })
                .ToDictionaryAsync(x => x.OfficeTaskID, x => x.FinishedOn);

            foreach (var assignment in taskAssignments)
            {
                var task = assignment.OfficeTask!;
                var s = stats[assignment.EmployeeID];

                if (string.Equals(task.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    s.CompletedJobsTasks++;
                    s.OnTimeEligible++;

                    var finishedOn = taskFinishDates.TryGetValue(task.OfficeTaskID, out var d)
                        ? d.Date
                        : task.DueDate.Date;
                    if (finishedOn <= task.DueDate.Date) s.OnTimeCount++;
                }
                else if (string.Equals(task.Status, "Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    s.OverdueTasks++;
                }
            }

            return stats;
        }

        public static async Task<EmployeePerformanceStats> BuildForAsync(AppDbContext context, int employeeId)
        {
            var all = await BuildAsync(context, new[] { employeeId });
            return all.TryGetValue(employeeId, out var s) ? s : new EmployeePerformanceStats();
        }
    }
}