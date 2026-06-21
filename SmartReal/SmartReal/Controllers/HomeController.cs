using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;
using RealEstateWebApp.ViewModels;

namespace RealEstateWebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var latest = await _context.Properties
            .Include(p => p.City)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .AsNoTracking()
            .ToListAsync();

        var featured = await _context.Properties
            .Include(p => p.City)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.ViewsCount)
            .Take(6)
            .AsNoTracking()
            .ToListAsync();

        var recommendations = await GetRecommendationsForCurrentUserAsync(limit: 8);

        var model = new HomeIndexViewModel
        {
            LatestProperties = latest,
            FeaturedProperties = featured,
            PersonalizedRecommendations = recommendations,
            HasPersonalizedRecommendations = recommendations.Count > 0,
            Cities = await _context.Cities.OrderBy(c => c.NameAr).AsNoTracking().ToListAsync(),
            PropertyTypes = await _context.PropertyTypes.OrderBy(t => t.NameAr).AsNoTracking().ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<List<Property>> GetRecommendationsForCurrentUserAsync(int limit)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new List<Property>();
        }

        var favoriteProperties = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.Property!)
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToListAsync();

        if (favoriteProperties.Count == 0)
        {
            return new List<Property>();
        }

        var excludedIds = favoriteProperties.Select(p => p.Id).ToHashSet();
        var preferredClusters = favoriteProperties.Where(p => p.ClusterId.HasValue).Select(p => p.ClusterId!.Value).ToHashSet();
        var preferredCities = favoriteProperties.Select(p => p.CityId).ToHashSet();
        var preferredTypes = favoriteProperties.Select(p => p.PropertyTypeId).ToHashSet();

        var candidates = await _context.Properties
            .Include(p => p.City)
            .Include(p => p.Images)
            .Where(p => p.IsActive && !excludedIds.Contains(p.Id))
            .AsNoTracking()
            .ToListAsync();

        return candidates
            .Select(p => new
            {
                Property = p,
                Score = (preferredClusters.Contains(p.ClusterId ?? -1) ? 6 : 0)
                    + (preferredTypes.Contains(p.PropertyTypeId) ? 3 : 0)
                    + (preferredCities.Contains(p.CityId) ? 2 : 0)
                    + (p.ViewsCount > 100 ? 1 : 0)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Property.CreatedAt)
            .Take(limit)
            .Select(x => x.Property)
            .ToList();
    }
}
