using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;
using RealEstateWebApp.Services;
using RealEstateWebApp.ViewModels;

namespace RealEstateWebApp.Controllers;

[Authorize]
public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly KMeansService _kMeansService;

    public PropertiesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        KMeansService kMeansService)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _kMeansService = kMeansService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(PropertySearchViewModel filters)
    {
        await PopulateSearchLookupsAsync(filters);

        filters.RecommendationMode = HasRecommendationCriteria(filters);

        if (filters.RecommendationMode)
        {
            filters.Recommendations = await _kMeansService.RecommendTopPropertiesAsync(filters, 10);
            filters.Results = filters.Recommendations.Select(item => item.Property).ToList();
        }
        else
        {
            var query = ApplyFilters(
                _context.Properties
                    .Where(p => p.IsActive)
                    .Include(p => p.City)
                    .Include(p => p.Neighborhood)
                    .Include(p => p.PropertyType)
                    .Include(p => p.Images)
                    .Include(p => p.Cluster)
                    .AsNoTracking(),
                filters);

            filters.Results = await ApplySorting(query, filters.SortBy).ToListAsync();
        }

        return View(filters);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Search(PropertySearchViewModel filters) => await Index(filters);

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var property = await _context.Properties
            .Include(p => p.City)
            .Include(p => p.Neighborhood)
            .Include(p => p.PropertyType)
            .Include(p => p.Images)
            .Include(p => p.Cluster)
            .Include(p => p.Advertiser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        property.ViewsCount += 1;
        _context.Update(property);
        await _context.SaveChangesAsync();

        ViewBag.SimilarProperties = await _kMeansService.RecommendSimilarPropertiesAsync(property, 6, cancellationToken);

        return View(property);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetNeighborhoods(int cityId)
    {
        var neighborhoods = await _context.Neighborhoods
            .Where(n => n.CityId == cityId)
            .OrderBy(n => n.NameAr)
            .Select(n => new { n.Id, n.NameAr })
            .ToListAsync();

        return Json(neighborhoods);
    }

    public async Task<IActionResult> Create()
    {
        var viewModel = new PropertyUpsertViewModel
        {
            AdvertiserId = _userManager.GetUserId(User) ?? string.Empty,
            Cities = await GetCitiesAsync(),
            PropertyTypes = await GetPropertyTypesAsync(),
            Clusters = await GetClustersAsync()
        };

        return View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyUpsertViewModel viewModel)
    {
        viewModel.AdvertiserId = _userManager.GetUserId(User) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(viewModel.AdvertiserId))
        {
            return Challenge();
        }

        await ValidatePropertySelectionAsync(viewModel);

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(viewModel);
            return View(viewModel);
        }

        var property = new Property
        {
            Code = viewModel.Code,
            Title = viewModel.Title,
            Price = viewModel.Price,
            PriceCurrency = viewModel.PriceCurrency,
            Area = viewModel.Area,
            Rooms = viewModel.Rooms,
            Bathrooms = viewModel.Bathrooms,
            Floor = viewModel.Floor,
            PropertyTypeId = viewModel.PropertyTypeId,
            Status = viewModel.Status,
            CityId = viewModel.CityId,
            NeighborhoodId = viewModel.NeighborhoodId,
            Address = viewModel.Address,
            Latitude = viewModel.Latitude,
            Longitude = viewModel.Longitude,
            Description = viewModel.Description,
            AdvertiserId = viewModel.AdvertiserId,
            IsActive = viewModel.IsActive,
            ClusterId = viewModel.ClusterId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        await SaveImagesAsync(property.Id, viewModel.ImageFiles);

        TempData["SuccessMessage"] = "تمت إضافة العقار بنجاح.";

        return RedirectToAction(nameof(Details), new { id = property.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var property = await _context.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
        {
            return NotFound();
        }

        var viewModel = new PropertyUpsertViewModel
        {
            Id = property.Id,
            Code = property.Code,
            Title = property.Title,
            Price = property.Price,
            PriceCurrency = property.PriceCurrency,
            Area = property.Area,
            Rooms = property.Rooms,
            Bathrooms = property.Bathrooms,
            Floor = property.Floor,
            PropertyTypeId = property.PropertyTypeId,
            Status = property.Status,
            CityId = property.CityId,
            NeighborhoodId = property.NeighborhoodId,
            Address = property.Address,
            Latitude = property.Latitude,
            Longitude = property.Longitude,
            Description = property.Description,
            AdvertiserId = property.AdvertiserId,
            IsActive = property.IsActive,
            ClusterId = property.ClusterId
        };

        await PopulateLookupsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyUpsertViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        await ValidatePropertySelectionAsync(viewModel);

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(viewModel);
            return View(viewModel);
        }

        var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
        {
            return NotFound();
        }

        property.Code = viewModel.Code;
        property.Title = viewModel.Title;
        property.Price = viewModel.Price;
        property.PriceCurrency = viewModel.PriceCurrency;
        property.Area = viewModel.Area;
        property.Rooms = viewModel.Rooms;
        property.Bathrooms = viewModel.Bathrooms;
        property.Floor = viewModel.Floor;
        property.PropertyTypeId = viewModel.PropertyTypeId;
        property.Status = viewModel.Status;
        property.CityId = viewModel.CityId;
        property.NeighborhoodId = viewModel.NeighborhoodId;
        property.Address = viewModel.Address;
        property.Latitude = viewModel.Latitude;
        property.Longitude = viewModel.Longitude;
        property.Description = viewModel.Description;
        property.IsActive = viewModel.IsActive;
        property.ClusterId = viewModel.ClusterId;

        await SaveImagesAsync(property.Id, viewModel.ImageFiles);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم تحديث بيانات العقار.";

        return RedirectToAction(nameof(Details), new { id = property.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var property = await _context.Properties
            .Include(p => p.Images)
            .Include(p => p.City)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return property is null ? NotFound() : View(property);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var property = await _context.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (property is not null)
        {
            DeletePropertyImages(property.Images);
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "تم حذف العقار.";

        return RedirectToAction(nameof(Index));
    }

    private IQueryable<Property> ApplyFilters(IQueryable<Property> query, PropertySearchViewModel filters)
    {
        if (filters.CityId.HasValue)
        {
            query = query.Where(p => p.CityId == filters.CityId.Value);
        }

        if (filters.NeighborhoodId.HasValue)
        {
            query = query.Where(p => p.NeighborhoodId == filters.NeighborhoodId.Value);
        }

        if (filters.PropertyTypeId.HasValue)
        {
            query = query.Where(p => p.PropertyTypeId == filters.PropertyTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            query = query.Where(p => p.Status == filters.Status);
        }

        if (filters.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filters.MinPrice.Value);
        }

        if (filters.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filters.MaxPrice.Value);
        }

        if (filters.MinArea.HasValue)
        {
            query = query.Where(p => p.Area >= filters.MinArea.Value);
        }

        if (filters.MaxArea.HasValue)
        {
            query = query.Where(p => p.Area <= filters.MaxArea.Value);
        }

        if (filters.Rooms.HasValue)
        {
            query = query.Where(p => p.Rooms == filters.Rooms.Value);
        }

        if (filters.Bathrooms.HasValue)
        {
            query = query.Where(p => p.Bathrooms == filters.Bathrooms.Value);
        }

        if (filters.Floor.HasValue)
        {
            query = query.Where(p => p.Floor == filters.Floor.Value);
        }

        if (filters.ClusterId.HasValue)
        {
            query = query.Where(p => p.ClusterId == filters.ClusterId.Value);
        }

        return query;
    }

    private static IQueryable<Property> ApplySorting(IQueryable<Property> query, string? sortBy)
    {
        return sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }

    private async Task PopulateLookupsAsync(PropertyUpsertViewModel viewModel)
    {
        viewModel.Cities = await GetCitiesAsync(viewModel.CityId);
        viewModel.PropertyTypes = await GetPropertyTypesAsync(viewModel.PropertyTypeId);
        viewModel.Clusters = await GetClustersAsync(viewModel.ClusterId);
        viewModel.Neighborhoods = await GetNeighborhoodsAsync(viewModel.CityId, viewModel.NeighborhoodId);
    }

    private async Task PopulateSearchLookupsAsync(PropertySearchViewModel filters)
    {
        var selectedCityId = filters.TargetCityId ?? filters.CityId;
        var selectedPropertyTypeId = filters.TargetPropertyTypeId ?? filters.PropertyTypeId;
        var selectedNeighborhoodId = filters.TargetNeighborhoodId;

        filters.Cities = await GetCitiesAsync(selectedCityId);
        filters.Neighborhoods = await GetNeighborhoodsAsync(selectedCityId, selectedNeighborhoodId);
        filters.PropertyTypes = await GetPropertyTypesAsync(selectedPropertyTypeId);
        filters.Clusters = await GetClustersAsync(filters.ClusterId);
    }

    private static bool HasRecommendationCriteria(PropertySearchViewModel filters)
    {
        return filters.TargetPrice.HasValue
            || filters.TargetArea.HasValue
            || filters.TargetRooms.HasValue
            || filters.TargetBathrooms.HasValue
            || filters.TargetFloor.HasValue
            || filters.TargetCityId.HasValue
            || filters.TargetNeighborhoodId.HasValue
            || filters.TargetPropertyTypeId.HasValue
            || !string.IsNullOrWhiteSpace(filters.TargetStatus);
    }

    private async Task<List<SelectListItem>> GetCitiesAsync(int? selectedId = null)
    {
        return await _context.Cities.OrderBy(c => c.NameAr)
            .Select(c => new SelectListItem(c.NameAr, c.Id.ToString(), c.Id == selectedId))
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetPropertyTypesAsync(int? selectedId = null)
    {
        return await _context.PropertyTypes.OrderBy(t => t.NameAr)
            .Select(t => new SelectListItem(t.NameAr, t.Id.ToString(), t.Id == selectedId))
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetClustersAsync(int? selectedId = null)
    {
        return await _context.Clusters.Where(c => c.Id > 0).OrderBy(c => c.NameAr)
            .Select(c => new SelectListItem(c.NameAr, c.Id.ToString(), c.Id == selectedId))
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetNeighborhoodsAsync(int? cityId, int? selectedId = null)
    {
        var query = _context.Neighborhoods.AsQueryable();
        if (cityId.HasValue)
        {
            query = query.Where(n => n.CityId == cityId.Value);
        }

        return await query.OrderBy(n => n.NameAr)
            .Select(n => new SelectListItem(n.NameAr, n.Id.ToString(), n.Id == selectedId))
            .ToListAsync();
    }

    private async Task SaveImagesAsync(int propertyId, List<IFormFile> imageFiles)
    {
        if (imageFiles.Count == 0)
        {
            return;
        }

        var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "properties");
        Directory.CreateDirectory(uploadFolder);

        var existingCount = await _context.PropertyImages.CountAsync(pi => pi.PropertyId == propertyId);

        foreach (var imageFile in imageFiles.Where(file => file.Length > 0))
        {
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
            var fullPath = Path.Combine(uploadFolder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            _context.PropertyImages.Add(new PropertyImage
            {
                PropertyId = propertyId,
                ImageUrl = $"/images/properties/{fileName}",
                IsMain = existingCount == 0,
                Order = ++existingCount
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task ValidatePropertySelectionAsync(PropertyUpsertViewModel viewModel)
    {
        if (!await _context.Cities.AnyAsync(c => c.Id == viewModel.CityId))
        {
            ModelState.AddModelError(nameof(viewModel.CityId), "المدينة المختارة غير صحيحة.");
        }

        var neighborhood = await _context.Neighborhoods.FirstOrDefaultAsync(n => n.Id == viewModel.NeighborhoodId);
        if (neighborhood is null)
        {
            ModelState.AddModelError(nameof(viewModel.NeighborhoodId), "الحي المختار غير صحيح.");
        }
        else if (neighborhood.CityId != viewModel.CityId)
        {
            ModelState.AddModelError(nameof(viewModel.NeighborhoodId), "الحي يجب أن يتبع نفس المدينة المختارة.");
        }

        if (!await _context.PropertyTypes.AnyAsync(t => t.Id == viewModel.PropertyTypeId))
        {
            ModelState.AddModelError(nameof(viewModel.PropertyTypeId), "نوع العقار المختار غير صحيح.");
        }

        if (viewModel.ClusterId.HasValue && !await _context.Clusters.AnyAsync(c => c.Id == viewModel.ClusterId.Value))
        {
            ModelState.AddModelError(nameof(viewModel.ClusterId), "المجموعة المختارة غير صحيحة.");
        }

        if (viewModel.Rooms > 30)
        {
            ModelState.AddModelError(nameof(viewModel.Rooms), "عدد الغرف يبدو غير منطقي.");
        }

        if (viewModel.Bathrooms > 20)
        {
            ModelState.AddModelError(nameof(viewModel.Bathrooms), "عدد الحمامات يبدو غير منطقي.");
        }

        if (viewModel.Floor.HasValue && (viewModel.Floor < 0 || viewModel.Floor > 100))
        {
            ModelState.AddModelError(nameof(viewModel.Floor), "الطابق يجب أن يكون بين 0 و 100.");
        }
    }

    private void DeletePropertyImages(IEnumerable<PropertyImage> images)
    {
        foreach (var image in images)
        {
            if (string.IsNullOrWhiteSpace(image.ImageUrl))
            {
                continue;
            }

            var relativePath = image.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_environment.WebRootPath, relativePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}