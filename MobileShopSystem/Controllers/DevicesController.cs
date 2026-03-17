using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;

namespace MobileShopSystem.Controllers
{
    public class DevicesController : Controller
    {
        private readonly AppDbContext _context;

        public DevicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Devices/Index
        public async Task<IActionResult> Index()
        {
            var devices = await _context.Devices
                .Where(d => d.Status != "تم الحذف نهائي")
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(devices);
        }
        // ===== صفحة الاستعلام (لجميع المستخدمين) =====
        public async Task<IActionResult> Inquiry()
        {
            var devices = await _context.Devices
                .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                .ToListAsync();

            return View(devices);
        }
        // POST: /Devices/Create أو تعديل الجهاز من نفس الفورم
        [HttpPost]
        public async Task<IActionResult> Create(Device device, string submitType)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "البيانات غير صحيحة";
                return RedirectToAction("Index");
            }

            // تعديل الجهاز
            if (submitType == "Update" && device.Id > 0)
            {
                var existingDevice = await _context.Devices.FindAsync(device.Id);
                if (existingDevice != null)
                {
                    existingDevice.Name = device.Name;
                    existingDevice.Serial = device.Serial;
                    existingDevice.Storage = device.Storage;
                    existingDevice.RAM = device.RAM;
                    existingDevice.PurchasePrice = device.PurchasePrice;
                    existingDevice.SalePrice = device.SalePrice;
                    existingDevice.OwnerType = device.OwnerType;
                    existingDevice.OwnerName = device.OwnerName;
                    existingDevice.OwnerPhone = device.OwnerPhone;
                    existingDevice.OwnerID = device.OwnerID;
                    existingDevice.UpdatedAt = DateTime.Now;
                    existingDevice.UpdatedBy = HttpContext.Session.GetString("Username") ?? "System";
                    existingDevice.Status = "تم تعديل";

                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "تم تعديل بيانات الجهاز بنجاح";
                    }
                    catch (DbUpdateException ex)
                    {
                        TempData["Error"] = "حدث خطأ أثناء تعديل البيانات: " + (ex.InnerException?.Message ?? ex.Message);
                    }
                }
                return RedirectToAction("Index");
            }

            // إضافة جهاز جديد
            device.CreatedAt = DateTime.Now;
            device.CreatedBy = HttpContext.Session.GetString("Username") ?? "System";
            device.Status = "سليم";

            // ===== تخزين البيانات الأصلية عند الإضافة =====
            device.OriginalName = device.Name;
            device.OriginalSerial = device.Serial;
            device.OriginalStorage = device.Storage;
            device.OriginalRAM = device.RAM;
            device.OriginalPurchasePrice = device.PurchasePrice;
            device.OriginalSalePrice = device.SalePrice;
            device.OriginalOwnerType = device.OwnerType;
            device.OriginalOwnerName = device.OwnerName;
            device.OriginalOwnerPhone = device.OwnerPhone;
            device.OriginalOwnerID = device.OwnerID;

            try
            {
                _context.Devices.Add(device);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حفظ بيانات الجهاز بنجاح";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = "حدث خطأ أثناء حفظ البيانات: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Index");
        }

        // POST: /Devices/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            device.Status = "تم الحذف";
            device.UpdatedAt = DateTime.Now;
            device.UpdatedBy = HttpContext.Session.GetString("Username") ?? "System";

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الجهاز مؤقتًا";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = "حدث خطأ أثناء حذف الجهاز: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Index");
        }

        // POST: /Devices/DeletePermanent/5
        [HttpPost]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            try
            {
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف الجهاز نهائيًا من قاعدة البيانات";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = "حدث خطأ أثناء الحذف النهائي: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Index");
        }

        // GET: /Devices/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            return View(device);
        }

        // GET: /Devices/Search
        public async Task<IActionResult> Search(string query)
        {
            var devices = string.IsNullOrEmpty(query)
                ? await _context.Devices.Where(d => d.Status != "تم الحذف نهائي").OrderByDescending(d => d.CreatedAt).ToListAsync()
                : await _context.Devices
                    .Where(d => (d.Name.Contains(query) || d.Serial.Contains(query)) && d.Status != "تم الحذف نهائي")
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();

            return View("Index", devices);
        }
    }
}