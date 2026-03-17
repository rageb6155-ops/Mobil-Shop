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

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Username == username &&
                x.Password == password &&
                x.IsApproved &&
                !x.IsBlocked);

            if (user == null)
            {
                ViewBag.Error = "بيانات الدخول غير صحيحة";
                return View();
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            user.IsOnline = true;
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Logout()
        {
            var username = HttpContext.Session.GetString("Username");

            if (username != null)
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

        public IActionResult Register()
        {
            // تفريغ الحقول دائمًا
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
            // 1️⃣ التحقق من تطابق كلمة السر
            if (password != confirmPassword)
            {
                ViewBag.Error = "كلمتا السر غير متطابقتين";
                return View();
            }

            // 2️⃣ التحقق من رقم الهاتف (11 رقم ويبدأ بـ 01)
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^01\d{9}$"))
            {
                ViewBag.Error = "رقم الهاتف يجب أن يبدأ بـ 01 ويتكون من 11 رقم";
                return View();
            }

            // 3️⃣ التحقق من وجود اسم المستخدم أو رقم الهاتف مسبقًا
            bool exists = await _context.Users.AnyAsync(u => u.Username == username || u.Phone == phone);
            if (exists)
            {
                ViewBag.Error = "اسم المستخدم أو رقم الهاتف موجود مسبقًا، يرجى تسجيل الدخول";
                return View();
            }

            // 4️⃣ إنشاء المستخدم الجديد
            var user = new User
            {
                Username = username,
                Password = password,
                Phone = phone,
                IsApproved = false,
                IsBlocked = false,
                IsAdmin = false,
                IsOnline = false,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5️⃣ رسالة نجاح
            ViewBag.Success = "تم إنشاء الحساب بنجاح، بانتظار موافقة الإدارة";
            ModelState.Clear(); // تفريغ الحقول بعد النجاح
            return View();
        }
    }
}
