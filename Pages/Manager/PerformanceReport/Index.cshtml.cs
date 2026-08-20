//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.EntityFrameworkCore;
//using TM_PE.Data;
//using TM_PE.Model;

//namespace TM_PE.Pages.Manager.PerformanceReport
//{
//    // Pulls together data already recorded elsewhere in the system — nothing
//    // here is entered manually. Sources: Employee, Job Tickets/Assignments
//    // (workload), Performance Evaluation + Evaluation Results, Feedback, and
//    // Appraisal.
//    public class IndexModel : PageModel
//    {
//        private readonly AppDbContext _context;
//        public IndexModel(AppDbContext context) => _context = context;

//        public List<Employee> EmployeeList { get; set; } = new();
//        public List<Model.PerformanceEvaluation> EvaluationList { get; set; } = new();

//        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
//        public int? EmployeeId { get; set; }

//        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
//        public int? EvaluationId { get; set; }

//        public ReportData? Report { get; set; }

//        public class WorkloadSummary
//        {
//            public int Assigned { get; set; }
//            public int Completed { get; set; }
//            public int InProgress { get; set; }
//            public int Overdue { get; set; }
//            public decimal CompletionRate => Assigned == 0 ? 0 : Math.Round(Completed * 100m / Assigned, 0);
//        }

//        public class CriterionLine
//        {
//            public string Name { get; set; } = string.Empty;
//            public decimal PercentAchieved { get; set; } // Score / Weight * 100
//        }

//        public class ReportData
//        {
//            public Employee Employee { get; set; } = default!;
//            public Model.PerformanceEvaluation Evaluation { get; set; } = default!;
//            public WorkloadSummary Workload { get; set; } = new();
//            public List<CriterionLine> Criteria { get; set; } = new();
//            public List<string> Strengths { get; set; } = new();
//            public List<string> AreasForImprovement { get; set; } = new();
//            public List<string> GeneralFeedback { get; set; } = new();
//            public TM_PE.Model.Appraisal? Appraisal { get; set; }
//        }

//        public async Task OnGetAsync()
//        {
//            EmployeeList = await _context.Employees
//                .Where(e => e.IsActive)
//                .OrderBy(e => e.FullName)
//                .ToListAsync();

//            EvaluationList = await _context.PerformanceEvaluations
//                .OrderByDescending(e => e.EvaluationDate)
//                .ToListAsync();

//            if (EmployeeId.HasValue && EvaluationId.HasValue)
//            {
//                Report = await BuildReportAsync(EmployeeId.Value, EvaluationId.Value);
//            }
//        }

//        private async Task<ReportData?> BuildReportAsync(int employeeId, int evaluationId)
//        {
//            var employee = await _context.Employees
//                .Include(e => e.Department)
//                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

//            var evaluation = await _context.PerformanceEvaluations
//                .Include(e => e.Results).ThenInclude(r => r.Criteria)
//                .FirstOrDefaultAsync(e => e.EvaluationID == evaluationId && e.EmployeeID == employeeId);

//            if (employee == null || evaluation == null) return null;

//            var data = new ReportData { Employee = employee, Evaluation = evaluation };

//            // ---- Workload summary (Job Tickets for Field Technicians, Office
//            // Tasks for Office Staff) — read straight from existing records. ----
//            if (employee.RoleType == RoleType.FieldTechnician)
//            {
//                var tickets = await _context.JobTicketAssignments
//                    .Where(a => a.EmployeeID == employeeId)
//                    .Include(a => a.JobTicket)
//                    .Select(a => a.JobTicket!)
//                    .Where(t => t != null)
//                    .ToListAsync();

//                data.Workload.Assigned = tickets.Count;
//                data.Workload.Completed = tickets.Count(t => t.Status is JobTicketStatuses.Completed or JobTicketStatuses.Closed);
//                data.Workload.InProgress = tickets.Count(t => t.Status == JobTicketStatuses.InProgress);
//                data.Workload.Overdue = tickets.Count(t => JobTicketOverdueChecker.IsOverdue(t) || t.Status == JobTicketStatuses.Overdue);
//            }
//            else if (employee.RoleType == RoleType.OfficeStaff)
//            {
//                var tasks = await _context.TaskAssignments
//                    .Where(a => a.EmployeeID == employeeId)
//                    .Include(a => a.OfficeTask)
//                    .Select(a => a.OfficeTask!)
//                    .Where(t => t != null)
//                    .ToListAsync();

//                data.Workload.Assigned = tasks.Count;
//                data.Workload.Completed = tasks.Count(t => t.Status == "Completed");
//                data.Workload.InProgress = tasks.Count(t => t.Status == "In Progress");
//                data.Workload.Overdue = tasks.Count(t => t.Status == "Overdue");
//            }

//            // ---- Performance Evaluation / Evaluation Results ----
//            data.Criteria = evaluation.Results
//                .Where(r => r.Criteria != null && r.Criteria.Weight > 0)
//                .OrderByDescending(r => r.Criteria!.Weight)
//                .Select(r => new CriterionLine
//                {
//                    Name = r.Criteria!.CriteriaName,
//                    PercentAchieved = Math.Round(r.Score / r.Criteria.Weight * 100m, 0)
//                })
//                .ToList();

//            // ---- Feedback: tied to this evaluation, or general (untied)
//            // feedback for the employee if nothing was tied specifically. ----
//            var feedback = await _context.Feedbacks
//                .Where(f => f.EmployeeID == employeeId && (f.EvaluationID == evaluationId || f.EvaluationID == null))
//                .OrderByDescending(f => f.DateCreated)
//                .ToListAsync();

//            data.Strengths = feedback
//                .Where(f => f.FeedbackType is FeedbackType.Positive or FeedbackType.Recognition)
//                .Select(f => f.Comment)
//                .ToList();

//            data.AreasForImprovement = feedback
//                .Where(f => f.FeedbackType is FeedbackType.Improvement or FeedbackType.Concern)
//                .Select(f => f.Comment)
//                .ToList();

//            data.GeneralFeedback = feedback
//                .Where(f => f.FeedbackType == FeedbackType.General)
//                .Select(f => f.Comment)
//                .ToList();

//            // ---- Appraisal (management decision, if one has been made yet) ----
//            data.Appraisal = await _context.Appraisals
//                .FirstOrDefaultAsync(a => a.EvaluationID == evaluationId);

//            return data;
//        }
//    }
//}