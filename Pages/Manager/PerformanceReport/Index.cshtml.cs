using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;

namespace TM_PE.Pages.Manager.PerformanceReport
{
    // Assembles a management report entirely from records that already exist -
    // Employees, Departments, Performance Evaluations, Job Tickets and Office
    // Tasks. Nothing on this page is entered by hand and nothing is written
    // back; it's a read-only roll-up filtered by evaluation period and
    // department.
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        // Filters arrive on the query string so "Generate Report" is a plain GET
        // - the report is bookmarkable and Export CSV can reuse the same values.
        [BindProperty(SupportsGet = true)]
        public string? Period { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DepartmentId { get; set; }

        public List<string> PeriodOptions { get; set; } = new();
        public List<Department> DepartmentOptions { get; set; } = new();

        public List<ReportRow> Rows { get; set; } = new();

        // ---- Summary tiles ----
        public int EmployeeCount => Rows.Count;
        public int AppraisalCount => Rows.Sum(r => r.Appraisals);
        public int CompletedWorkItems => Rows.Sum(r => r.Completed);

        // Averaged over employees who actually have an evaluation in the
        // selected period, so unevaluated staff don't drag the figure to zero.
        public decimal AverageScore
        {
            get
            {
                var scored = Rows.Where(r => r.Appraisals > 0).ToList();
                return scored.Count == 0
                    ? 0
                    : Math.Round(scored.Average(r => r.AverageScore), 2, MidpointRounding.AwayFromZero);
            }
        }

        public decimal AverageStars => EvaluationScoring.StarsFor(AverageScore);

        // ---- Chart data ----
        // Bar chart: average score per employee (only those with evaluations,
        // highest first, so the chart stays readable).
        public List<ChartPoint> ScoreByEmployee => Rows
            .Where(r => r.Appraisals > 0)
            .OrderByDescending(r => r.AverageScore)
            .Select(r => new ChartPoint { Label = r.EmployeeName, Value = r.AverageScore })
            .ToList();

        // Pie chart: how many employees fall into each rating band.
        public List<ChartPoint> RatingDistribution
        {
            get
            {
                var scored = Rows.Where(r => r.Appraisals > 0).ToList();
                return EvaluationScoring.Bands
                    .Select(b => new ChartPoint
                    {
                        Label = b.Rating,
                        Value = scored.Count(r => r.Rating == b.Rating)
                    })
                    .Where(p => p.Value > 0)
                    .ToList();
            }
        }

        // How many of the listed employees actually have an evaluation inside
        // the selected filters - the number that decides whether the charts
        // have anything to draw.
        public int EvaluatedEmployeeCount => Rows.Count(r => r.Appraisals > 0);

        public bool HasData => EvaluatedEmployeeCount > 0;

        public class ChartPoint
        {
            public string Label { get; set; } = string.Empty;
            public decimal Value { get; set; }
        }

        public class ReportRow
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public RoleType RoleType { get; set; }
            public string DepartmentName { get; set; } = "-";
            public int Appraisals { get; set; }
            public int Completed { get; set; }
            public decimal AverageScore { get; set; }

            public decimal Stars => EvaluationScoring.StarsFor(AverageScore);

            public string Rating => Appraisals == 0
                ? "Not yet evaluated"
                : EvaluationScoring.RatingFor(AverageScore);

            public string RoleLabel => RoleType == RoleType.FieldTechnician
                ? "Field Technician"
                : RoleType == RoleType.OfficeStaff ? "Office Staff" : RoleType.ToString();
        }

        public async Task OnGetAsync()
        {
            await BuildReportAsync();
        }

        // Exports exactly what the on-screen table shows, using the same
        // filters, so the file always matches the report the manager is looking
        // at rather than re-querying with different defaults.
        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            await BuildReportAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Performance Report");
            csv.AppendLine($"Report Period,{Csv(string.IsNullOrWhiteSpace(Period) ? "All Periods" : Period)}");
            csv.AppendLine($"Department,{Csv(DepartmentLabel())}");
            csv.AppendLine($"Generated,{Csv(DateTime.Now.ToString("MMMM d yyyy h:mm tt"))}");
            csv.AppendLine();
            csv.AppendLine($"Employees,{EmployeeCount}");
            csv.AppendLine($"Appraisals in Period,{AppraisalCount}");
            csv.AppendLine($"Average Score,{AverageScore.ToString("0.##")}");
            csv.AppendLine($"Completed Work Items,{CompletedWorkItems}");
            csv.AppendLine();
            csv.AppendLine("Employee Name,Role,Department,Appraisals,Completed,Average,Rating");

            foreach (var r in Rows)
            {
                csv.AppendLine(string.Join(",",
                    Csv(r.EmployeeName),
                    Csv(r.RoleLabel),
                    Csv(r.DepartmentName),
                    r.Appraisals,
                    r.Completed,
                    r.Appraisals == 0 ? "" : r.AverageScore.ToString("0.##"),
                    Csv(r.Rating)));
            }

            var fileName = $"performance-report-{DateTime.Now:yyyyMMdd-HHmm}.csv";

            // UTF-8 BOM so Excel opens accented names correctly.
            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();

            return File(bytes, "text/csv", fileName);
        }

        private string DepartmentLabel()
        {
            if (DepartmentId == null) return "All Departments";
            return DepartmentOptions.FirstOrDefault(d => d.DepartmentId == DepartmentId)?.DepartmentName
                   ?? "All Departments";
        }

        // Wraps a value for CSV: doubles any quotes and quotes the field so
        // commas in a name can't shift the columns.
        private static string Csv(string? value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        private async Task BuildReportAsync()
        {
            DepartmentOptions = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            PeriodOptions = await _context.PerformanceEvaluations
                .Select(e => e.EvaluationPeriod)
                .Distinct()
                .OrderByDescending(p => p)
                .ToListAsync();

            // Only Field Technicians and Office Staff are evaluated, so
            // Admin/Manager accounts never appear on the report.
            var employeeQuery = _context.Employees
                .Include(e => e.Department)
                .Where(e => e.RoleType != RoleType.Admin && e.RoleType != RoleType.Manager);

            if (DepartmentId.HasValue)
                employeeQuery = employeeQuery.Where(e => e.DepartmentId == DepartmentId.Value);

            var employees = await employeeQuery
                .OrderBy(e => e.FullName)
                .ToListAsync();

            var employeeIds = employees.Select(e => e.EmployeeId).ToList();

            // Evaluations for the selected period. An empty period means the
            // report covers every period on record.
            var evaluationQuery = _context.PerformanceEvaluations
                .Where(e => employeeIds.Contains(e.EmployeeID));

            if (!string.IsNullOrWhiteSpace(Period))
                evaluationQuery = evaluationQuery.Where(e => e.EvaluationPeriod == Period);

            var evaluationSummaries = await evaluationQuery
                .GroupBy(e => e.EmployeeID)
                .Select(g => new
                {
                    EmployeeID = g.Key,
                    Count = g.Count(),
                    Average = g.Average(x => x.OverallScore)
                })
                .ToDictionaryAsync(x => x.EmployeeID, x => x);

            // Completed work items reuse the same helper the evaluation pages
            // use, so "Completed" means the same thing everywhere in the system.
            var stats = await EmployeePerformanceStatsBuilder.BuildAsync(_context, employeeIds);

            Rows = employees.Select(e =>
            {
                evaluationSummaries.TryGetValue(e.EmployeeId, out var summary);
                var stat = stats.GetValueOrDefault(e.EmployeeId) ?? new EmployeePerformanceStats();

                return new ReportRow
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.FullName,
                    RoleType = e.RoleType,
                    DepartmentName = e.Department?.DepartmentName ?? "-",
                    Appraisals = summary?.Count ?? 0,
                    Completed = stat.CompletedJobsTasks,
                    AverageScore = summary == null
                        ? 0
                        : Math.Round(summary.Average, 2, MidpointRounding.AwayFromZero)
                };
            }).ToList();
        }
    }
}
