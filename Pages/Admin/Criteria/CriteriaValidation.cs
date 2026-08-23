using Microsoft.EntityFrameworkCore;
using TM_PE.Data;
using TM_PE.Model;
using CriteriaModel = TM_PE.Model.Criteria;

namespace TM_PE.Pages.Admin.Criteria;

// Shared by Create/Edit (Admin and, if it ever grows its own forms, Manager
// too). Field Technician criteria are automated around exactly three
// MetricType values (see Model.Criteria.MetricType) - Job Completion,
// Timeliness, Work Quality - so a technician's Overall Score is never scored
// twice against the same underlying number. Office Staff criteria are always
// WorkQuality, so the MetricType-uniqueness rule doesn't apply to them, but
// their weights still feed into the same Overall Score, so the active set for
// either role type can never add up to more than the 100% the Overall Score
// is out of.
public static class CriteriaValidation
{
    // Pass excludingId when editing an existing criterion so it doesn't
    // collide with its own prior values.
    public static async Task<string?> ValidateAsync(
        AppDbContext db, CriteriaModel item, int? excludingId)
    {
        if (!item.IsActive
            || (item.RoleType != RoleType.FieldTechnician && item.RoleType != RoleType.OfficeStaff))
        {
            return null;
        }

        var others = await db.Criteria
            .Where(c => c.RoleType == item.RoleType
                && c.IsActive
                && (excludingId == null || c.CriteriaId != excludingId.Value))
            .ToListAsync();

        if (item.RoleType == RoleType.FieldTechnician && others.Any(c => c.MetricType == item.MetricType))
        {
            return $"An active Field Technician criterion already uses \"{DisplayName(item.MetricType)}\" as its scoring source. " +
                   "Job Completion, Timeliness, and Work Quality can each only be used once - deactivate the other one first if you want to replace it.";
        }

        var totalWeight = others.Sum(c => c.Weight) + item.Weight;
        if (totalWeight > 100)
        {
            var roleLabel = item.RoleType == RoleType.FieldTechnician ? "Field Technician" : "Office Staff";
            return $"Active {roleLabel} criteria would total {totalWeight.ToString("0.##")}% weight, which is over 100%. " +
                   $"Lower this criterion's weight, or deactivate another {roleLabel} criterion first.";
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
