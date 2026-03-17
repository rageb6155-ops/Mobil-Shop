using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;

namespace MobileShopSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ===== صفحة تسجيل الدخول =====
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // البحث عن المستخدم مع التحقق من جميع الشروط
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Username == username &&
                x.Password == password &&
                !x.IsDeleted);  // مش محذوف

            if (user == null)
            {
                ViewBag.Error = "❌ اسم المستخدم أو كلمة المرور غير صحيحة";
                return View();
            }

            // ⭐ التحقق: هل المستخدم قيد الموافقة؟
            if (!user.IsApproved)
            {
                ViewBag.Error = "⏳ حسابك قيد الموافقة، يرجى الانتظار حتى توافق الإدارة";
                return View();
            }

            // ⭐ التحقق: هل المستخدم محظور؟
            if (user.IsBlocked)
            {
                ViewBag.Error = "🚫 حسابك محظور، يرجى التواصل مع الإدارة";
                return View();
            }

            // ⭐ التحقق: هل المستخدم معلق؟ (الخاصية الجديدة)
            if (user.IsSuspended)
            {
                // تخزين معرف المستخدم المعلق في الجلسة
                HttpContext.Session.SetInt32("SuspendedUserId", user.Id);
                // توجيه المستخدم إلى صفحة التعليق
                return RedirectToAction("Suspended");
            }

            // ✅ كل شيء تمام، تسجيل الدخول
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            user.IsOnline = true;
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // ===== صفحة الحساب المعلق =====
        [HttpGet]
        public async Task<IActionResult> Suspended()
        {
            var suspendedUserId = HttpContext.Session.GetInt32("SuspendedUserId");

            if (!suspendedUserId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(suspendedUserId.Value);

            // التحقق: لو المستخدم مش موجود أو مش معلق، نمسح الجلسة ونوجه للدخول
            if (user == null || !user.IsSuspended)
            {
                HttpContext.Session.Remove("SuspendedUserId");
                return RedirectToAction("Login");
            }

            // تمرير رسالة التعليق للـ View
            ViewBag.SuspensionMessage = user.SuspensionMessage ?? "حسابك معلق مؤقتاً. يرجى التواصل مع الإدارة.";
            return View();
        }

        // ===== تأكيد الخروج من صفحة التعليق =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmLogoutFromSuspension()
        {
            var suspendedUserId = HttpContext.Session.GetInt32("SuspendedUserId");

            if (suspendedUserId.HasValue)
            {
                var user = await _context.Users.FindAsync(suspendedUserId.Value);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastLogout = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ===== تسجيل الخروج العادي =====
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var username = HttpContext.Session.GetString("Username");

            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastLogout = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            else if (username != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastLogout = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ===== صفحة التسجيل =====
        public IActionResult Register()
        {
            ModelState.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            string username,
            string password,
            string confirmPassword,
            string phone)
        {
            // التحقق من تطابق كلمة السر
            if (password != confirmPassword)
            {
                ViewBag.Error = "❌ كلمتا السر غير متطابقتين";
                return View();
            }

            // التحقق من رقم الهاتف
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^01\d{9}$"))
            {
                ViewBag.Error = "❌ رقم الهاتف يجب أن يبدأ بـ 01 ويتكون من 11 رقم";
                return View();
            }

            // التحقق من وجود اسم المستخدم أو رقم الهاتف مسبقاً
            bool exists = await _context.Users.AnyAsync(u =>
                (u.Username == username || u.Phone == phone) &&
                !u.IsDeleted);

            if (exists)
            {
                ViewBag.Error = "❌ اسم المستخدم أو رقم الهاتف موجود مسبقاً";
                return View();
            }

            // إنشاء المستخدم الجديد
            var user = new User
            {
                Username = username,
                Password = password,
                Phone = phone,
                IsApproved = false,      // يحتاج موافقة
                IsBlocked = false,
                IsAdmin = false,
                IsOnline = false,
                CreatedDate = DateTime.Now,
                IsDeleted = false,
                IsSuspended = false,      // جديد: غير معلق
                SuspensionMessage = null  // جديد: لا يوجد رسالة
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            ViewBag.Success = "✅ تم إنشاء الحساب بنجاح، بانتظار موافقة الإدارة";
            ModelState.Clear();
            return View();
        }

        // ===== دوال مساعدة (اختياري) =====
        private async Task<User?> GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                return await _context.Users.FindAsync(userId.Value);
            }

            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                return await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
            }

            return null;
        }
    }
}