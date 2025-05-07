using System.Threading.Tasks;
using dotnetprojekt.Models;
using dotnetprojekt.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using dotnetprojekt.Context;
using System.Linq;

namespace dotnetprojekt.Controllers
{
    public class SearchController : Controller
    {
        private readonly SearchService _searchService;
        private readonly WineLoversContext _context;

        public SearchController(SearchService searchService, WineLoversContext context)
        {
            _searchService = searchService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string query, 
            int? typeId = null, 
            int? countryId = null, 
            int? regionId = null,
            int? grapeId = null,
            int? wineryId = null,  // Re-added wineryId parameter
            int? acidityId = null, 
            decimal? minAbv = null, 
            decimal? maxAbv = null,
            int? minVintage = null,
            int? maxVintage = null,
            SearchService.SortOption sort = SearchService.SortOption.Relevance)
        {
            ViewData["Query"] = query;
            
            // Load all filter data for the view and sort alphabetically
            ViewData["WineTypes"] = await _context.WineTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
                
            ViewData["Countries"] = await _context.Countries
                .OrderBy(c => c.Name)
                .ToListAsync();
                
            ViewData["AcidityLevels"] = await _context.WineAcidities
                .OrderBy(a => a.Name)
                .ToListAsync();
                
            ViewData["Grapes"] = await _context.Grapes
                .OrderBy(g => g.Name)
                .ToListAsync();
            
            // Load all regions, but keep the sorting logic for better UX
            if (countryId.HasValue)
            {
                // Only regions from selected country
                ViewData["Regions"] = await _context.Regions
                    .Where(r => r.CountryId == countryId.Value)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            else
            {
                // All regions sorted alphabetically with no limit
                ViewData["Regions"] = await _context.Regions
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            
            // Load all wineries, but keep the filtering logic for better UX
            if (regionId.HasValue)
            {
                // Only wineries from selected region
                ViewData["Wineries"] = await _context.Wineries
                    .Where(w => w.RegionId == regionId.Value)
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            else if (countryId.HasValue)
            {
                // Wineries from regions in the selected country
                var regionIds = await _context.Regions
                    .Where(r => r.CountryId == countryId.Value)
                    .Select(r => r.Id)
                    .ToListAsync();
                    
                ViewData["Wineries"] = await _context.Wineries
                    .Where(w => regionIds.Contains(w.RegionId))
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            else
            {
                // All wineries sorted alphabetically with no limit
                ViewData["Wineries"] = await _context.Wineries
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            
            // Set selected filters in ViewData
            ViewData["SelectedType"] = typeId;
            ViewData["SelectedCountry"] = countryId;
            ViewData["SelectedRegion"] = regionId;
            ViewData["SelectedGrape"] = grapeId;
            ViewData["SelectedWinery"] = wineryId;  // Re-added selected winery
            ViewData["SelectedAcidity"] = acidityId;
            ViewData["MinAbv"] = minAbv;
            ViewData["MaxAbv"] = maxAbv;
            ViewData["MinVintage"] = minVintage;
            ViewData["MaxVintage"] = maxVintage;
            ViewData["SelectedSort"] = sort;
            
            // Always search for wines, with or without filters
            var results = await _searchService.SearchWines(
                query, typeId, countryId, regionId, grapeId, wineryId,  // Re-added wineryId parameter
                acidityId, minAbv, maxAbv, minVintage, maxVintage, sort);
                
            return View(results);
        }

        // Add API endpoint to get regions for a country (for dynamic filtering)
        [HttpGet]
        [Route("api/regions")]
        public async Task<IActionResult> GetRegions(int countryId)
        {
            var regions = await _context.Regions
                .Where(r => r.CountryId == countryId)
                .OrderBy(r => r.Name)
                .Select(r => new { id = r.Id, name = r.Name })
                .ToListAsync();
                
            return Json(regions);
        }
        
        // Add API endpoint to get wineries for a region (for dynamic filtering)
        [HttpGet]
        [Route("api/wineries")]
        public async Task<IActionResult> GetWineries(int regionId)
        {
            var wineries = await _context.Wineries
                .Where(w => w.RegionId == regionId)
                .OrderBy(w => w.Name)
                .Select(w => new { id = w.Id, name = w.Name })
                .ToListAsync();
                
            return Json(wineries);
        }

        // Add endpoint to get wineries for a country
        [HttpGet]
        [Route("api/wineries-by-country")]
        public async Task<IActionResult> GetWineriesByCountry(int countryId)
        {
            var regionIds = await _context.Regions
                .Where(r => r.CountryId == countryId)
                .Select(r => r.Id)
                .ToListAsync();
                
            var wineries = await _context.Wineries
                .Where(w => regionIds.Contains(w.RegionId))
                .OrderBy(w => w.Name)
                .Select(w => new { id = w.Id, name = w.Name, regionId = w.RegionId })
                .ToListAsync();
                
            return Json(wineries);
        }

        [HttpGet]
        [Route("api/search")]
        public async Task<IActionResult> LiveSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<Wine>());
            }
            
            var results = await _searchService.LiveSearch(query);
            
            // Transform to simpler object for JSON response
            var searchResults = results.Select(w => new
            {
                id = w.Id,
                name = w.Name,
                type = w.Type?.Name ?? "Unknown"
            }).ToList();
            
            return Json(searchResults);
        }
    }
}