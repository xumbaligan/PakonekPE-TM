using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.JobTickets
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Model.JobTicket JobTicket { get; set; } = default!;

        // Required only when the manager changes the service date; captured as a
        // separate field (rather than reusing JobTicket.Remarks, which belongs to
        // the field technician leader) and logged to JobTicketRescheduleHistory.
        [BindProperty]
        public string? RescheduleReason { get; set; }

        // Used by the page to detect whether the submitted date actually changed,
        // both for rendering the hidden comparison field and for re-rendering it
        // correctly if validation fails.
        public DateTime OriginalServiceDate { get; set; }

        public int[] FiberPlanOptions { get; set; } = FiberPlans.Allowed;

        public string[] JobTypeOptions { get; set; } = JobTypes.Allowed;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobTicket = await _context.JobTickets
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                .FirstOrDefaultAsync(m => m.JobTicketID == id);

            if (jobTicket == null)
            {
                return NotFound();
            }

            // Completed (and Closed) tickets are locked — no further edits allowed.
            if (jobTicket.IsLockedFromEditing)
            {
                return RedirectToPage("Details", new { id });
            }

            JobTicket = jobTicket;
            OriginalServiceDate = jobTicket.ServiceDate;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Auto-generated / assignment-related fields are not editable from this page
            ModelState.Remove("JobTicket.TicketNumber");
            ModelState.Remove("JobTicket.Assignments");
            ModelState.Remove("JobTicket.Submissions");
            // FiberPlan/Description are conditionally required depending on JobType,
            // so their built-in validation attributes are skipped in favor of manual checks below.
            ModelState.Remove("JobTicket.FiberPlan");
            ModelState.Remove("JobTicket.Description");
            // Client Full Name / Primary / Secondary Number are only collected for
            // non-Maintenance jobs, so their built-in [Required] attributes are skipped
            // in favor of the manual, JobType-aware checks below.
            ModelState.Remove("JobTicket.ClientFullName");
            ModelState.Remove("JobTicket.PrimaryNumber");
            ModelState.Remove("JobTicket.SecondaryNumber");

            var ticket = await _context.JobTickets.FirstOrDefaultAsync(t => t.JobTicketID == JobTicket.JobTicketID);

            if (ticket == null)
            {
                return NotFound();
            }

            // Completed (and Closed) tickets are locked — no further edits allowed,
            // even if this page was submitted directly.
            if (ticket.IsLockedFromEditing)
            {
                return RedirectToPage("Details", new { id = ticket.JobTicketID });
            }

            OriginalServiceDate = ticket.ServiceDate;

            // Changing the service date reschedules the job — require a reason so
            // the field technician knows why the date moved.
            bool dateChanged = JobTicket.ServiceDate.Date != ticket.ServiceDate.Date;
            if (dateChanged && string.IsNullOrWhiteSpace(RescheduleReason))
            {
                ModelState.AddModelError("RescheduleReason", "Please provide a reason for rescheduling the date.");
            }

            if (!JobTypes.Allowed.Contains(JobTicket.JobType))
                ModelState.AddModelError("JobTicket.JobType", "Please select a valid job type.");

            if (JobTicket.JobType == JobTypes.Installation)
            {
                if (JobTicket.FiberPlan == null || !FiberPlans.Allowed.Contains(JobTicket.FiberPlan.Value))
                    ModelState.AddModelError("JobTicket.FiberPlan", "Please select a valid fiber plan.");

                JobTicket.Description = null;
            }
            else if (JobTicket.JobType == JobTypes.Repair || JobTicket.JobType == JobTypes.Maintenance || JobTicket.JobType == JobTypes.Inspection)
            {
                if (string.IsNullOrWhiteSpace(JobTicket.Description))
                    ModelState.AddModelError("JobTicket.Description", "Please provide a description.");

                JobTicket.FiberPlan = null;
            }

            // Maintenance jobs don't collect client contact info — clear whatever was
            // submitted instead of requiring it. Every other job type still requires
            // Client Full Name and Primary Number.
            if (JobTicket.JobType == JobTypes.Maintenance)
            {
                JobTicket.ClientFullName = string.Empty;
                JobTicket.PrimaryNumber = string.Empty;
                JobTicket.SecondaryNumber = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(JobTicket.ClientFullName))
                    ModelState.AddModelError("JobTicket.ClientFullName", "Client full name is required.");

                if (string.IsNullOrWhiteSpace(JobTicket.PrimaryNumber))
                    ModelState.AddModelError("JobTicket.PrimaryNumber", "Primary number is required.");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(JobTicket.PrimaryNumber, @"^[0-9+\-\s()]{7,20}$"))
                    ModelState.AddModelError("JobTicket.PrimaryNumber", "Enter a valid contact number.");

                if (!string.IsNullOrWhiteSpace(JobTicket.SecondaryNumber) &&
                    !System.Text.RegularExpressions.Regex.IsMatch(JobTicket.SecondaryNumber, @"^[0-9+\-\s()]{7,20}$"))
                    ModelState.AddModelError("JobTicket.SecondaryNumber", "Enter a valid contact number.");
            }

            if (!ModelState.IsValid)
            {
                var reload = await _context.JobTickets
                    .Include(t => t.Assignments).ThenInclude(a => a.Employee)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.JobTicketID == JobTicket.JobTicketID);

                if (reload != null)
                {
                    JobTicket.Assignments = reload.Assignments;
                    JobTicket.TicketNumber = reload.TicketNumber;
                    JobTicket.DateCreated = reload.DateCreated;
                    OriginalServiceDate = reload.ServiceDate;
                }

                return Page();
            }

            // If the manager changed the date, log it and reset the ticket to
            // Rescheduled so the field technician sees a fresh cycle — the old
            // remarks and current photos/files are archived to a history entry
            // instead of being lost.
            if (dateChanged)
            {
                var history = new Model.JobTicketRescheduleHistory
                {
                    JobTicketID = ticket.JobTicketID,
                    OldServiceDate = ticket.ServiceDate,
                    NewServiceDate = JobTicket.ServiceDate,
                    Reason = RescheduleReason!.Trim(),
                    PreviousStatus = ticket.Status,
                    PreviousRemarks = ticket.Remarks,
                    DateChanged = DateTime.Now
                };

                var currentSubmissions = await _context.JobTicketSubmissions
                    .Where(s => s.JobTicketID == ticket.JobTicketID && s.RescheduleHistoryID == null)
                    .ToListAsync();

                foreach (var sub in currentSubmissions)
                {
                    history.ArchivedSubmissions.Add(sub);
                }

                _context.JobTicketRescheduleHistories.Add(history);

                ticket.Status = JobTicketStatuses.Rescheduled;
                ticket.Remarks = null;
            }

            // Assignees (and who leads them) are intentionally locked after creation —
            // only the ticket's own details can change here. Status is never copied
            // from the posted JobTicket object (it isn't collected on this form) —
            // it only changes via the reschedule logic above, or elsewhere (the field
            // technician workflow, or the manager's Close action).
            ticket.JobType = JobTicket.JobType;
            ticket.ClientFullName = JobTicket.ClientFullName;
            ticket.PrimaryNumber = JobTicket.PrimaryNumber;
            ticket.SecondaryNumber = JobTicket.SecondaryNumber;
            ticket.FiberPlan = JobTicket.FiberPlan;
            ticket.Description = JobTicket.Description;
            ticket.ServiceDate = JobTicket.ServiceDate;
            ticket.LocationAddress = JobTicket.LocationAddress;
            ticket.NearestLandmark = JobTicket.NearestLandmark;

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
