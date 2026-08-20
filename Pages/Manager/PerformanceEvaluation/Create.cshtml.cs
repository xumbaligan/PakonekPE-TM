using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.PerformanceEvaluation
{
    // Performance Evaluation and Appraisal are saved together here — there is
    // no separate Appraisal page anymore. One Evaluation Date doubles as the
    // appraisal date, one Status doubles as the appraisal status, and one
    // Remarks field covers both.
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Model.PerformanceEvaluation Evaluation { get; set; } = new()
        {
            EvaluationDate = DateTime.Now,
            EvaluationStatus = EvaluationStatus.Draft
        };

        [BindProperty]
        public List<ResultInput> Results { get; set; } = new();

        // Only Field Technicians / Office Staff can be evaluated — Admin and
        // Manager accounts are excluded from the picker.
        public List<Employee> EmployeeList { get; set; } = new();

        // Criteria are pulled straight from the database (Manager > Criteria),
        // never hardcoded here — each RoleType gets its own weighted set.
        public List<TM_PE.Model.Criteria> FieldTechnicianCriteria { get; set; } = new();
        public List<TM_PE.Model.Criteria> OfficeStaffCriteria { get; set; } = new();

        // Read-only, supporting context only — nothing here is written back to
        // JobTicket/OfficeTask by this page.
        public Dictionary<int, EmployeeSupportInfo> SupportInfo { get; set; } = new();

        public string CurrentManagerName { get; set; } = "Manager";

        public class ResultInput
        {
            public int CriteriaID { get; set; }
            public decimal Score { get; set; }
            public string? Remarks { get; set; }
        }

        public class EmployeeSupportInfo
        {
            public int CompletedJobTickets { get; set; }
            public int ActiveJobTickets { get; set; }
            public int CompletedOfficeTasks { get; set; }
            public int ActiveOfficeTasks { get; set; }
            public decimal AverageTaskScore { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadReferenceDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var employee = await _context.Employees.FindAsync(Evaluation.EmployeeID);
            if (employee == null || employee.RoleType is RoleType.Admin or RoleType.Manager)
            {
                ModelState.AddModelError("Evaluation.EmployeeID", "Please select a valid employee.");
                employee = null;
            }

            ModelState.Remove("Evaluation.Employee");
            ModelState.Remove("Evaluation.OverallScore");
            ModelState.Remove("Evaluation.OverallRating");
            ModelState.Remove("Evaluation.EvaluatorName");

            if (!ModelState.IsValid || employee == null)
            {
                await LoadReferenceDataAsync();
                return Page();
            }

            // Only accept scores for criteria that actually belong to this
            // employee's role type and are still active — never trust the
            // posted CriteriaID list blindly.
            var allowedCriteria = await _context.Criteria
                .Where(c => c.IsActive && c.RoleType == employee.RoleType)
                .ToListAsync();
            var allowedIds = allowedCriteria.ToDictionary(c => c.CriteriaId);

            var evaluation = new Model.PerformanceEvaluation
            {
                EmployeeID = employee.EmployeeId,
                // The evaluator is always the manager who's currently logged
                // in — never a free-text field a person could misattribute.
                EvaluatorName = GetCurrentManagerName(),
                EvaluationDate = Evaluation.EvaluationDate,
                EvaluationPeriod = Evaluation.EvaluationPeriod.Trim(),
                GeneralRemarks = string.IsNullOrWhiteSpace(Evaluation.GeneralRemarks) ? null : Evaluation.GeneralRemarks.Trim(),
                EvaluationStatus = Evaluation.EvaluationStatus,
                Recommendation = Evaluation.Recommendation,
                SalaryAdjustmentRecommendation = Evaluation.SalaryAdjustmentRecommendation,
                PromotionRecommendation = Evaluation.PromotionRecommendation,
                TrainingRecommendation = Evaluation.TrainingRecommendation,
                DateCreated = DateTime.Now
            };

            decimal overallScore = 0;
            foreach (var r in Results)
            {
                if (!allowedIds.TryGetValue(r.CriteriaID, out var criteria)) continue;

                // Clamp server-side: a score can never exceed that criterion's
                // configured weight, regardless of what the client sent.
                var score = Math.Clamp(r.Score, 0, criteria.Weight);
                overallScore += score;

                evaluation.Results.Add(new EvaluationResult
                {
                    CriteriaID = criteria.CriteriaId,
                    Score = score,
                    Remarks = string.IsNullOrWhiteSpace(r.Remarks) ? null : r.Remarks.Trim()
                });
            }

            evaluation.OverallScore = overallScore;
            evaluation.OverallRating = EvaluationScoring.RatingFor(overallScore);

            _context.PerformanceEvaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            return RedirectToPage("Details", new { id = evaluation.EvaluationID });
        }

        private string GetCurrentManagerName() =>
            HttpContext.Session.GetString("AuthEmployeeName") ?? "Manager";

        private async Task LoadReferenceDataAsync()
        {
            CurrentManagerName = GetCurrentManagerName();

            EmployeeList = await _context.Employees
                .Where(e => e.IsActive && e.RoleType != RoleType.Admin && e.RoleType != RoleType.Manager)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            FieldTechnicianCriteria = await _context.Criteria
                .Where(c => c.IsActive && c.RoleType == RoleType.FieldTechnician)
                .OrderByDescending(c => c.Weight)
                .ToListAsync();

            OfficeStaffCriteria = await _context.Criteria
                .Where(c => c.IsActive && c.RoleType == RoleType.OfficeStaff)
                .OrderByDescending(c => c.Weight)
                .ToListAsync();

            await BuildSupportInfoAsync();
        }

        private async Task BuildSupportInfoAsync()
        {
            SupportInfo = new Dictionary<int, EmployeeSupportInfo>();

            var ticketAssignments = await _context.JobTicketAssignments
                .Include(a => a.JobTicket)
                .ToListAsync();

            var taskAssignments = await _context.TaskAssignments
                .Include(a => a.OfficeTask)
                .ToListAsync();

            foreach (var emp in EmployeeList)
            {
                var info = new EmployeeSupportInfo();

                var myTickets = ticketAssignments.Where(a => a.EmployeeID == emp.EmployeeId && a.JobTicket != null).ToList();
                info.CompletedJobTickets = myTickets.Count(a =>
                    a.JobTicket!.Status is JobTicketStatuses.Completed or JobTicketStatuses.Closed);
                info.ActiveJobTickets = myTickets.Count(a =>
                    a.JobTicket!.Status is JobTicketStatuses.Pending or JobTicketStatuses.InProgress or JobTicketStatuses.Overdue);

                var myTasks = taskAssignments.Where(a => a.EmployeeID == emp.EmployeeId && a.OfficeTask != null).ToList();
                info.CompletedOfficeTasks = myTasks.Count(a => a.OfficeTask!.Status == "Completed");
                info.ActiveOfficeTasks = myTasks.Count(a => a.OfficeTask!.Status is "Pending" or "In Progress" or "Overdue");
                info.AverageTaskScore = myTasks.Any() ? Math.Round(myTasks.Average(a => a.OfficeTask!.Score), 1) : 0;

                SupportInfo[emp.EmployeeId] = info;
            }
        }
    }
}