using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // ✅ السطر المهم
using System;

namespace MobileShopSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // التحقق من تسجيل الدخول
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpGet] // ✅ تأكيد إن الطلب GET
        public IActionResult Logout()
        {
            // مسح جميع بيانات الجلسة
            HttpContext.Session.Clear();

            // إعادة التوجيه لشاشة تسجيل الدخول
            return RedirectToAction("Login", "Account");
        }
    }
}
