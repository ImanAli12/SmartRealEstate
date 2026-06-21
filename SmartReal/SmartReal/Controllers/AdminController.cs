using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;
using RealEstateWebApp.Services;
using RealEstateWebApp.Utils;
using RealEstateWebApp.ViewModels;

namespace RealEstateWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly KMeansService _kMeansService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public AdminController(
        KMeansService kMeansService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _kMeansService = kMeansService;
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var report = await _kMeansService.LoadLatestReportAsync(cancellationToken);

        var model = new AdminDashboardViewModel
        {
            ActivePropertiesCount = _context.Properties.Count(p => p.IsActive),
            LatestKMeansReport = report,
            CentroidsFileExists = System.IO.File.Exists(Path.Combine(_environment.ContentRootPath, "Data", "centroids.json")),
            NormalizerFileExists = System.IO.File.Exists(Path.Combine(_environment.ContentRootPath, "Data", "normalizer.json"))
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> KMeansReport(CancellationToken cancellationToken)
    {
        var report = await _kMeansService.LoadLatestReportAsync(cancellationToken);
        if (report is null)
        {
            TempData["SuccessMessage"] = "لا يوجد تقرير تدريب بعد. شغّل K-Means أولًا.";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(report);
    }

    [HttpGet]
    public IActionResult DownloadKMeansFile(string fileName)
    {
        var allowedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["normalizer"] = Path.Combine(_environment.ContentRootPath, "Data", "normalizer.json"),
            ["centroids"] = Path.Combine(_environment.ContentRootPath, "Data", "centroids.json"),
            ["report"] = Path.Combine(_environment.ContentRootPath, "Data", "kmeans-report.json")
        };

        if (!allowedFiles.TryGetValue(fileName, out var path) || !System.IO.File.Exists(path))
        {
            TempData["ErrorMessage"] = "الملف المطلوب غير موجود بعد. شغّل K-Means أولًا لإنشاء ملفات النتائج.";
            return RedirectToAction(nameof(Dashboard));
        }

        var contentType = "application/json";
        var downloadName = Path.GetFileName(path);
        return PhysicalFile(path, contentType, downloadName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunKMeans(int k = 5)
    {
        try
        {
            await _kMeansService.UpdateClustersAsync(k);
            TempData["SuccessMessage"] = "تم تطبيق خوارزمية K-Means بنجاح.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"فشل تشغيل K-Means: {ex.Message}";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportSampleCsv()
    {
        try
        {
            var admin = await _userManager.FindByEmailAsync("admin@example.com");
            if (admin is null)
            {
                TempData["ErrorMessage"] = "تعذر العثور على حساب الإدمن لاستيراد البيانات.";
                return RedirectToAction(nameof(Dashboard));
            }

            var excelPath = Path.Combine(_environment.ContentRootPath, "real_estate.xlsx");
            var importPath = System.IO.File.Exists(excelPath)
                ? excelPath
                : Path.Combine(_environment.ContentRootPath, "Utils", "sample-properties.csv");

            var importedCount = await PropertyCsvImporter.ImportAsync(_context, importPath, admin.Id);
            TempData["SuccessMessage"] = importedCount > 0
                ? $"تم استيراد {importedCount} عقار من ملف {Path.GetFileName(importPath)}."
                : $"لم يتم استيراد عقارات جديدة من ملف {Path.GetFileName(importPath)} (إما البيانات موجودة مسبقًا أو لا يوجد صفوف صالحة جديدة).";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"فشل استيراد البيانات: {ex.Message}";
        }

        return RedirectToAction(nameof(Dashboard));
    }
}