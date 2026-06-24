using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Models;
using SmartReal.Data;
using System.Security.Claims;

namespace SmartReal.Controllers
{
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PropertyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // قائمة العقارات (مع فلتر حسب المحافظة)
        public IActionResult Index(string province)
        {
            var query = _context.Properties
                .Include(p => p.Images)
                .Include(p => p.City)
                .Include(p => p.Neighborhood)
                .Include(p => p.PropertyType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(province))
            {
                var city = _context.Cities
                    .FirstOrDefault(c => c.NameAr.Contains(province) || c.NameEn.Contains(province));
                if (city != null)
                    query = query.Where(p => p.CityId == city.Id);
            }

            return View(query.ToList());
        }

        // صفحة البحث (تعرض الفورم والنتائج)
        public IActionResult Search(string keyword, int? cityId, int? typeId, decimal? minPrice, decimal? maxPrice)
        {
            ViewBag.Cities = _context.Cities.ToList();
            ViewBag.PropertyTypes = _context.PropertyTypes.ToList();

            var query = _context.Properties
                .Include(p => p.Images)
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.Title.Contains(keyword) || p.Description.Contains(keyword));
            if (cityId.HasValue && cityId.Value > 0)
                query = query.Where(p => p.CityId == cityId.Value);
            if (typeId.HasValue && typeId.Value > 0)
                query = query.Where(p => p.PropertyTypeId == typeId.Value);
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            var results = query.ToList();
            return View("SearchResults", results);
        }

        // تفاصيل العقار
        public IActionResult Details(int id)
        {
            var property = _context.Properties
                .Include(p => p.Images)
                .Include(p => p.City)
                .Include(p => p.Neighborhood)
                .Include(p => p.PropertyType)
                .Include(p => p.Advertiser)
                .Include(p => p.Cluster)
                .FirstOrDefault(p => p.Id == id);

            if (property == null) return NotFound();
            return View(property);
        }

        // تبديل المفضلة (Ajax)
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int propertyId)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, message = "يجب تسجيل الدخول" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favorite = _context.Favorites
                .FirstOrDefault(f => f.UserId == userId && f.PropertyId == propertyId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = false });
            }
            else
            {
                _context.Favorites.Add(new Favorite
                {
                    UserId = userId,
                    PropertyId = propertyId,
                    SavedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = true });
            }
        }

        // جلب قائمة المفضلة للمستخدم الحالي (Ajax)
        public IActionResult GetFavoritesStatus()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new List<int>());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ids = _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.PropertyId)
                .ToList();
            return Json(ids);
        }
    }
}