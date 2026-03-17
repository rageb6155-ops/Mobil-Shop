using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using MobileShopSystem.ViewModels;
using System.Text.Json;

namespace MobileShopSystem.Controllers
{
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SalesController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
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

        private string GetCurrentUsername()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "Unknown";
        }

        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("IsAdmin") == "True";
        }

        // ===== صفحة البيع الرئيسية =====
        public IActionResult Index()
        {
            ViewBag.Username = GetCurrentUsername();
            return View();
        }

        // ===== البحث عن منتج أو جهاز بالسيريال =====
        [HttpGet]
        public async Task<IActionResult> SearchItem(string serial)
        {
            if (string.IsNullOrEmpty(serial))
                return Json(new { success = false, message = "الرجاء إدخال سيريال" });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.SerialNumber == serial);

            if (product != null)
            {
                if (product.Quantity <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "هذا المنتج نفد من المخزون",
                        type = "منتج",
                        name = product.Name,
                        price = product.SalePrice,
                        productId = product.Id,
                        quantity = product.Quantity
                    });
                }

                return Json(new
                {
                    success = true,
                    type = "منتج",
                    name = product.Name,
                    price = product.SalePrice,
                    productId = product.Id,
                    quantity = product.Quantity,
                    warning = product.Quantity <= 3 ? $"تحذير: الكمية المتبقية {product.Quantity} فقط" : null
                });
            }

            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Serial == serial);

            if (device != null)
            {
                return Json(new
                {
                    success = true,
                    type = "جهاز",
                    name = device.Name,
                    price = device.SalePrice,
                    deviceId = device.Id,
                    quantity = 1,
                    warning = (string?)null
                });
            }

            return Json(new { success = false, message = "لا يوجد منتج أو جهاز بهذا السيريال" });
        }

        // ===== إضافة عنصر إلى قائمة البيع المؤقتة =====
        [HttpPost]
        public IActionResult AddItem([FromBody] SaleItemViewModel item)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return Json(new { success = false, message = "جلسة غير صالحة" });

            var itemsJson = session.GetString("CurrentSaleItems") ?? "[]";
            var items = JsonSerializer.Deserialize<List<SaleItemViewModel>>(itemsJson) ?? new List<SaleItemViewModel>();

            items.Add(new SaleItemViewModel
            {
                Id = items.Count + 1,
                ItemName = item.ItemName,
                ItemPrice = item.ItemPrice,
                SaleTypeName = item.SaleTypeName,
                ProductId = item.ProductId,
                DeviceId = item.DeviceId
            });

            session.SetString("CurrentSaleItems", JsonSerializer.Serialize(items));

            return Json(new { success = true, items = items });
        }

        // ===== حذف عنصر من قائمة البيع المؤقتة =====
        [HttpPost]
        public IActionResult RemoveItem([FromBody] RemoveItemViewModel model)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return Json(new { success = false, message = "جلسة غير صالحة" });

            var itemsJson = session.GetString("CurrentSaleItems") ?? "[]";
            var items = JsonSerializer.Deserialize<List<SaleItemViewModel>>(itemsJson) ?? new List<SaleItemViewModel>();

            var itemToRemove = items.FirstOrDefault(i => i.Id == model.Id);
            if (itemToRemove != null)
            {
                items.Remove(itemToRemove);
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Id = i + 1;
                }
            }

            session.SetString("CurrentSaleItems", JsonSerializer.Serialize(items));

            return Json(new { success = true, items = items });
        }

        // ===== الحصول على قائمة البيع المؤقتة =====
        [HttpGet]
        public IActionResult GetCurrentItems()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return Json(new { items = new List<SaleItemViewModel>() });

            var itemsJson = session.GetString("CurrentSaleItems") ?? "[]";
            var items = JsonSerializer.Deserialize<List<SaleItemViewModel>>(itemsJson) ?? new List<SaleItemViewModel>();

            return Json(new { items = items });
        }

        // ===== إتمام عملية البيع =====
        [HttpPost]
        public async Task<IActionResult> CompleteSale([FromBody] CompleteSaleViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                    return Json(new { success = false, message = "جلسة غير صالحة" });

                var itemsJson = session.GetString("CurrentSaleItems") ?? "[]";
                var items = JsonSerializer.Deserialize<List<SaleItemViewModel>>(itemsJson) ?? new List<SaleItemViewModel>();

                if (!items.Any())
                    return Json(new { success = false, message = "لا توجد عناصر للبيع" });

                var productItems = items.Where(i => i.ProductId.HasValue).ToList();

                foreach (var item in productItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        return Json(new { success = false, message = $"المنتج {item.ItemName} غير موجود" });

                    if (product.Quantity <= 0)
                        return Json(new { success = false, message = $"المنتج {item.ItemName} نفد من المخزون" });
                }

                decimal totalAmount = items.Sum(i => i.ItemPrice);

                if (model.PaidAmount < totalAmount)
                    return Json(new { success = false, message = "المبلغ المدفوع أقل من المبلغ الكلي" });

                decimal remainingAmount = model.PaidAmount - totalAmount;

                string saleNumber = "SALE-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                var sale = new Sale
                {
                    SaleNumber = saleNumber,
                    UserId = userId.Value,
                    TotalAmount = totalAmount,
                    PaidAmount = model.PaidAmount,
                    RemainingAmount = remainingAmount,
                    SaleDate = DateTime.Now,
                    Notes = model.Notes
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                var warnings = new List<string>();

                foreach (var item in items)
                {
                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        SaleTypeId = item.SaleTypeName == "رصيد" ? 1 : 2,
                        ItemName = item.ItemName,
                        ItemPrice = item.ItemPrice,
                        ProductId = item.ProductId,
                        DeviceId = item.DeviceId
                    };
                    _context.SaleItems.Add(saleItem);

                    if (item.ProductId.HasValue)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.Quantity -= 1;

                            if (product.Quantity <= 3 && product.Quantity > 0)
                            {
                                warnings.Add($"تحذير: المنتج {product.Name} تبقت منه {product.Quantity} قطع فقط");
                            }
                            else if (product.Quantity == 0)
                            {
                                warnings.Add($"تنبيه: المنتج {product.Name} نفد من المخزون");
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                var saleData = new
                {
                    saleNumber = sale.SaleNumber,
                    totalAmount = sale.TotalAmount,
                    paidAmount = sale.PaidAmount,
                    remainingAmount = sale.RemainingAmount,
                    saleDate = sale.SaleDate,
                    items = items.Select(i => new
                    {
                        itemName = i.ItemName,
                        itemPrice = i.ItemPrice,
                        saleTypeName = i.SaleTypeName
                    }).ToList()
                };

                session.Remove("CurrentSaleItems");

                return Json(new
                {
                    success = true,
                    message = "تمت عملية البيع بنجاح",
                    saleNumber = saleNumber,
                    sale = saleData,
                    warnings = warnings
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== الحصول على مبيعات المستخدم الحالي لليوم =====
        [HttpGet]
        public async Task<IActionResult> GetTodaySales()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var sales = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.SaleType)
                .Where(s => s.UserId == userId &&
                            s.SaleDate >= today &&
                            s.SaleDate < tomorrow &&
                            !s.IsDeleted)
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new
                {
                    Id = s.Id,
                    SaleNumber = s.SaleNumber,
                    UserName = s.User != null ? s.User.Username : "Unknown",
                    TotalAmount = s.TotalAmount,
                    PaidAmount = s.PaidAmount,
                    RemainingAmount = s.RemainingAmount,
                    SaleDate = s.SaleDate,
                    IsDeleted = s.IsDeleted,
                    IsModified = s.IsModified,
                    ModifiedBy = s.ModifiedBy.HasValue ? _context.Users.FirstOrDefault(u => u.Id == s.ModifiedBy)!.Username : null,
                    ModifiedAt = s.ModifiedAt,
                    Items = s.SaleItems.Select(i => new
                    {
                        Id = i.Id,
                        ItemName = i.ItemName,
                        ItemPrice = i.ItemPrice,
                        SaleTypeName = i.SaleType != null ? i.SaleType.Name : "",
                        ProductId = i.ProductId,
                        DeviceId = i.DeviceId
                    }).ToList()
                })
                .ToListAsync();

            return Json(new { success = true, sales = sales });
        }

        // ===== الحصول على عملية بيع للتعديل =====
        [HttpGet]
        public async Task<IActionResult> GetSaleForEdit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.SaleType)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && !s.IsDeleted);

            if (sale == null)
                return Json(new { success = false, message = "العملية غير موجودة" });

            // التحقق من عدد التعديلات للمستخدم العادي
            int modificationCount = 0;
            if (!IsAdmin())
            {
                modificationCount = await _context.SaleModificationLogs
                    .CountAsync(l => l.SaleId == sale.Id && l.ModificationType == "تعديل");
            }

            var saleData = new
            {
                sale.Id,
                sale.SaleNumber,
                sale.TotalAmount,
                sale.PaidAmount,
                sale.RemainingAmount,
                sale.SaleDate,
                sale.Notes,
                sale.IsModified,
                modificationCount,
                Items = sale.SaleItems.Select(i => new SaleItemViewModel
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    ItemPrice = i.ItemPrice,
                    SaleTypeName = i.SaleType != null ? i.SaleType.Name : "",
                    ProductId = i.ProductId,
                    DeviceId = i.DeviceId
                }).ToList()
            };

            return Json(new { success = true, sale = saleData, isAdmin = IsAdmin() });
        }

        // ===== تحميل عناصر للتعديل في الجلسة =====
        [HttpPost]
        public IActionResult LoadItemsForEdit([FromBody] LoadItemsViewModel model)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return Json(new { success = false, message = "جلسة غير صالحة" });

            for (int i = 0; i < model.Items.Count; i++)
            {
                model.Items[i].Id = i + 1;
            }

            session.SetString("CurrentSaleItems", JsonSerializer.Serialize(model.Items));

            return Json(new { success = true, items = model.Items });
        }

        // ===== حذف عملية بيع (مع إعادة الكميات) =====
        [HttpPost]
        public async Task<IActionResult> SoftDeleteSale([FromBody] DeleteSaleViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (sale == null)
                    return Json(new { success = false, message = "العملية غير موجودة" });

                if (sale.UserId != userId && !IsAdmin())
                    return Json(new { success = false, message = "لا يمكنك حذف عملية لم تقم بها" });

                // إعادة الكميات للمنتجات
                var productRestored = new List<string>();
                foreach (var item in sale.SaleItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.Quantity += 1;
                            productRestored.Add($"{product.Name} (الكمية الجديدة: {product.Quantity})");
                        }
                    }
                }

                // حفظ البيانات القديمة
                var oldData = JsonSerializer.Serialize(new
                {
                    sale.Id,
                    sale.SaleNumber,
                    sale.TotalAmount,
                    sale.PaidAmount,
                    sale.RemainingAmount,
                    Items = sale.SaleItems.Select(i => new
                    {
                        i.ItemName,
                        i.ItemPrice,
                        SaleTypeName = i.SaleType?.Name ?? "",
                        i.ProductId,
                        i.DeviceId
                    }).ToList()
                });

                sale.IsDeleted = true;
                sale.DeletedBy = userId;
                sale.DeletedAt = DateTime.Now;

                var log = new SaleModificationLog
                {
                    SaleId = sale.Id,
                    ModifiedBy = userId.Value,
                    ModificationType = "حذف",
                    OldData = oldData
                };
                _context.SaleModificationLogs.Add(log);

                await _context.SaveChangesAsync();

                var message = "تم حذف العملية بنجاح";
                if (productRestored.Any())
                {
                    message += " وتم إعادة الكميات التالية: " + string.Join("، ", productRestored);
                }

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== تعديل عملية بيع =====
        [HttpPost]
        public async Task<IActionResult> EditSale([FromBody] EditSaleViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.SaleType)
                    .FirstOrDefaultAsync(s => s.Id == model.SaleId);

                if (sale == null)
                    return Json(new { success = false, message = "العملية غير موجودة" });

                if (sale.UserId != userId && !IsAdmin())
                    return Json(new { success = false, message = "لا يمكنك تعديل عملية لم تقم بها" });

                // التحقق من عدد التعديلات للمستخدم العادي
                if (!IsAdmin())
                {
                    // حساب عدد التعديلات السابقة على هذه العملية
                    var previousModifications = await _context.SaleModificationLogs
                        .CountAsync(l => l.SaleId == sale.Id && l.ModificationType == "تعديل");

                    if (previousModifications >= 1)
                    {
                        return Json(new { success = false, message = "لا يمكن تعديل العملية أكثر من مرة يرجي الاتصال بالمشرف " });
                    }

                    // التحقق من وجود تغييرات فعلية
                    var oldTotal = sale.TotalAmount;
                    var newTotal = model.Items.Sum(i => i.ItemPrice);
                    var oldPaid = sale.PaidAmount;
                    var newPaid = model.PaidAmount;

                    // مقارنة العناصر
                    var oldItemsList = sale.SaleItems.Select(i => new { i.ItemName, i.ItemPrice }).OrderBy(x => x.ItemName).ToList();
                    var newItemsList = model.Items.Select(i => new { i.ItemName, i.ItemPrice }).OrderBy(x => x.ItemName).ToList();

                    var itemsChanged = !oldItemsList.SequenceEqual(newItemsList);
                    var totalsChanged = oldTotal != newTotal || oldPaid != newPaid;

                    if (!itemsChanged && !totalsChanged)
                    {
                        return Json(new { success = true, message = "لم يتم إجراء أي تغييرات", noChanges = true });
                    }
                }

                // حفظ البيانات الأصلية إذا كان هذا أول تعديل
                if (!sale.IsModified)
                {
                    sale.OriginalTotalAmount = sale.TotalAmount;
                    sale.OriginalPaidAmount = sale.PaidAmount;
                    sale.OriginalRemainingAmount = sale.RemainingAmount;
                }

                // تجهيز بيانات العناصر القديمة للتسجيل
                var oldItemsForLog = sale.SaleItems.Select(i => new SaleItemReportViewModel
                {
                    ItemName = i.ItemName,
                    ItemPrice = i.ItemPrice,
                    SaleTypeName = i.SaleType?.Name ?? ""
                }).ToList();

                // تجهيز بيانات العناصر الجديدة للتسجيل
                var newItemsForLog = model.Items.Select(i => new SaleItemReportViewModel
                {
                    ItemName = i.ItemName,
                    ItemPrice = i.ItemPrice,
                    SaleTypeName = i.SaleTypeName
                }).ToList();

                // حذف العناصر القديمة
                _context.SaleItems.RemoveRange(sale.SaleItems);

                // إضافة العناصر الجديدة
                foreach (var item in model.Items)
                {
                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        SaleTypeId = item.SaleTypeName == "رصيد" ? 1 : 2,
                        ItemName = item.ItemName,
                        ItemPrice = item.ItemPrice,
                        ProductId = item.ProductId,
                        DeviceId = item.DeviceId
                    };
                    _context.SaleItems.Add(saleItem);
                }

                decimal totalAmount = model.Items.Sum(i => i.ItemPrice);
                decimal oldTotalAmount = sale.TotalAmount;
                decimal oldPaidAmount = sale.PaidAmount;

                sale.TotalAmount = totalAmount;
                sale.PaidAmount = model.PaidAmount;
                sale.RemainingAmount = model.PaidAmount - totalAmount;
                sale.IsModified = true;
                sale.ModifiedBy = userId;
                sale.ModifiedAt = DateTime.Now;
                sale.Notes = model.Notes;

                // تسجيل عملية التعديل مع الأعمدة الجديدة
                var log = new SaleModificationLog
                {
                    SaleId = sale.Id,
                    ModifiedBy = userId.Value,
                    ModificationType = "تعديل",
                    OldTotalAmount = oldTotalAmount,
                    NewTotalAmount = totalAmount,
                    OldPaidAmount = oldPaidAmount,
                    NewPaidAmount = model.PaidAmount,
                    OldItemsJson = JsonSerializer.Serialize(oldItemsForLog),
                    NewItemsJson = JsonSerializer.Serialize(newItemsForLog)
                };
                _context.SaleModificationLogs.Add(log);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم تعديل العملية بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== مسح الجلسة =====
        [HttpPost]
        public IActionResult ClearSession()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.Remove("CurrentSaleItems");
            }
            return Json(new { success = true });
        }

        // ===== تقارير المبيعات اليومية =====
        [HttpGet]
        public async Task<IActionResult> DailyReports(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (!fromDate.HasValue)
                fromDate = DateTime.Today;
            if (!toDate.HasValue)
                toDate = DateTime.Today.AddDays(1).AddSeconds(-1);

            ViewBag.FromDate = fromDate.Value;
            ViewBag.ToDate = toDate.Value;

            // جلب جميع المبيعات مع التعديلات
            var sales = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.SaleType)
                .Where(s => s.SaleDate >= fromDate.Value && s.SaleDate <= toDate.Value)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            // تجهيز قائمة النتائج
            var result = new List<MobileShopSystem.ViewModels.SaleReportViewModel>();

            foreach (var sale in sales)
            {
                // جلب التعديلات لهذه العملية
                var modifications = await _context.SaleModificationLogs
                    .Where(l => l.SaleId == sale.Id)
                    .OrderByDescending(l => l.ModifiedAt)
                    .Select(l => new MobileShopSystem.ViewModels.ModificationReportViewModel
                    {
                        ModifiedBy = _context.Users.FirstOrDefault(u => u.Id == l.ModifiedBy) != null ?
                            _context.Users.FirstOrDefault(u => u.Id == l.ModifiedBy)!.Username : "Unknown",
                        ModifiedAt = l.ModifiedAt,
                        ModificationType = l.ModificationType,
                        OldAmount = l.OldTotalAmount,
                        NewAmount = l.NewTotalAmount,
                        OldItems = !string.IsNullOrEmpty(l.OldItemsJson) ?
                            JsonSerializer.Deserialize<List<SaleItemReportViewModel>>(l.OldItemsJson) : new List<SaleItemReportViewModel>(),
                        NewItems = !string.IsNullOrEmpty(l.NewItemsJson) ?
                            JsonSerializer.Deserialize<List<SaleItemReportViewModel>>(l.NewItemsJson) : new List<SaleItemReportViewModel>()
                    }).ToListAsync();

                var reportItem = new MobileShopSystem.ViewModels.SaleReportViewModel
                {
                    Id = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    UserName = sale.User != null ? sale.User.Username : "Unknown",
                    TotalAmount = sale.TotalAmount,
                    PaidAmount = sale.PaidAmount,
                    RemainingAmount = sale.RemainingAmount,
                    SaleDate = sale.SaleDate,
                    IsDeleted = sale.IsDeleted,
                    IsModified = sale.IsModified,
                    MachinesTotal = sale.SaleItems.Where(i => i.SaleTypeId == 1).Sum(i => i.ItemPrice),
                    OriginalTotalAmount = sale.OriginalTotalAmount ?? sale.TotalAmount,
                    OriginalPaidAmount = sale.OriginalPaidAmount ?? sale.PaidAmount,
                    OriginalRemainingAmount = sale.OriginalRemainingAmount ?? sale.RemainingAmount,
                    Items = sale.SaleItems.Select(i => new MobileShopSystem.ViewModels.SaleItemReportViewModel
                    {
                        ItemName = i.ItemName,
                        ItemPrice = i.ItemPrice,
                        SaleTypeName = i.SaleType != null ? i.SaleType.Name : ""
                    }).ToList(),
                    Modifications = modifications
                };

                result.Add(reportItem);
            }

            return View(result);
        }

        // ===== استعادة عملية محذوفة (مع خصم الكميات) =====
        [HttpPost]
        public async Task<IActionResult> RestoreSale([FromBody] RestoreSaleViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (sale == null)
                    return Json(new { success = false, message = "العملية غير موجودة" });

                if (sale.UserId != userId && !IsAdmin())
                    return Json(new { success = false, message = "لا يمكنك استعادة عملية لم تقم بها" });

                // التحقق من توفر الكميات قبل الاستعادة
                var insufficientProducts = new List<string>();
                foreach (var item in sale.SaleItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null)
                        {
                            insufficientProducts.Add($"{item.ItemName} (المنتج غير موجود)");
                        }
                        else if (product.Quantity <= 0)
                        {
                            insufficientProducts.Add($"{product.Name} (الكمية المتاحة: {product.Quantity})");
                        }
                    }
                }

                if (insufficientProducts.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "لا يمكن استعادة العملية بسبب نقص الكميات التالية: " + string.Join("، ", insufficientProducts)
                    });
                }

                // خصم الكميات مرة أخرى
                var productsDeducted = new List<string>();
                foreach (var item in sale.SaleItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.Quantity -= 1;
                            productsDeducted.Add($"{product.Name} (الكمية الجديدة: {product.Quantity})");
                        }
                    }
                }

                sale.IsDeleted = false;
                sale.DeletedBy = null;
                sale.DeletedAt = null;

                await _context.SaveChangesAsync();

                var message = "تم استعادة العملية بنجاح";
                if (productsDeducted.Any())
                {
                    message += " وتم خصم الكميات التالية: " + string.Join("، ", productsDeducted);
                }

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== تفاصيل عملية بيع =====
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.SaleType)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return NotFound();

            var viewModel = new MobileShopSystem.ViewModels.SaleDetailsViewModel
            {
                Id = sale.Id,
                SaleNumber = sale.SaleNumber,
                UserName = sale.User != null ? sale.User.Username : "Unknown",
                TotalAmount = sale.TotalAmount,
                PaidAmount = sale.PaidAmount,
                RemainingAmount = sale.RemainingAmount,
                SaleDate = sale.SaleDate,
                IsDeleted = sale.IsDeleted,
                IsModified = sale.IsModified,
                ModifiedBy = sale.ModifiedBy.HasValue ? _context.Users.FirstOrDefault(u => u.Id == sale.ModifiedBy)!.Username : null,
                ModifiedAt = sale.ModifiedAt,
                Notes = sale.Notes,
                OriginalTotalAmount = sale.OriginalTotalAmount ?? sale.TotalAmount,
                OriginalPaidAmount = sale.OriginalPaidAmount ?? sale.PaidAmount,
                OriginalRemainingAmount = sale.OriginalRemainingAmount ?? sale.RemainingAmount,
                Items = sale.SaleItems.Select(i => new MobileShopSystem.ViewModels.SaleItemDetailsViewModel
                {
                    ItemName = i.ItemName,
                    ItemPrice = i.ItemPrice,
                    SaleTypeName = i.SaleType != null ? i.SaleType.Name : ""
                }).ToList(),
                Modifications = _context.SaleModificationLogs
                    .Where(l => l.SaleId == sale.Id)
                    .OrderByDescending(l => l.ModifiedAt)
                    .Select(l => new MobileShopSystem.ViewModels.ModificationReportViewModel
                    {
                        ModifiedBy = _context.Users.FirstOrDefault(u => u.Id == l.ModifiedBy) != null ?
                            _context.Users.FirstOrDefault(u => u.Id == l.ModifiedBy)!.Username : "Unknown",
                        ModifiedAt = l.ModifiedAt,
                        ModificationType = l.ModificationType,
                        OldAmount = l.OldTotalAmount,
                        NewAmount = l.NewTotalAmount,
                        OldItems = l.OldItemsJson != null ?
                            JsonSerializer.Deserialize<List<SaleItemReportViewModel>>(l.OldItemsJson) : null,
                        NewItems = l.NewItemsJson != null ?
                            JsonSerializer.Deserialize<List<SaleItemReportViewModel>>(l.NewItemsJson) : null
                    }).ToList()
            };

            // محاولة الحصول على العناصر الأصلية إذا كانت موجودة
            var lastModification = await _context.SaleModificationLogs
                .Where(l => l.SaleId == sale.Id && l.ModificationType == "تعديل")
                .OrderByDescending(l => l.ModifiedAt)
                .FirstOrDefaultAsync();

            if (lastModification != null && lastModification.OldItemsJson != null)
            {
                var originalItems = JsonSerializer.Deserialize<List<SaleItemReportViewModel>>(lastModification.OldItemsJson);
                if (originalItems != null)
                {
                    viewModel.OriginalItems = originalItems.Select(i => new SaleItemDetailsViewModel
                    {
                        ItemName = i.ItemName,
                        ItemPrice = i.ItemPrice,
                        SaleTypeName = i.SaleTypeName
                    }).ToList();
                }
            }

            return View(viewModel);
        }
    }
}