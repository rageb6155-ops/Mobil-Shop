using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using MobileShopSystem.Services;
using System.Text.Json;

namespace MobileShopSystem.Controllers
{
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly WhatsAppService _whatsAppService;
        private readonly ILogger<SettingsController> _logger;
        private readonly IConfiguration _configuration;

        public SettingsController(
            AppDbContext context,
            WhatsAppService whatsAppService,
            ILogger<SettingsController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _logger = logger;
            _configuration = configuration;
        }

        // شاشة الإعدادات الرئيسية (قائمة المستخدمين)
        public async Task<IActionResult> Index()
        {
            // إرسال قائمة كاملة من المستخدمين للـ View
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // ========== دوال الواتساب الجديدة ==========

        // الحصول على جميع المستخدمين للرسائل الجماعية
        [HttpGet]
        public async Task<IActionResult> GetAllUsersForBulk()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.Phone))
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Phone,
                        u.Password,
                        u.IsAdmin,
                        u.IsOnline,
                        u.IsBlocked,
                        u.IsApproved,
                        LastLogin = u.LastLogin.HasValue ? u.LastLogin.Value.ToString("yyyy-MM-dd HH:mm") : null,
                        LastLogout = u.LastLogout.HasValue ? u.LastLogout.Value.ToString("yyyy-MM-dd HH:mm") : null,
                        Role = u.IsAdmin ? "أدمن" : "مستخدم",
                        Status = u.IsBlocked ? "محظور" : (u.IsOnline ? "متصل" : "غير متصل"),
                        StatusText = u.IsBlocked ? "محظور" : (u.IsOnline ? "متصل حالياً" : "غير متصل")
                    })
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                return Json(new { success = true, users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUsersForBulk");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // إرسال رسالة واتساب لمستخدم واحد
        [HttpPost]
        public async Task<IActionResult> SendWhatsAppMessage([FromBody] SendUserWhatsAppViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                    return Json(new { success = false, message = "المستخدم غير موجود" });

                if (string.IsNullOrEmpty(user.Phone))
                    return Json(new { success = false, message = "رقم الهاتف غير متوفر لهذا المستخدم" });

                // الحصول على رابط الموقع من الإعدادات
                var siteUrl = _configuration["AppSettings:SiteUrl"] ?? "https://mobileshop.com";

                WhatsAppResponse response;
                string messageType = "";
                string userStatus = user.IsBlocked ? "محظور" : (user.IsOnline ? "متصل حالياً" : "غير متصل");
                string lastLoginText = user.LastLogin.HasValue ? user.LastLogin.Value.ToString("yyyy/MM/dd HH:mm") : "لم يسجل الدخول بعد";
                string lastLogoutText = user.LastLogout.HasValue ? user.LastLogout.Value.ToString("yyyy/MM/dd HH:mm") : "لم يسجل خروج";

                switch (model.MessageType?.ToLower())
                {
                    case "welcome":
                        var welcomeMessage = $" *مرحباً بك في نظام Mobile Shop*\n\n";
                        welcomeMessage += $"عزيزي {user.Username}،\n";
                        welcomeMessage += $"تم إنشاء حسابك بنجاح في النظام.\n\n";
                        welcomeMessage += $"━━━━━━━━━━━━━━━━\n";
                        welcomeMessage += $" *بيانات الدخول:*\n";
                        welcomeMessage += $" *اسم المستخدم:* {user.Username}\n";
                        welcomeMessage += $" *كلمة المرور:* {user.Password}\n";
                        welcomeMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        welcomeMessage += $"━━━━━━━━━━━━━━━━\n";
                        welcomeMessage += $" *حالتك:* {userStatus}\n";
                        welcomeMessage += $" *الصلاحية:* {(user.IsAdmin ? "أدمن" : "مستخدم")}\n";
                        welcomeMessage += $"━━━━━━━━━━━━━━━━\n";
                        welcomeMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        welcomeMessage += $" *للتواصل :* 01064211484\n";
                        welcomeMessage += $" *العنوان:*   --------، ، \n";
                        welcomeMessage += $"━━━━━━━━━━━━━━━━\n";
                        welcomeMessage += $" برجاء عدم الافصاح عن بينات حسابك لعدم الوقع في عقوبات  ";
                        response = await _whatsAppService.SendLongMessage(user.Phone, welcomeMessage);
                        messageType = "ترحيب";
                        break;

                    case "promote":
                        var promoteMessage = $" *تهانينا! تم ترقيتك إلى أدمن*\n\n";
                        promoteMessage += $"عزيزي {user.Username}،\n";
                        promoteMessage += $"تم ترقية حسابك إلى مسؤول (أدمن) في النظام.\n\n";
                        promoteMessage += $"━━━━━━━━━━━━━━━━\n";
                        promoteMessage += $" *بيانات حسابك:*\n";
                        promoteMessage += $" *اسم المستخدم:* {user.Username}\n";
                        promoteMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        promoteMessage += $" *الحالة:* {userStatus}\n";
                        promoteMessage += $" *الصلاحية الجديدة:* أدمن\n";
                        promoteMessage += $"━━━━━━━━━━━━━━━━\n";
                        promoteMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        promoteMessage += $" للاستفسار: 01064211484\n";
                        promoteMessage += $"-------------------\n";
                        promoteMessage += $"━━━━━━━━━━━━━━━━\n";
                        promoteMessage += $" مع أطيب التمنيات";
                        response = await _whatsAppService.SendLongMessage(user.Phone, promoteMessage);
                        messageType = "ترقية";
                        break;

                    case "demote":
                        var demoteMessage = $" *تحديث صلاحيات الحساب*\n\n";
                        demoteMessage += $"عزيزي {user.Username}،\n";
                        demoteMessage += $"تم تعديل صلاحيات حسابك إلى مستخدم عادي.\n\n";
                        demoteMessage += $"━━━━━━━━━━━━━━━━\n";
                        demoteMessage += $" *بيانات حسابك:*\n";
                        demoteMessage += $" *اسم المستخدم:* {user.Username}\n";
                        demoteMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        demoteMessage += $" *الحالة:* {userStatus}\n";
                        demoteMessage += $" *الصلاحية الجديدة:* مستخدم\n";
                        demoteMessage += $"━━━━━━━━━━━━━━━━\n";
                        demoteMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        demoteMessage += $" للاستفسار: 01064211484\n";
                        demoteMessage += $"━━━━━━━━━━━━━━━━";
                        response = await _whatsAppService.SendLongMessage(user.Phone, demoteMessage);
                        messageType = "تعديل صلاحيات";
                        break;

                    case "block":
                        var blockMessage = $" *إشعار حظر الحساب*\n\n";
                        blockMessage += $"عزيزي {user.Username}،\n";
                        blockMessage += $"تم حظر حسابك في النظام مؤقتاً.\n";
                        blockMessage += $"يرجى التواصل مع الإدارة للمزيد من المعلومات.\n\n";
                        blockMessage += $"━━━━━━━━━━━━━━━━\n";
                        blockMessage += $" *بيانات حسابك:*\n";
                        blockMessage += $" *اسم المستخدم:* {user.Username}\n";
                        blockMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        blockMessage += $" *الحالة الحالية:* محظور\n";
                        blockMessage += $" *الصلاحية:* {(user.IsAdmin ? "أدمن" : "مستخدم")}\n";
                        blockMessage += $"━━━━━━━━━━━━━━━━\n";
                        blockMessage += $" *للتواصل مع الإدارة:* 01064211484\n";
                        blockMessage += $"━━━━━━━━━━━━━━━━";
                        response = await _whatsAppService.SendLongMessage(user.Phone, blockMessage);
                        messageType = "حظر";
                        break;

                    case "unblock":
                        var unblockMessage = $" *إشعار إلغاء الحظر*\n\n";
                        unblockMessage += $"عزيزي {user.Username}،\n";
                        unblockMessage += $"تم إلغاء حظر حسابك في النظام.\n";
                        unblockMessage += $"يمكنك الآن الدخول إلى حسابك كالمعتاد.\n\n";
                        unblockMessage += $"━━━━━━━━━━━━━━━━\n";
                        unblockMessage += $" *بيانات حسابك:*\n";
                        unblockMessage += $" *اسم المستخدم:* {user.Username}\n";
                        unblockMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        unblockMessage += $" *الحالة الحالية:* نشط\n";
                        unblockMessage += $" *الصلاحية:* {(user.IsAdmin ? "أدمن" : "مستخدم")}\n";
                        unblockMessage += $"━━━━━━━━━━━━━━━━\n";
                        unblockMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        unblockMessage += $" للاستفسار: 01064211484\n";
                        unblockMessage += $"━━━━━━━━━━━━━━━━";
                        response = await _whatsAppService.SendLongMessage(user.Phone, unblockMessage);
                        messageType = "إلغاء حظر";
                        break;

                    case "approve":
                        var approveMessage = $" *تم قبول طلب التسجيل*\n\n";
                        approveMessage += $"عزيزي {user.Username}،\n";
                        approveMessage += $"تمت الموافقة على طلب تسجيلك في النظام.\n\n";
                        approveMessage += $"━━━━━━━━━━━━━━━━\n";
                        approveMessage += $" *بيانات الدخول:*\n";
                        approveMessage += $" *اسم المستخدم:* {user.Username}\n";
                        approveMessage += $" *كلمة المرور:* {user.Password}\n";
                        approveMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        approveMessage += $"━━━━━━━━━━━━━━━━\n";
                        approveMessage += $" *حالتك:* {userStatus}\n";
                        approveMessage += $" *الصلاحية:* {(user.IsAdmin ? "أدمن" : "مستخدم")}\n";
                        approveMessage += $"━━━━━━━━━━━━━━━━\n";
                        approveMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        approveMessage += $" *للتواصل والاستفسار:* 01064211484\n";
                        approveMessage += $" *العنوان:* ٥ شارع المختار، ميامي، الإسكندرية\n";
                        approveMessage += $"━━━━━━━━━━━━━━━━\n";
                        approveMessage += $" مرحباً بك في فريقنا";
                        response = await _whatsAppService.SendLongMessage(user.Phone, approveMessage);
                        messageType = "موافقة";
                        break;

                    case "login":
                        var loginMessage = $" *تم تسجيل الدخول إلى حسابك*\n\n";
                        loginMessage += $"عزيزي {user.Username}،\n";
                        loginMessage += $"تم تسجيل دخولك إلى النظام بنجاح.\n\n";
                        loginMessage += $"━━━━━━━━━━━━━━━━\n";
                        loginMessage += $" *معلومات الجلسة:*\n";
                        loginMessage += $" *المستخدم:* {user.Username}\n";
                        loginMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        loginMessage += $" *وقت الدخول:* {DateTime.Now:yyyy/MM/dd HH:mm}\n";
                        loginMessage += $" *الحالة:* متصل حالياً\n";
                        loginMessage += $"━━━━━━━━━━━━━━━━\n";
                        loginMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        loginMessage += $" للاستفسار: 01064211484\n";
                        loginMessage += $"━━━━━━━━━━━━━━━━";
                        response = await _whatsAppService.SendLongMessage(user.Phone, loginMessage);
                        messageType = "تسجيل دخول";
                        break;

                    case "logout":
                        var logoutMessage = $" *تم تسجيل الخروج من حسابك*\n\n";
                        logoutMessage += $"عزيزي {user.Username}،\n";
                        logoutMessage += $"تم تسجيل خروجك من النظام بنجاح.\n\n";
                        logoutMessage += $"━━━━━━━━━━━━━━━━\n";
                        logoutMessage += $" *معلومات الجلسة:*\n";
                        logoutMessage += $" *المستخدم:* {user.Username}\n";
                        logoutMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        logoutMessage += $" *وقت الخروج:* {DateTime.Now:yyyy/MM/dd HH:mm}\n";
                        logoutMessage += $" *آخر دخول:* {lastLoginText}\n";
                        logoutMessage += $" *الحالة:* غير متصل\n";
                        logoutMessage += $"━━━━━━━━━━━━━━━━\n";
                        logoutMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        logoutMessage += $" للاستفسار: 01064211484\n";
                        logoutMessage += $"━━━━━━━━━━━━━━━━\n";
                        logoutMessage += $" نتمنى لك يوماً سعيداً";
                        response = await _whatsAppService.SendLongMessage(user.Phone, logoutMessage);
                        messageType = "تسجيل خروج";
                        break;

                    case "reminder":
                        var reminderMessage = $" *تذكير*\n\n";
                        reminderMessage += $"عزيزي {user.Username}،\n";
                        reminderMessage += $"نذكرك بتسجيل الدخول إلى النظام ومتابعة مهامك.\n\n";
                        reminderMessage += $"━━━━━━━━━━━━━━━━\n";
                        reminderMessage += $" *بيانات حسابك:*\n";
                        reminderMessage += $" *اسم المستخدم:* {user.Username}\n";
                        reminderMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        reminderMessage += $" *آخر دخول:* {lastLoginText}\n";
                        reminderMessage += $" *الحالة:* {userStatus}\n";
                        reminderMessage += $"━━━━━━━━━━━━━━━━\n";
                        reminderMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        reminderMessage += $" للاستفسار: 01064211484\n";
                        reminderMessage += $"━━━━━━━━━━━━━━━━";
                        response = await _whatsAppService.SendLongMessage(user.Phone, reminderMessage);
                        messageType = "تذكير";
                        break;

                    default:
                        var customMessage = $" *رسالة من النظام*\n\n";
                        customMessage += $"عزيزي {user.Username}،\n\n";
                        customMessage += model.CustomMessage + "\n\n";
                        customMessage += $"━━━━━━━━━━━━━━━━\n";
                        customMessage += $" *بيانات حسابك:*\n";
                        customMessage += $" *اسم المستخدم:* {user.Username}\n";
                        customMessage += $" *رقم الهاتف:* {user.Phone}\n";
                        customMessage += $" *الحالة:* {userStatus}\n";
                        customMessage += $" *الصلاحية:* {(user.IsAdmin ? "أدمن" : "مستخدم")}\n";
                        customMessage += $"━━━━━━━━━━━━━━━━\n";
                        customMessage += $" *رابط الدخول:*\n{siteUrl}\n\n";
                        customMessage += $" للاستفسار: 01064211484\n";
                        customMessage += $"━━━━━━━━━━━━━━━━";
                        response = await _whatsAppService.SendLongMessage(user.Phone, customMessage);
                        messageType = "مخصص";
                        break;
                }

                return Json(new
                {
                    success = response.Success,
                    message = response.Success ? "✅ تم إرسال الرسالة" : "❌ فشل الإرسال: " + response.Error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendWhatsAppMessage");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // إرسال رسالة جماعية لمستخدمين محددين
        [HttpPost]
        public async Task<IActionResult> SendBulkWhatsAppMessage([FromBody] SendBulkUserWhatsAppViewModel model)
        {
            try
            {
                var results = new List<UserBulkWhatsAppResult>();
                int successCount = 0;
                int failCount = 0;
                var siteUrl = _configuration["AppSettings:SiteUrl"] ?? "https://mobileshop.com";

                foreach (var userId in model.UserIds)
                {
                    try
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user == null || string.IsNullOrEmpty(user.Phone))
                        {
                            failCount++;
                            results.Add(new UserBulkWhatsAppResult
                            {
                                UserId = userId,
                                Success = false,
                                Error = "المستخدم غير موجود أو رقم الهاتف غير متوفر"
                            });
                            continue;
                        }

                        string userStatus = user.IsBlocked ? "محظور" : (user.IsOnline ? "متصل حالياً" : "غير متصل");
                        string lastLoginText = user.LastLogin.HasValue ? user.LastLogin.Value.ToString("yyyy/MM/dd HH:mm") : "لم يسجل الدخول بعد";
                        string lastLogoutText = user.LastLogout.HasValue ? user.LastLogout.Value.ToString("yyyy/MM/dd HH:mm") : "لم يسجل خروج";

                        // تخصيص الرسالة للمستخدم
                        string personalizedMessage = model.Message
                            .Replace("{اسم المستخدم}", user.Username)
                            .Replace("{كلمة المرور}", user.Password)
                            .Replace("{رقم الهاتف}", user.Phone)
                            .Replace("{الصلاحية}", user.IsAdmin ? "أدمن" : "مستخدم")
                            .Replace("{الحالة}", userStatus)
                            .Replace("{آخر دخول}", lastLoginText)
                            .Replace("{آخر خروج}", lastLogoutText)
                            .Replace("{رابط الموقع}", siteUrl);

                        var response = await _whatsAppService.SendLongMessage(user.Phone, personalizedMessage);

                        if (response.Success)
                        {
                            successCount++;
                            results.Add(new UserBulkWhatsAppResult
                            {
                                UserId = userId,
                                Username = user.Username,
                                Phone = user.Phone,
                                Success = true,
                                MessageId = response.MessageId
                            });
                        }
                        else
                        {
                            failCount++;
                            results.Add(new UserBulkWhatsAppResult
                            {
                                UserId = userId,
                                Username = user.Username,
                                Phone = user.Phone,
                                Success = false,
                                Error = response.Error
                            });
                        }

                        // انتظار نصف ثانية بين كل رسالة
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        results.Add(new UserBulkWhatsAppResult
                        {
                            UserId = userId,
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"✅ تم إرسال {successCount} رسالة بنجاح، فشل {failCount} رسالة",
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendBulkWhatsAppMessage");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== دوال المستخدمين الموجودة ==========

        // فتح نموذج تعديل مستخدم
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
                return NotFound();

            // تعديل البيانات المسموح بها فقط
            user.Username = model.Username;
            user.Phone = model.Phone;
            user.IsAdmin = model.IsAdmin;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // حذف مستخدم
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // حظر مستخدم
        public async Task<IActionResult> Block(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // فك الحظر
        public async Task<IActionResult> Unblock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = false;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ترقية مستخدم لأدمن
        public async Task<IActionResult> Promote(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsAdmin = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // تنزيل مستخدم من أدمن
        public async Task<IActionResult> Demote(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsAdmin = false;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // الموافقة على مستخدم جديد
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsApproved = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // عرض تفاصيل المستخدم
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }
    }

    // ========== ViewModels جديدة ==========
    public class SendUserWhatsAppViewModel
    {
        public int UserId { get; set; }
        public string MessageType { get; set; }
        public string CustomMessage { get; set; }
    }

    public class SendBulkUserWhatsAppViewModel
    {
        public List<int> UserIds { get; set; }
        public string Message { get; set; }
    }

    public class UserBulkWhatsAppResult
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Phone { get; set; }
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
    }
}