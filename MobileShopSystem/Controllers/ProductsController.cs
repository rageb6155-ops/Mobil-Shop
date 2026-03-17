using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;

namespace MobileShopSystem.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // ===== تحقق من صلاحية Admin =====
        private bool IsAdmin()
            => HttpContext.Session.GetString("IsAdmin") == "True";

        // ===== صفحة إدارة المنتجات (Admin فقط) =====
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var products = await _context.Products
                .Include(p => p.AddedByUser)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // المنتجات التي كميتها أقل من 3
            var lowStock = products.Where(p => p.Quantity < 3).ToList();
            ViewBag.LowStock = lowStock;

            return View(products);
        }

        // ===== حفظ أو تعديل منتج =====
        [HttpPost]
        public async Task<IActionResult> Save(Product model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // التحقق من تكرار الاسم (في حالة الإضافة فقط)
            if (model.Id == 0)
            {
                bool nameExists = await _context.Products.AnyAsync(p => p.Name == model.Name);
                if (nameExists)
                {
                    TempData["ErrorName"] = "اسم المنتج موجود بالفعل";
                    return RedirectToAction("Index");
                }

                bool codeExists = await _context.Products.AnyAsync(p => p.ShortCode == model.ShortCode);
                if (codeExists)
                {
                    TempData["ErrorCode"] = "الكود المختصر موجود بالفعل";
                    return RedirectToAction("Index");
                }

                bool serialExists = await _context.Products.AnyAsync(p => p.SerialNumber == model.SerialNumber);
                if (serialExists)
                {
                    TempData["ErrorSerial"] = "السيريال موجود بالفعل";
                    return RedirectToAction("Index");
                }
            }
            else // في حالة التعديل
            {
                bool nameExists = await _context.Products.AnyAsync(p => p.Name == model.Name && p.Id != model.Id);
                if (nameExists)
                {
                    TempData["ErrorName"] = "اسم المنتج موجود بالفعل";
                    return RedirectToAction("Index");
                }

                bool codeExists = await _context.Products.AnyAsync(p => p.ShortCode == model.ShortCode && p.Id != model.Id);
                if (codeExists)
                {
                    TempData["ErrorCode"] = "الكود المختصر موجود بالفعل";
                    return RedirectToAction("Index");
                }

                bool serialExists = await _context.Products.AnyAsync(p => p.SerialNumber == model.SerialNumber && p.Id != model.Id);
                if (serialExists)
                {
                    TempData["ErrorSerial"] = "السيريال موجود بالفعل";
                    return RedirectToAction("Index");
                }
            }

            if (model.Id == 0)
            {
                var user = await _context.Users
                    .FirstAsync(u => u.Username ==
                        HttpContext.Session.GetString("Username"));

                model.AddedByUserId = user.Id;
                model.CreatedAt = DateTime.Now;

                _context.Products.Add(model);
            }
            else
            {
                var product = await _context.Products.FindAsync(model.Id);
                if (product == null) return NotFound();

                product.Name = model.Name;
                product.ShortCode = model.ShortCode;
                product.SerialNumber = model.SerialNumber;
                product.FactoryPrice = model.FactoryPrice;
                product.SalePrice = model.SalePrice;
                product.PurchasePlace = model.PurchasePlace;
                product.StoragePlace = model.StoragePlace;
                product.Quantity = model.Quantity;
                product.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حفظ المنتج بنجاح";
            return RedirectToAction("Index");
        }

        // ===== تفاصيل المنتج (Admin فقط) =====
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var product = await _context.Products
                .Include(p => p.AddedByUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ===== حذف المنتج (Admin فقط) =====
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (product.Quantity > 0)
            {
                TempData["Error"] = "يوجد مخزون، لا يمكن الحذف";
                return RedirectToAction("Index");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف المنتج بنجاح";
            return RedirectToAction("Index");
        }

        // ===== صفحة عرض المنتجات لجميع المستخدمين (عرض فقط) =====
        public async Task<IActionResult> ViewAll(string search)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.ShortCode.Contains(search) ||
                    p.SerialNumber.Contains(search) ||
                    p.Name.Contains(search));
            }

            var products = await query
                .OrderBy(p => p.Name)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    ShortCode = p.ShortCode,
                    SerialNumber = p.SerialNumber,
                    SalePrice = p.SalePrice
                })
                .ToListAsync();

            return View(products);
        }
    }

    // ===== ViewModel للعرض فقط =====
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ShortCode { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;
        public decimal SalePrice { get; set; }
    }
}