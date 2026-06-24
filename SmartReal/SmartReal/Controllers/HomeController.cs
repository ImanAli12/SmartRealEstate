using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Models;
using SmartReal.Data;

namespace SmartReal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // الصفحة الرئيسية
        public IActionResult Index()
        {
            // جلب المدن للشبكة
            var cities = _context.Cities.ToList();
            // جلب أحدث 6 عقارات مميزة
            var featured = _context.Properties
                .Include(p => p.Images)
                .Include(p => p.City)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToList();

            ViewBag.Cities = cities;
            return View(featured);
        }

        // صفحة "من نحن" (A.html سابقاً)
        public IActionResult About()
        {
            return View();
        }

        // صفحة الطلبات (requests.html)
        public IActionResult Requests()
        {
            return View();
        }
    }
}