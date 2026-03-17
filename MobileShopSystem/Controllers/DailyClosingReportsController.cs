using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;

namespace MobileShopSystem.Controllers
{
    public class DailyClosingReportsController : Controller
    {
        private readonly AppDbContext _context;

        public DailyClosingReportsController(AppDbContext context)
        {
            _context = context;
        }

        // ===== صفحة عرض كل التقارير مع البحث بالتاريخ =====
        public async Task<IActionResult> Index(DateTime? date)
        {
            var query = _context.DailyClosings
                .Include(d => d.Machines)
                .OrderByDescending(d => d.ClosingDate)
                .AsQueryable();

            if (date.HasValue)
            {
                query = query.Where(d => d.ClosingDate.Date == date.Value.Date);
            }

            var closings = await query.ToListAsync();
            return View(closings);
        }

        // ===== تعديل تقرير =====
        public async Task<IActionResult> Edit(int id)
        {
            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (closing == null) return NotFound();

            return View(closing);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DailyClosing model)
        {
            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .FirstOrDefaultAsync(d => d.Id == model.Id);

            if (closing == null) return NotFound();

            closing.CashLeft = model.CashLeft;
            closing.CoinsAmount = model.CoinsAmount;
            closing.UpdatedAt = DateTime.Now;
            closing.UpdatedBy = HttpContext.Session.GetString("Username");
            closing.IsEdited = true;

            // تحديث رصيد الماكينات
            for (int i = 0; i < closing.Machines.Count && i < model.Machines.Count; i++)
            {
                closing.Machines.ElementAt(i).Balance = model.Machines.ElementAt(i).Balance;
                closing.Machines.ElementAt(i).MachineName = model.Machines.ElementAt(i).MachineName;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تعديل التقرير بنجاح";
            return RedirectToAction("Index");
        }

        // ===== حذف تقرير (علم الحذف فقط) =====
        public async Task<IActionResult> MarkDelete(int id)
        {
            var closing = await _context.DailyClosings.FindAsync(id);
            if (closing == null) return NotFound();

            closing.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تظليل التقرير كحُذِف";
            return RedirectToAction("Index");
        }

        // ===== عرض تفاصيل اليوم =====
        public async Task<IActionResult> Details(int id)
        {
            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (closing == null) return NotFound();

            return View(closing);
        }
    }
}