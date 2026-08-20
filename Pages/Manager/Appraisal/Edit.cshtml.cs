//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.EntityFrameworkCore;
//using TM_PE.Data;
//using TM_PE.Model;

//namespace TM_PE.Pages.Manager.Appraisal
//{
//    // The Employee and Related Evaluation on an appraisal can't be changed
//    // once created — only the decision fields can. Finalized appraisals are
//    // locked entirely, same as a Finalized Performance Evaluation.
//    public class EditModel : PageModel
//    {
//        private readonly AppDbContext _context;
//        public EditModel(AppDbContext context) => _context = context;

//        [BindProperty]
//        public Model.Appraisal Item { get; set; } = new();

//        public async Task<IActionResult> OnGetAsync(int id)
//        {
//            var item = await _context.Appraisals
//                .Include(a => a.Employee)
//                .Include(a => a.Evaluation)
//                .FirstOrDefaultAsync(a => a.AppraisalID == id);

//            if (item == null) return NotFound();
//            if (item.AppraisalStatus == AppraisalStatus.Finalized)
//                return RedirectToPage("Details", new { id });

//            Item = item;
//            return Page();
//        }

//        public async Task<IActionResult> OnPostAsync(int id)
//        {
//            var item = await _context.Appraisals.FindAsync(id);
//            if (item == null) return NotFound();
//            if (item.AppraisalStatus == AppraisalStatus.Finalized)
//                return RedirectToPage("Details", new { id });

//            ModelState.Remove("Item.Employee");
//            ModelState.Remove("Item.Evaluation");
//            ModelState.Remove("Item.EmployeeID");
//            ModelState.Remove("Item.EvaluationID");
//            ModelState.Remove("Item.OverallRating");

//            if (!ModelState.IsValid)
//            {
//                Item.AppraisalID = id;
//                return Page();
//            }

//            item.AppraisalDate = Item.AppraisalDate;
//            item.Recommendation = Item.Recommendation;
//            item.SalaryAdjustmentRecommendation = Item.SalaryAdjustmentRecommendation;
//            item.PromotionRecommendation = Item.PromotionRecommendation;
//            item.TrainingRecommendation = Item.TrainingRecommendation;
//            item.ManagerRemarks = string.IsNullOrWhiteSpace(Item.ManagerRemarks) ? null : Item.ManagerRemarks.Trim();
//            item.AppraisalStatus = Item.AppraisalStatus;

//            await _context.SaveChangesAsync();
//            return RedirectToPage("Details", new { id = item.AppraisalID });
//        }
//    }
//}