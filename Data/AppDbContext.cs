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