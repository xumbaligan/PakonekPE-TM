using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using TM_PE.Model;


namespace TM_PE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Criteria> Criteria => Set<Criteria>();
        // Backwards-compatible DbSet name used in pages (was previously named "Task")
        public DbSet<OfficeTask> OfficeTasks => Set<OfficeTask>();
        public DbSet<TaskActivity> TaskActivities => Set<TaskActivity>();
        public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
        public DbSet<ActivitySubmission> ActivitySubmissions => Set<ActivitySubmission>();
        public DbSet<JobTicket> JobTickets => Set<JobTicket>();
        public DbSet<JobTicketAssignment> JobTicketAssignments => Set<JobTicketAssignment>();
        public DbSet<JobTicketSubmission> JobTicketSubmissions => Set<JobTicketSubmission>();
        public DbSet<JobTicketRescheduleHistory> JobTicketRescheduleHistories => Set<JobTicketRescheduleHistory>();
        public DbSet<JobTicketSubmissionHistory> JobTicketSubmissionHistories => Set<JobTicketSubmissionHistory>();
        public DbSet<FiberPlan> FiberPlans => Set<FiberPlan>();
        public DbSet<PerformanceEvaluation> PerformanceEvaluations => Set<PerformanceEvaluation>();
        public DbSet<EvaluationResult> EvaluationResults => Set<EvaluationResult>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<Appraisal> Appraisals => Set<Appraisal>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            b.Entity<Employee>()
                .Property(e => e.RoleType)
                .HasConversion<string>();

            b.Entity<Criteria>()
                .Property(e => e.RoleType)
                .HasConversion<string>();

            // Managers can't add the same fiber plan name twice.
            b.Entity<FiberPlan>()
                .HasIndex(f => f.PlanName)
                .IsUnique();

            b.Entity<PerformanceEvaluation>()
                .Property(e => e.EvaluationStatus)
                .HasConversion<string>();

            // An evaluation is a historical record: if the employee it was
            // written about is later removed, keep the evaluation instead of
            // deleting it out from under the audit trail.
            b.Entity<PerformanceEvaluation>()
                .HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Deleting a Performance Evaluation removes its own results...
            b.Entity<EvaluationResult>()
                .HasOne(r => r.PerformanceEvaluation)
                .WithMany(e => e.Results)
                .HasForeignKey(r => r.EvaluationID)
                .OnDelete(DeleteBehavior.Cascade);

            // ...but a result must never disappear just because a Criteria
            // record was edited/removed elsewhere - block that instead.
            b.Entity<EvaluationResult>()
                .HasOne(r => r.Criteria)
                .WithMany()
                .HasForeignKey(r => r.CriteriaID)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Feedback>()
                .Property(f => f.FeedbackType)
                .HasConversion<string>();

            // Feedback is its own historical trail: removing the employee it's
            // about is blocked, and if the evaluation it was tied to is later
            // deleted, the feedback stays — it just loses that optional link.
            b.Entity<Feedback>()
                .HasOne(f => f.Employee)
                .WithMany()
                .HasForeignKey(f => f.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Feedback>()
                .HasOne(f => f.Evaluation)
                .WithMany()
                .HasForeignKey(f => f.EvaluationID)
                .OnDelete(DeleteBehavior.SetNull);

            b.Entity<Appraisal>()
                .Property(a => a.Recommendation)
                .HasConversion<string>();

            b.Entity<Appraisal>()
                .Property(a => a.AppraisalStatus)
                .HasConversion<string>();

            // Appraisal always follows an Evaluation + Feedback, so the link to
            // the Evaluation is required and can't be removed out from under it.
            b.Entity<Appraisal>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Appraisal>()
                .HasOne(a => a.Evaluation)
                .WithMany()
                .HasForeignKey(a => a.EvaluationID)
                .OnDelete(DeleteBehavior.Restrict);

            // Let the database set CreatedAt automatically
            b.Entity<Department>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // A submission's optional link to the reschedule entry that archived it
            // must NOT cascade-delete — it already cascades via JobTicketID, and SQL
            // Server disallows multiple cascade paths to the same table.
            b.Entity<JobTicketSubmission>()
                .HasOne(s => s.RescheduleHistory)
                .WithMany(h => h.ArchivedSubmissions)
                .HasForeignKey(s => s.RescheduleHistoryID)
                .OnDelete(DeleteBehavior.Restrict);

            // Same reasoning as RescheduleHistory above: a submission's optional
            // link to the "History of Submission" entry that archived it must NOT
            // cascade-delete, since it already cascades via JobTicketID.
            b.Entity<JobTicketSubmission>()
                .HasOne(s => s.SubmissionHistory)
                .WithMany(h => h.ArchivedSubmissions)
                .HasForeignKey(s => s.SubmissionHistoryID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}