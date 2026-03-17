using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using System.Text.Json;

namespace MobileShopSystem.Controllers
{
    public class DailyClosingController : Controller
    {
        private readonly AppDbContext _context;

        public DailyClosingController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("IsAdmin") == "True";

        // ===== الشاشة الرئيسية =====
        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.ShowAlert = DateTime.Now.Hour == 0;
            return View();
        }

        // ===== حفظ تقفيل الحساب =====
        [HttpPost]
        public async Task<IActionResult> Save(decimal CashLeft, decimal CoinsAmount, List<string> machineNames, List<decimal> balances)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (machineNames == null || balances == null || machineNames.Count != balances.Count)
            {
                TempData["Error"] = "يجب إدخال كل اسم ماكينة مع رصيدها";
                return RedirectToAction("Index");
            }

            var username = HttpContext.Session.GetString("Username") ?? "Unknown";

            var closing = new DailyClosing
            {
                ClosingDate = DateTime.Now,
                CashLeft = CashLeft,
                CoinsAmount = CoinsAmount,
                CreatedBy = username,
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                IsEdited = false
            };

            for (int i = 0; i < machineNames.Count; i++)
            {
                closing.Machines.Add(new DailyClosingMachine
                {
                    MachineName = machineNames[i],
                    Balance = balances[i]
                });
            }

            _context.DailyClosings.Add(closing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ تقفيل الحساب بنجاح";
            return RedirectToAction("Index");
        }

        // ===== التقارير =====
        public async Task<IActionResult> Reports(DateTime? filterDate)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var query = _context.DailyClosings
                .Include(d => d.Machines)
                .OrderByDescending(d => d.ClosingDate)
                .AsQueryable();

            if (filterDate.HasValue)
                query = query.Where(d => d.ClosingDate.Date == filterDate.Value.Date);

            var closings = await query.ToListAsync();
            return View(closings);
        }

        // ===== طباعة الفاتورة (نسخة PDF مؤقتة) =====
        public async Task<IActionResult> Print(int id)
        {
            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (closing == null)
            {
                return NotFound();
            }

            // مؤقتاً نرجع View عادية بدل PDF
            return View(closing);
        }

        // ===== تعديل =====
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (closing == null)
                return NotFound();

            return View(closing);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DailyClosing model, List<int>? machineIds, List<string> machineNames, List<decimal> balances)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .FirstOrDefaultAsync(d => d.Id == model.Id);

            if (closing == null)
                return NotFound();

            // ===== Snapshot قبل التعديل =====
            var snapshot = new DailyClosingSnapshot
            {
                CashLeft = closing.CashLeft,
                CoinsAmount = closing.CoinsAmount,
                Machines = closing.Machines
                    .Select(m => new DailyClosingMachine
                    {
                        MachineName = m.MachineName,
                        Balance = m.Balance
                    }).ToList()
            };

            closing.PreviousDataJson = JsonSerializer.Serialize(snapshot);

            // ===== تحديث البيانات =====
            closing.CashLeft = model.CashLeft;
            closing.CoinsAmount = model.CoinsAmount;
            closing.UpdatedAt = DateTime.Now;
            closing.UpdatedBy = HttpContext.Session.GetString("Username");
            closing.IsEdited = true;

            machineIds ??= new List<int>();

            // ===== حذف الماكينات المحذوفة =====
            var machinesToRemove = closing.Machines
                .Where(m => !machineIds.Contains(m.Id))
                .ToList();

            foreach (var m in machinesToRemove)
                _context.DailyClosingMachines.Remove(m);

            // ===== تحديث أو إضافة =====
            for (int i = 0; i < machineNames.Count; i++)
            {
                if (i < machineIds.Count && machineIds[i] != 0)
                {
                    var machine = closing.Machines.FirstOrDefault(m => m.Id == machineIds[i]);
                    if (machine != null)
                    {
                        machine.MachineName = machineNames[i];
                        machine.Balance = balances[i];
                    }
                }
                else
                {
                    closing.Machines.Add(new DailyClosingMachine
                    {
                        MachineName = machineNames[i],
                        Balance = balances[i]
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث البيانات بنجاح";
            return RedirectToAction("Reports");
        }

        // ===== تظليل =====
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var closing = await _context.DailyClosings.FindAsync(id);
            if (closing == null)
                return NotFound();

            closing.IsDeleted = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تظليل هذا اليوم كتم حذفه";
            return RedirectToAction("Reports");
        }

        // ===== حذف نهائي =====
        [HttpPost]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (closing == null)
                return NotFound();

            _context.DailyClosings.Remove(closing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف اليوم نهائيًا من قاعدة البيانات";
            return RedirectToAction("Reports");
        }

        // ===== تفاصيل =====
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (closing == null)
                return NotFound();

            return View(closing);
        }

        // ===== عرض تقفيل اليوم (لجميع المستخدمين - عرض فقط) =====
        public async Task<IActionResult> Today()
        {
            var today = DateTime.Today;

            var closing = await _context.DailyClosings
                .Include(d => d.Machines)
                .Where(d => d.ClosingDate.Date == today && !d.IsDeleted)
                .OrderByDescending(d => d.UpdatedAt ?? d.ClosingDate)
                .FirstOrDefaultAsync();

            if (closing == null)
            {
                ViewBag.Message = "يرجى الاتصال بالمشرف لتقفيل الحساب";
                return View(null);
            }

            return View(closing);
        }
    }
}