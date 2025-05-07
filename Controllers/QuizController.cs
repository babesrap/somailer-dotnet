using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Models;
using dotnetprojekt.Context;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;

namespace dotnetprojekt.Controllers;

public class QuizController : Controller
{
    private readonly ILogger<QuizController> _logger;
    private readonly WineLoversContext _context;

    public QuizController(ILogger<QuizController> logger, WineLoversContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("User accessed the Wine Quiz page");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ProcessQuiz(QuizViewModel model)
    {
        _logger.LogInformation("User submitted wine quiz answers");
        
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        // Get wine preferences based on quiz answers
        var preferences = DeterminePreferences(model);
        
        // Find matching wines
        var recommendations = await GetRecommendations(preferences);
        
        // Store only the wine IDs in TempData to avoid serialization issues
        if (recommendations != null && recommendations.Any())
        {
            TempData["RecommendationIds"] = string.Join(",", recommendations.Select(w => w.Id));
        }
        
        // Save user preferences if the user is authenticated
        if (User.Identity.IsAuthenticated)
        {
            await SaveUserPreferences(model, preferences);
        }
        
        return RedirectToAction("Results");
    }

    public async Task<IActionResult> Results()
    {
        _logger.LogInformation("User viewing quiz results");
        
        // Get recommendation IDs from TempData
        var recommendationIdsString = TempData["RecommendationIds"] as string;
        
        if (string.IsNullOrEmpty(recommendationIdsString))
        {
            return RedirectToAction("Index");
        }
        
        // Parse the IDs and fetch the full wine details
        var recommendationIds = recommendationIdsString.Split(',').Select(int.Parse).ToList();
        
        var recommendations = await _context.Wines
            .Include(w => w.Type)
            .Include(w => w.Country)
            .Include(w => w.Acidity)
            .Include(w => w.Ratings)
            .Where(w => recommendationIds.Contains(w.Id))
            .ToListAsync();
        
        // If no recommendations found, go back to quiz
        if (recommendations == null || !recommendations.Any())
        {
            return RedirectToAction("Index");
        }
        
        // Reorder the recommendations to match the original order
        recommendations = recommendationIds
            .Select(id => recommendations.FirstOrDefault(w => w.Id == id))
            .Where(w => w != null)
            .ToList();
        
        return View(recommendations);
    }

    // New method to save user preferences to the database
    private async Task SaveUserPreferences(QuizViewModel model, QuizPreferences preferences)
    {
        try
        {
            // Get current user ID
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                _logger.LogWarning("Failed to parse user ID when saving quiz preferences");
                return;
            }

            // Check if the user already has preferences
            var userPreference = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userPreference == null)
            {
                // Create new preferences record
                userPreference = new UserPreference
                {
                    UserId = userId,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserPreferences.Add(userPreference);
            }
            else
            {
                // Update timestamp
                userPreference.LastUpdated = DateTime.UtcNow;
            }

            // Update preferences from quiz
            userPreference.PreferredWineTypeId = preferences.PreferredWineTypeId;
            userPreference.PreferredAcidityId = preferences.PreferredAcidityId;
            userPreference.BodyPreference = preferences.BodyPreference;
            userPreference.SweetnessPreference = model.SweetnessPreference;
            userPreference.PreferredDishIds = preferences.PreferredDishIds?.ToArray() ?? Array.Empty<int>();
            userPreference.PreferredFlavors = string.Join(",", preferences.PreferredFlavors ?? new List<string>());
            
            // Save changes
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Saved quiz preferences for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user preferences from quiz");
        }
    }

    private QuizPreferences DeterminePreferences(QuizViewModel model)
    {
        var preferences = new QuizPreferences
        {
            PreferredWineTypeId = model.SweetnessPreference > 3 ? 2 : 1, // If sweetness > 3, prefer white wine over red
            PreferredFlavors = new List<string>()
        };

        // Map food pairings to preferred dish types
        if (model.FoodPairings != null && model.FoodPairings.Any())
        {
            preferences.PreferredDishIds = new List<int>();
            foreach (var foodPairing in model.FoodPairings)
            {
                switch (foodPairing.ToLower())
                {
                    case "meat":
                        preferences.PreferredDishIds.Add(1); // Red meat dish ID
                        preferences.PreferredDishIds.Add(5); // Game meat dish ID
                        break;
                    case "seafood":
                        preferences.PreferredDishIds.Add(2); // Seafood dish ID
                        break;
                    case "pasta":
                        preferences.PreferredDishIds.Add(3); // Pasta dish ID
                        break;
                    case "dessert":
                        preferences.PreferredDishIds.Add(4); // Dessert dish ID
                        break;
                }
            }
        }

        // Map flavor preferences
        if (model.FlavorPreference == "fruity")
        {
            preferences.PreferredFlavors.Add("berry");
            preferences.PreferredFlavors.Add("fruit");
            preferences.PreferredFlavors.Add("citrus");
        }
        else if (model.FlavorPreference == "earthy")
        {
            preferences.PreferredFlavors.Add("oak");
            preferences.PreferredFlavors.Add("earth");
            preferences.PreferredFlavors.Add("mineral");
        }
        else if (model.FlavorPreference == "spicy")
        {
            preferences.PreferredFlavors.Add("spice");
            preferences.PreferredFlavors.Add("pepper");
        }

        // Set acidity level based on user preference
        preferences.PreferredAcidityId = model.AcidityPreference;
        
        // Set body preference
        preferences.BodyPreference = model.BodyPreference;
        
        return preferences;
    }

    private async Task<List<Wine>> GetRecommendations(QuizPreferences preferences)
    {
        // Start with a base query
        var query = _context.Wines
            .Include(w => w.Type)
            .Include(w => w.Country)
            .Include(w => w.Acidity)
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
        
        // Apply dish pairing filter if specified
        if (preferences.PreferredDishIds != null && preferences.PreferredDishIds.Any())
        {
            // Find wines that pair with at least one of the preferred dishes
            query = query.Where(w => preferences.PreferredDishIds.Any(d => w.PairWithIds.Contains(d)));
        }
        
        // TODO: In a real implementation, you would use a more sophisticated algorithm
        // that takes into account flavor preferences and body preference
        
        // Get top 5 wines ordered by average rating
        var recommendations = await query
            .OrderByDescending(w => w.Ratings.Any() ? w.Ratings.Average(r => r.RatingValue) : 0)
            .Take(5)
            .ToListAsync();
            
        return recommendations;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}