using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstateWebApp.Models;

namespace SmartReal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Json(new { success = false, message = "البريد الإلكتروني غير موجود" });

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
            if (result.Succeeded)
                return Json(new { success = true });

            return Json(new { success = false, message = "كلمة المرور غير صحيحة" });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return Json(new { success = true });
            }
            return Json(new { success = false, message = string.Join(" ", result.Errors) });
        }

        // التحقق من حالة تسجيل الدخول (Ajax)
        public IActionResult IsLoggedIn()
        {
            return Json(new { isLoggedIn = User.Identity?.IsAuthenticated ?? false });
        }
    }
}