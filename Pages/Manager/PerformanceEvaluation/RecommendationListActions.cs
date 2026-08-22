using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TM_PE.Data;

namespace TM_PE.Pages.Manager.PerformanceEvaluation
{
    // Add/delete behaviour for the manager-maintained recommendation list,
    // shared by the Create and Edit page handlers so both pages validate and
    // respond identically. Mirrors the Fiber Plans handlers on Job Ticket
    // Create, returning JSON for the modal's AJAX calls.
    public static class RecommendationListActions
    {
        public static async Task<IActionResult> AddAsync(AppDbContext context, string recommendationName)
        {
            var name = (recommendationName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Fail("Please enter a recommendation.", 400);

            if (name.Length > TM_PE.Model.RecommendationRules.MaxLength)
                return Fail($"Recommendation is too long (max {TM_PE.Model.RecommendationRules.MaxLength} characters).", 400);

            if (!Regex.IsMatch(name, TM_PE.Model.RecommendationRules.AllowedPattern))
                return Fail("Only letters, numbers, spaces, and . , - / # & ( ) are allowed.", 400);

            if (await context.Recommendations.AnyAsync(r => r.RecommendationName == name))
                return Fail("That recommendation already exists.", 400);

            var recommendation = new TM_PE.Model.Recommendation { RecommendationName = name };
            context.Recommendations.Add(recommendation);
            await context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = recommendation.RecommendationID,
                name = recommendation.RecommendationName
            });
        }

        public static async Task<IActionResult> DeleteAsync(AppDbContext context, int id)
        {
            var recommendation = await context.Recommendations.FindAsync(id);
            if (recommendation == null)
                return Fail("Recommendation not found.", 404);

            context.Recommendations.Remove(recommendation);
            await context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        private static IActionResult Fail(string message, int statusCode) =>
            new JsonResult(new { success = false, message }) { StatusCode = statusCode };
    }
}
