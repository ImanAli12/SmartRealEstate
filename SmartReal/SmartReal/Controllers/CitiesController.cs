using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class CitiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CitiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index() => View(await _context.Cities.AsNoTracking().OrderBy(c => c.NameAr).ToListAsync());

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(City city)
    {
        if (!ModelState.IsValid)
        {
            return View(city);
        }

        _context.Cities.Add(city);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        return city is null ? NotFound() : View(city);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, City city)
    {
        if (id != city.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(city);
        }

        _context.Update(city);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var city = await _context.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return city is null ? NotFound() : View(city);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city is not null)
        {
            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}