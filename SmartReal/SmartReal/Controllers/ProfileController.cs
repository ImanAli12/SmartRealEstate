using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var myProperties = await _context.Properties
            .Where(p => p.AdvertiserId == userId)
            .Include(p => p.City)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var myFavorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Property!)
                .ThenInclude(p => p.City)
            .AsNoTracking()
            .ToListAsync();

        var favoriteProperties = myFavorites
            .Where(f => f.Property is not null)
            .Select(f => f.Property!)
            .ToList();

        var recommended = new List<Property>();
        if (favoriteProperties.Count > 0)
        {
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

            recommended = candidates
                .Select(p => new
                {
                    Property = p,
                    Score = (preferredClusters.Contains(p.ClusterId ?? -1) ? 6 : 0)
                        + (preferredTypes.Contains(p.PropertyTypeId) ? 3 : 0)
                        + (preferredCities.Contains(p.CityId) ? 2 : 0)
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Property.CreatedAt)
                .Take(8)
                .Select(x => x.Property)
                .ToList();
        }

        ViewBag.MyProperties = myProperties;
        ViewBag.MyFavorites = myFavorites;
        ViewBag.RecommendedProperties = recommended;
        return View();
    }
}