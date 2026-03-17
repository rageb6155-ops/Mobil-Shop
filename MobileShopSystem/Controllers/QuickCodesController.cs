using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using MobileShopSystem.ViewModels;

namespace MobileShopSystem.Controllers
{
    public class QuickCodesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QuickCodesController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private int? GetCurrentUserId()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return null;

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            return user?.Id;
        }

        // ===== صفحة إدارة الأكواد السريعة =====
        public IActionResult Index()
        {
            return View();
        }

        // ===== الحصول على جميع الأكواد =====
        [HttpGet]
        public async Task<IActionResult> GetAllCodes(string? search)
        {
            var query = _context.QuickCodes
                .Include(q => q.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(q =>
                    q.CodeName.Contains(search) ||
                    q.CodeValue.Contains(search));
            }

            var codes = await query
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new QuickCodeViewModel
                {
                    Id = q.Id,
                    CodeName = q.CodeName,
                    CodeValue = q.CodeValue,
                    UserName = q.User!.Username,
                    CreatedAt = q.CreatedAt
                })
                .ToListAsync();

            return Json(new { success = true, codes = codes });
        }

        // ===== إضافة كود جديد =====
        [HttpPost]
        public async Task<IActionResult> AddCode([FromBody] QuickCodeViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                // التحقق من تكرار الاسم
                var nameExists = await _context.QuickCodes
                    .AnyAsync(q => q.CodeName == model.CodeName);
                if (nameExists)
                    return Json(new { success = false, message = "اسم العملية موجود بالفعل" });

                // التحقق من تكرار الكود
                var valueExists = await _context.QuickCodes
                    .AnyAsync(q => q.CodeValue == model.CodeValue);
                if (valueExists)
                    return Json(new { success = false, message = "الكود المختصر موجود بالفعل" });

                var quickCode = new QuickCode
                {
                    CodeName = model.CodeName,
                    CodeValue = model.CodeValue,
                    UserId = userId.Value,
                    CreatedAt = DateTime.Now
                };

                _context.QuickCodes.Add(quickCode);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تمت الإضافة بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== تعديل كود =====
        [HttpPost]
        public async Task<IActionResult> UpdateCode([FromBody] QuickCodeViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var code = await _context.QuickCodes.FindAsync(model.Id);
                if (code == null)
                    return Json(new { success = false, message = "الكود غير موجود" });

                // التحقق من تكرار الاسم
                var nameExists = await _context.QuickCodes
                    .AnyAsync(q => q.CodeName == model.CodeName && q.Id != model.Id);
                if (nameExists)
                    return Json(new { success = false, message = "اسم العملية موجود بالفعل" });

                // التحقق من تكرار الكود
                var valueExists = await _context.QuickCodes
                    .AnyAsync(q => q.CodeValue == model.CodeValue && q.Id != model.Id);
                if (valueExists)
                    return Json(new { success = false, message = "الكود المختصر موجود بالفعل" });

                code.CodeName = model.CodeName;
                code.CodeValue = model.CodeValue;
                code.UpdatedAt = DateTime.Now;
                code.UpdatedBy = userId;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم التعديل بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== حذف كود =====
        [HttpPost]
        public async Task<IActionResult> DeleteCode(int id)
        {
            try
            {
                var code = await _context.QuickCodes.FindAsync(id);
                if (code == null)
                    return Json(new { success = false, message = "الكود غير موجود" });

                _context.QuickCodes.Remove(code);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم الحذف بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== البحث عن كود =====
        [HttpGet]
        public async Task<IActionResult> SearchCode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Json(new { success = false });

            var code = await _context.QuickCodes
                .FirstOrDefaultAsync(q => q.CodeValue == value);

            if (code == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                codeName = code.CodeName,
                codeValue = code.CodeValue
            });
        }
    }
}