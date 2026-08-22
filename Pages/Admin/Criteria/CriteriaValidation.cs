using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;
using CriteriaModel = TM_PE.Model.Criteria;

namespace TM_PE.Pages.Admin.Criteria;

// Shared by Create/Edit (Admin and, if it ever grows its own forms, Manager
// too). Field Technician criteria are automated around exactly three
// MetricType values (see Model.Criteria.MetricType) - Job Completion,
// Timeliness, Work Quality - so a technician's Overall Score is never scored
// twice against the same underlying number, and the active set can never add
// up to more than the 100% the Overall Score is out of. Office Staff criteria
// are untouched: they're always WorkQuality and this validation never applies
// to them.
public static class CriteriaValidation
{
    // Pass excludingId when editing an existing criterion so it doesn't
    // collide with its own prior values.
    public static async Task<string?> ValidateFieldTechnicianAsync(
        AppDbContext db, CriteriaModel item, int? excludingId)
    {
        if (item.RoleType != RoleType.FieldTechnician || !item.IsActive)
        {
            return null;
        }

        var others = await db.Criteria
            .Where(c => c.RoleType == RoleType.FieldTechnician
                && c.IsActive
                && (excludingId == null || c.CriteriaId != excludingId.Value))
            .ToListAsync();

        if (others.Any(c => c.MetricType == item.MetricType))
        {
            return $"An active Field Technician criterion already uses \"{DisplayName(item.MetricType)}\" as its scoring source. " +
                   "Job Completion, Timeliness, and Work Quality can each only be used once - deactivate the other one first if you want to replace it.";
        }

        var totalWeight = others.Sum(c => c.Weight) + item.Weight;
        if (totalWeight > 100)
        {
            return $"Active Field Technician criteria would total {totalWeight.ToString("0.##")}% weight, which is over 100%. " +
                   "Lower this criterion's weight, or deactivate another Field Technician criterion first.";
        }

        return null;
    }

    private static string DisplayName(CriteriaMetricType type) => type switch
    {
        CriteriaMetricType.JobCompletion => "Job Completion",
        CriteriaMetricType.Timeliness => "Timeliness",
        _ => "Work Quality"
    };
}
