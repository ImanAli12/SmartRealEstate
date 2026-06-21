using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FavoritesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> MyFavorites()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Property!)
                .ThenInclude(p => p.Images)
            .Include(f => f.Property!)
                .ThenInclude(p => p.City)
            .AsNoTracking()
            .ToListAsync();

        return View(favorites);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToFavorites(int propertyId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.PropertyId == propertyId);
        if (!exists)
        {
            _context.Favorites.Add(new Favorite { UserId = userId, PropertyId = propertyId });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", "Properties", new { id = propertyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromFavorites(int propertyId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);
        if (favorite is not null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(MyFavorites));
    }
}