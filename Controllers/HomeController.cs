using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Models;
using dotnetprojekt.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace dotnetprojekt.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly WineLoversContext _context;

    public HomeController(ILogger<HomeController> logger, WineLoversContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        // Get top rated wines (highest average rating)
        var topRatedWines = _context.Wines
            .Include(w => w.Ratings)
            .Include(w => w.Type)
            .Include(w => w.Country)
            .Where(w => w.Ratings.Count > 0)
            .Select(w => new
            {
                Wine = w,
                AverageRating = w.Ratings.Average(r => r.RatingValue)
            })
            .OrderByDescending(x => x.AverageRating)
            .Take(3)
            .Select(x => x.Wine)
            .ToList();

        return View(topRatedWines);
    }

    
    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
