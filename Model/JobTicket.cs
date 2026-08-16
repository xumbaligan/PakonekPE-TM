using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    // Fiber subscription plans a job ticket can be created for (monthly price, in PHP).
    public static class FiberPlans
    {
        public static readonly int[] Allowed = { 999, 1199, 1499, 1999, 2499, 2999 };
    }

    // The kind of job a ticket covers. Drives which fields appear on the form.
    public static class JobTypes
    {
        public const string Installation = "Installation";
        public const string Repair = "Repair";
        public const string Maintenance = "Maintenance";
        public const string Inspection = "Inspection";

        public static readonly string[] Allowed = { Installation, Repair, Maintenance, Inspection };
    }

    // Job ticket lifecycle. "Closed" is a manager-only, terminal state — once a
    // ticket is Closed it is locked from further edits. "Reschedule Request" can
    // be set either by the manager directly (immediate reschedule) or requested
    // by the field technician leader (awaiting manager approval).
    public static class JobTicketStatuses
    {
        public const string Pending = "Pending";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Closed = "Closed";
        public const string RescheduleRequest = "Reschedule Request";
        public const string Rescheduled = "Rescheduled";

        public static readonly string[] Allowed = { Pending, InProgress, Completed, Cancelled, Closed, RescheduleRequest };
    }

    [Table("tbl_jobticket")]
    public class JobTicket
    {
        [Key]
        public int JobTicketID { get; set; }

        [Required]
        [StringLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; } = DateTime.Now;

        // One of JobTypes.Allowed (Installation / Repair / Maintenance)
        [Required(ErrorMessage = "Job type is required.")]
        [StringLength(20)]
        public string JobType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client full name is required.")]
        [StringLength(100)]
        public string ClientFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Primary number is required.")]
        [StringLength(20)]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid contact number.")]
        public string PrimaryNumber { get; set; } = string.Empty;

        [StringLength(20)]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid contact number.")]
        public string? SecondaryNumber { get; set; }

        // Only used when JobType == Installation. One of FiberPlans.Allowed.
        public int? FiberPlan { get; set; }

        // Only used when JobType == Repair or Maintenance.
        [StringLength(500)]
        public string? Description { get; set; }

        // Meaning depends on JobType: install date, repair date, or maintenance date.
        [Required(ErrorMessage = "Date is required.")]
        public DateTime ServiceDate { get; set; } = DateTime.Now;

        // Free-text address entered directly by the manager.
        [Required(ErrorMessage = "Location address is required.")]
        [StringLength(300)]
        public string LocationAddress { get; set; } = string.Empty;

        // Free-text nearby landmark to help the field technician find the site.
        [StringLength(300)]
        public string? NearestLandmark { get; set; }

        // Always starts as "Pending" on creation; never exposed on the Create form.
        // Valid values: Pending, In Progress, Completed, Cancelled, Closed
        public string Status { get; set; } = JobTicketStatuses.Pending;

        // Free-text notes from the field technician leader about the current status.
        // Set alongside Status via the leader's consolidated Save action.
        [StringLength(500)]
        public string? Remarks { get; set; }

        [NotMapped]
        public bool IsClosed => Status == JobTicketStatuses.Closed;

        // Once the field technician leader marks the job Completed (or the manager
        // later Closes it), the ticket is locked from further edits/status changes
        // by anyone — field technician or manager.
        [NotMapped]
        public bool IsLockedFromEditing =>
            Status == JobTicketStatuses.Completed || Status == JobTicketStatuses.Closed;

        // Field technicians additionally cannot upload files or change
        // Status/Remarks while a Reschedule Request they submitted is still
        // awaiting the manager's decision — the manager must approve/act on it
        // (directly reschedule) before the leader can resume updating the ticket.
        // The manager themselves is NOT subject to this extra lock, since they're
        // the one who needs to act on the request.
        [NotMapped]
        public bool IsLockedFromFieldTechnicianEditing =>
            IsLockedFromEditing || Status == JobTicketStatuses.RescheduleRequest;

        // Label shown above the ServiceDate field/value, based on JobType.
        [NotMapped]
        public string ServiceDateLabel => JobType switch
        {
            JobTypes.Repair => "Date of Repair",
            JobTypes.Maintenance => "Date of Maintenance",
            _ => "Date of Installation"
        };

        // Navigation
        public ICollection<JobTicketAssignment> Assignments { get; set; }
            = new List<JobTicketAssignment>();

        public ICollection<JobTicketSubmission> Submissions { get; set; }
            = new List<JobTicketSubmission>();

        // History of date changes made by the manager. Each entry snapshots what
        // the ticket looked like (status, remarks) right before that reschedule,
        // and the submissions that were archived at that point.
        public ICollection<JobTicketRescheduleHistory> RescheduleHistory { get; set; }
            = new List<JobTicketRescheduleHistory>();

        // History of Submission — every status/remarks update the field
        // technician leader has saved for this ticket, in order, along with
        // whatever photos/files were attached at the time of each save.
        public ICollection<JobTicketSubmissionHistory> SubmissionHistory { get; set; }
            = new List<JobTicketSubmissionHistory>();
    }
}
