using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class PropertyTypesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PropertyTypesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index() => View(await _context.PropertyTypes.AsNoTracking().OrderBy(t => t.NameAr).ToListAsync());

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyType propertyType)
    {
        if (!ModelState.IsValid)
        {
            return View(propertyType);
        }

        _context.PropertyTypes.Add(propertyType);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var propertyType = await _context.PropertyTypes.FindAsync(id);
        return propertyType is null ? NotFound() : View(propertyType);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyType propertyType)
    {
        if (id != propertyType.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(propertyType);
        }

        _context.Update(propertyType);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var propertyType = await _context.PropertyTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return propertyType is null ? NotFound() : View(propertyType);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var propertyType = await _context.PropertyTypes.FindAsync(id);
        if (propertyType is not null)
        {
            _context.PropertyTypes.Remove(propertyType);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}