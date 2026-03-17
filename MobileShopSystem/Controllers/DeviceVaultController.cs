using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using System.Text.Json;

namespace MobileShopSystem.Controllers
{
    public class DeviceVaultController : Controller
    {
        private readonly AppDbContext _context;

        public DeviceVaultController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================
        // صفحة الإضافة
        // ==============================
        public IActionResult Index()
        {
            return View();
        }

        // ==============================
        // حفظ (إضافة أو تحديث لو السيريال موجود)
        // ==============================
        [HttpPost]
        public async Task<IActionResult> Save(Device model, string submitType)
        {
            var username = HttpContext.Session.GetString("Username") ?? "System";

            var existing = await _context.Devices
                .FirstOrDefaultAsync(x => x.Serial == model.Serial);

            // لو السيريال موجود → تحديث
            if (existing != null)
            {
                if (submitType == "Update")
                {
                    string oldData = JsonSerializer.Serialize(existing);
                    string newData = JsonSerializer.Serialize(model);

                    existing.Name = model.Name;
                    existing.Storage = model.Storage;
                    existing.RAM = model.RAM;
                    existing.PurchasePrice = model.PurchasePrice;
                    existing.SalePrice = model.SalePrice;
                    existing.OwnerType = model.OwnerType;
                    existing.OwnerName = model.OwnerName;
                    existing.OwnerPhone = model.OwnerPhone;
                    existing.OwnerID = model.OwnerID;
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = username;
                    existing.Status = "تم تعديل";

                    await _context.SaveChangesAsync();

                    TempData["Message"] = "تم تحديث بيانات الجهاز بنجاح";
                    return RedirectToAction("Reports");
                }
                else
                {
                    ViewBag.Warning = $"الجهاز موجود مسبقًا! اسم الجهاز: {existing.Name}, Serial: {existing.Serial}";
                    return View("Index", model);
                }
            }

            // إضافة جديدة
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = username;
            model.Status = "سليم";

            _context.Devices.Add(model);
            await _context.SaveChangesAsync();

            TempData["Message"] = "تم حفظ الجهاز بتاريخ " + DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            return RedirectToAction("Reports");
        }

        // ==============================
        // صفحة التقارير
        // ==============================
        public async Task<IActionResult> Reports(string search)
        {
            var devices = _context.Devices.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                devices = devices.Where(x => x.Name.Contains(search) || x.Serial.Contains(search));
            }

            var result = await devices
                .Where(d => d.Status != "تم الحذف نهائي")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(result);
        }

        // ==============================
        // عرض تفاصيل الجهاز
        // ==============================
        public async Task<IActionResult> Details(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
                return NotFound();

            return View(device);
        }

        // ==============================
        // تعديل GET
        // ==============================
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
                return NotFound();

            return View(device);
        }

        // ==============================
        // تعديل POST
        // ==============================
        [HttpPost]
        public async Task<IActionResult> Edit(Device model)
        {
            var username = HttpContext.Session.GetString("Username") ?? "System";

            var existing = await _context.Devices.FindAsync(model.Id);
            if (existing == null)
                return NotFound();

            existing.Name = model.Name;
            existing.Serial = model.Serial;
            existing.Storage = model.Storage;
            existing.RAM = model.RAM;
            existing.PurchasePrice = model.PurchasePrice;
            existing.SalePrice = model.SalePrice;
            existing.OwnerType = model.OwnerType;
            existing.OwnerName = model.OwnerName;
            existing.OwnerPhone = model.OwnerPhone;
            existing.OwnerID = model.OwnerID;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = username;
            existing.Status = "تم تعديل";

            await _context.SaveChangesAsync();

            TempData["Message"] = "تم تعديل الجهاز بنجاح";
            return RedirectToAction("Reports");
        }

        // ==============================
        // حذف جزئي
        // ==============================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                device.Status = "تم الحذف";
                device.UpdatedAt = DateTime.Now;
                device.UpdatedBy = HttpContext.Session.GetString("Username") ?? "System";
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "تم حذف الجهاز مؤقتًا";
            return RedirectToAction("Reports");
        }

        // ==============================
        // حذف نهائي
        // ==============================
        [HttpPost]
        public async Task<IActionResult> HardDelete(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "تم حذف الجهاز نهائيًا من قاعدة البيانات";
            return RedirectToAction("Reports");
        }

        // ==============================
        // طباعة الجهاز
        // ==============================
        public async Task<IActionResult> Print(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
                return NotFound();

            return View(device);
        }
    }
}