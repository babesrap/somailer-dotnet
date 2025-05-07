using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace dotnetprojekt.Controllers
{
    public class WineController : Controller
    {
        private readonly WineLoversContext _context;
        private readonly ILogger<WineController> _logger;

        public WineController(WineLoversContext context, ILogger<WineController> logger)
        {
            _context = context;
            _logger = logger;
        }        public async Task<IActionResult> Details(int id)
        {
            var wine = await _context.Wines
                .Include(w => w.Type)
                .Include(w => w.Country)
                .Include(w => w.Acidity)
                .Include(w => w.Ratings)
                .Include(w => w.Winery)
                    .ThenInclude(winery => winery.Region)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wine == null)
            {
                return NotFound();
            }

            // Load related grapes
            if (wine.GrapeIds.Length > 0)
            {
                wine.Grapes = await _context.Grapes
                    .Where(g => wine.GrapeIds.Contains(g.Id))
                    .ToListAsync();
            }

            // Load paired dishes
            if (wine.PairWithIds.Length > 0)
            {
                wine.PairedDishes = await _context.Dishes
                    .Where(d => wine.PairWithIds.Contains(d.Id))
                    .ToListAsync();
            }

            return View(wine);
        }

        [Authorize]
        public async Task<IActionResult> PersonalRecommendations()
        {
            try
            {
                // Get current user ID
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    _logger.LogWarning("Failed to parse user ID when retrieving personal recommendations");
                    return RedirectToAction("Index", "Quiz");
                }

                // Get user preferences
                var preferences = await _context.UserPreferences
                    .Include(p => p.PreferredWineType)
                    .Include(p => p.PreferredAcidity)
                    .Include(p => p.PreferredCountry)
                    .Include(p => p.PreferredRegion)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences == null)
                {
                    // User hasn't completed the quiz yet
                    TempData["Message"] = "Complete the Wine Quiz to get personalized recommendations.";
                    return RedirectToAction("Index", "Quiz");
                }

                // Get recommendations based on user preferences
                var recommendations = await GetPersonalizedRecommendations(preferences);

                // Create view model
                var viewModel = new PersonalRecommendationsViewModel
                {
                    Preferences = preferences,
                    Recommendations = recommendations,
                    LastUpdated = preferences.LastUpdated
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal recommendations");
                TempData["ErrorMessage"] = "An error occurred while retrieving your recommendations.";
                return RedirectToAction("Index", "Home");
            }
        }        private async Task<List<Wine>> GetPersonalizedRecommendations(UserPreference preferences)
        {
            var query = _context.Wines
                .Include(w => w.Type)
                .Include(w => w.Country)
                .Include(w => w.Acidity)
                .Include(w => w.Ratings)
                .Include(w => w.Winery)
                .AsQueryable();

            // Apply wine type filter if specified
            if (preferences.PreferredWineTypeId.HasValue)
            {
                query = query.Where(w => w.TypeId == preferences.PreferredWineTypeId.Value);
            }

            // Apply acidity filter if specified
            if (preferences.PreferredAcidityId.HasValue)
            {
                query = query.Where(w => w.AcidityId == preferences.PreferredAcidityId.Value);
            }

            // Apply country filter if specified
            if (preferences.PreferredCountryId.HasValue)
            {
                query = query.Where(w => w.CountryId == preferences.PreferredCountryId.Value);
            }            // Apply region filter if specified - now filtering through winery
            if (preferences.PreferredRegionId.HasValue)
            {
                // Filter wines that have a winery in the preferred region
                query = query.Where(w => w.Winery != null && w.Winery.RegionId == preferences.PreferredRegionId);
            }

            // Apply dish pairing filter if specified
            if (preferences.PreferredDishIds != null && preferences.PreferredDishIds.Length > 0)
            {
                // Find wines that pair with at least one of the preferred dishes
                query = query.Where(w => preferences.PreferredDishIds.Any(d => w.PairWithIds.Contains(d)));
            }

            // Apply body preference if specified
            if (preferences.BodyPreference.HasValue)
            {
                // For simplicity, we'll use a heuristic based on ABV as a rough proxy for body
                // In a real app, we'd have a dedicated field for body
                decimal minAbv = 0, maxAbv = 20;
                
                switch (preferences.BodyPreference.Value)
                {
                    case 1: // Light
                        maxAbv = 11.5m;
                        break;
                    case 2: // Light-medium
                        minAbv = 10.0m;
                        maxAbv = 12.5m;
                        break;
                    case 3: // Medium
                        minAbv = 11.5m;
                        maxAbv = 13.5m;
                        break;
                    case 4: // Medium-full
                        minAbv = 12.5m;
                        maxAbv = 14.5m;
                        break;
                    case 5: // Full
                        minAbv = 13.5m;
                        break;
                }
                
                // Apply ABV filter as a proxy for body
                if (minAbv > 0)
                    query = query.Where(w => w.ABV >= minAbv);
                if (maxAbv < 20)
                    query = query.Where(w => w.ABV <= maxAbv);
            }

            // Apply flavor preferences if specified
            if (!string.IsNullOrEmpty(preferences.PreferredFlavors))
            {
                var flavors = preferences.PreferredFlavors.Split(',').Select(f => f.Trim().ToLower()).ToList();
                if (flavors.Any())
                {
                    // Use Elaborate field which contains wine descriptions
                    query = query.Where(w => flavors.Any(f => w.Elaborate.ToLower().Contains(f)));
                }
            }

            // Get top wines ordered by average rating
            return await query
                .OrderByDescending(w => w.Ratings.Any() ? w.Ratings.Average(r => r.RatingValue) : 0)
                .Take(10)
                .ToListAsync();
        }
    }
}