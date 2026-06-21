using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class NeighborhoodsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NeighborhoodsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var neighborhoods = await _context.Neighborhoods
            .Include(n => n.City)
            .AsNoTracking()
            .OrderBy(n => n.NameAr)
            .ToListAsync();

        return View(neighborhoods);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.CityId = new SelectList(await _context.Cities.OrderBy(c => c.NameAr).ToListAsync(), "Id", "NameAr");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Neighborhood neighborhood)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CityId = new SelectList(await _context.Cities.OrderBy(c => c.NameAr).ToListAsync(), "Id", "NameAr", neighborhood.CityId);
            return View(neighborhood);
        }

        _context.Neighborhoods.Add(neighborhood);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var neighborhood = await _context.Neighborhoods.FindAsync(id);
        if (neighborhood is null)
        {
            return NotFound();
        }

        ViewBag.CityId = new SelectList(await _context.Cities.OrderBy(c => c.NameAr).ToListAsync(), "Id", "NameAr", neighborhood.CityId);
        return View(neighborhood);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Neighborhood neighborhood)
    {
        if (id != neighborhood.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.CityId = new SelectList(await _context.Cities.OrderBy(c => c.NameAr).ToListAsync(), "Id", "NameAr", neighborhood.CityId);
            return View(neighborhood);
        }

        _context.Update(neighborhood);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var neighborhood = await _context.Neighborhoods.Include(n => n.City).AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        return neighborhood is null ? NotFound() : View(neighborhood);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var neighborhood = await _context.Neighborhoods.FindAsync(id);
        if (neighborhood is not null)
        {
            _context.Neighborhoods.Remove(neighborhood);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}