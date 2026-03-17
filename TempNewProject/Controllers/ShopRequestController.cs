using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using MobileShopSystem.Services;
using MobileShopSystem.ViewModels;
using System.Text;

namespace MobileShopSystem.Controllers
{
    public class ShopRequestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly WhatsAppService _whatsAppService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ShopRequestController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ShopRequestController(
            AppDbContext context,
            WhatsAppService whatsAppService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ShopRequestController> logger,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
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

        // GET: ShopRequest
        public async Task<IActionResult> Index(DateTime? date = null)
        {
            ViewBag.Username = GetCurrentUsername();
            var targetDate = date ?? DateTime.Today;

            var requests = await _context.ShopRequests
                .Include(r => r.Creator)
                .Where(r => !r.IsDeleted && r.RequestDate.Date == targetDate.Date)
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.RequestDate)
                .Select(r => new ShopRequestViewModel
                {
                    Id = r.Id,
                    ItemName = r.ItemName,
                    ItemType = r.ItemType,
                    Quantity = r.Quantity,
                    UnitPrice = r.UnitPrice,
                    TotalPrice = r.Quantity * (r.UnitPrice ?? 0),
                    Supplier = r.Supplier,
                    PhoneNumber = r.PhoneNumber,
                    Notes = r.Notes,
                    Priority = r.Priority,
                    Status = r.Status,
                    RequestDate = r.RequestDate,
                    DueDate = r.DueDate,
                    CreatedByName = r.Creator != null ? r.Creator.Username : "Unknown",
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    UpdatedByName = r.Updater != null ? r.Updater.Username : null
                })
                .ToListAsync();

            ViewBag.CurrentDate = targetDate.ToString("yyyy-MM-dd");
            ViewBag.TotalRequests = requests.Count;
            ViewBag.TotalEstimatedCost = requests.Sum(r => r.TotalPrice ?? 0);

            return View(requests);
        }

        // GET: ShopRequest/History
        public async Task<IActionResult> History(DateTime? fromDate, DateTime? toDate, string? itemType, string? status, int? priority, string? searchTerm)
        {
            ViewBag.Username = GetCurrentUsername();

            var query = _context.ShopRequests
                .Include(r => r.Creator)
                .Include(r => r.Updater)
                .Where(r => !r.IsDeleted);

            if (fromDate.HasValue)
                query = query.Where(r => r.RequestDate.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(r => r.RequestDate.Date <= toDate.Value.Date);

            if (!string.IsNullOrEmpty(itemType))
                query = query.Where(r => r.ItemType == itemType);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (priority.HasValue)
                query = query.Where(r => r.Priority == priority.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(r =>
                    r.ItemName.Contains(searchTerm) ||
                    r.Supplier.Contains(searchTerm) ||
                    r.Notes.Contains(searchTerm));
            }

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new ShopRequestViewModel
                {
                    Id = r.Id,
                    ItemName = r.ItemName,
                    ItemType = r.ItemType,
                    Quantity = r.Quantity,
                    UnitPrice = r.UnitPrice,
                    TotalPrice = r.Quantity * (r.UnitPrice ?? 0),
                    Supplier = r.Supplier,
                    PhoneNumber = r.PhoneNumber,
                    Notes = r.Notes,
                    Priority = r.Priority,
                    Status = r.Status,
                    RequestDate = r.RequestDate,
                    DueDate = r.DueDate,
                    CreatedByName = r.Creator != null ? r.Creator.Username : "Unknown",
                    UpdatedByName = r.Updater != null ? r.Updater.Username : null,
                    UpdatedAt = r.UpdatedAt,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            ViewBag.ItemTypes = GetItemTypes();
            ViewBag.Statuses = GetStatuses();
            ViewBag.Priorities = GetPriorities();

            var filter = new ShopRequestFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                ItemType = itemType,
                Status = status,
                Priority = priority,
                SearchTerm = searchTerm
            };

            ViewBag.Filter = filter;
            ViewBag.TotalRequests = requests.Count;
            ViewBag.TotalCost = requests.Sum(r => r.TotalPrice ?? 0);

            return View(requests);
        }

        // GET: ShopRequest/Create
        public IActionResult Create()
        {
            ViewBag.ItemTypes = GetItemTypes();
            ViewBag.Priorities = GetPriorities();
            ViewBag.Statuses = GetStatuses();
            return View(new ShopRequestViewModel());
        }

        // POST: ShopRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShopRequestViewModel model)
        {
            try
            {
                // إزالة التحقق من الحقول التي قد تكون null
                ModelState.Remove("ItemName");
                ModelState.Remove("ItemType");
                ModelState.Remove("Quantity");

                var userId = GetCurrentUserId();

                var request = new ShopRequest
                {
                    ItemName = model.ItemName ?? "غير محدد",
                    ItemType = model.ItemType ?? "أخرى",
                    Quantity = model.Quantity > 0 ? model.Quantity : 1,
                    UnitPrice = model.UnitPrice,
                    Supplier = model.Supplier,
                    PhoneNumber = model.PhoneNumber,
                    Notes = model.Notes,
                    Priority = model.Priority,
                    Status = model.Status ?? "معلق",
                    RequestDate = DateTime.Now,
                    DueDate = model.DueDate,
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.ShopRequests.Add(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ تم إضافة الطلب بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة طلب جديد");
                TempData["ErrorMessage"] = "حدث خطأ أثناء إضافة الطلب";
                return View(model);
            }
        }

        // GET: ShopRequest/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var request = await _context.ShopRequests
                    .Include(r => r.Creator)
                    .Include(r => r.Updater)
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "الطلب غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                var model = new ShopRequestViewModel
                {
                    Id = request.Id,
                    ItemName = request.ItemName,
                    ItemType = request.ItemType,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice,
                    Supplier = request.Supplier,
                    PhoneNumber = request.PhoneNumber,
                    Notes = request.Notes,
                    Priority = request.Priority,
                    Status = request.Status,
                    RequestDate = request.RequestDate,
                    DueDate = request.DueDate,
                    CreatedByName = request.Creator?.Username ?? "Unknown",
                    UpdatedAt = request.UpdatedAt,
                    UpdatedByName = request.Updater?.Username
                };

                ViewBag.ItemTypes = GetItemTypes();
                ViewBag.Priorities = GetPriorities();
                ViewBag.Statuses = GetStatuses();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحميل صفحة التعديل للطلب {id}");
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل البيانات";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ShopRequest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ShopRequestViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return NotFound();
                }

                // إزالة التحقق من الحقول التي قد تكون null
                ModelState.Remove("ItemName");
                ModelState.Remove("ItemType");
                ModelState.Remove("Quantity");

                var request = await _context.ShopRequests
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "الطلب غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                var userId = GetCurrentUserId();

                // تحديث البيانات مع الاحتفاظ بالتاريخ الأصلي
                request.ItemName = model.ItemName ?? "غير محدد";
                request.ItemType = model.ItemType ?? "أخرى";
                request.Quantity = model.Quantity > 0 ? model.Quantity : 1;
                request.UnitPrice = model.UnitPrice;
                request.Supplier = model.Supplier;
                request.PhoneNumber = model.PhoneNumber;
                request.Notes = model.Notes;
                request.Priority = model.Priority;
                request.Status = model.Status ?? "معلق";
                request.DueDate = model.DueDate;
                request.UpdatedBy = userId;
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ تم تحديث الطلب بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحديث الطلب {id}");
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث الطلب";
                return View(model);
            }
        }

        // POST: ShopRequest/Delete/5 (نقل إلى المحذوفات)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var request = await _context.ShopRequests.FindAsync(id);
                if (request == null)
                {
                    return Json(new { success = false, message = "الطلب غير موجود" });
                }

                var userId = GetCurrentUserId();

                request.IsDeleted = true;
                request.DeletedBy = userId;
                request.DeletedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم نقل الطلب إلى المحذوفات" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في حذف الطلب {id}");
                return Json(new { success = false, message = "حدث خطأ أثناء الحذف" });
            }
        }

        // POST: ShopRequest/HardDelete/5 (حذف نهائي)
        [HttpPost]
        public async Task<IActionResult> HardDelete(int id)
        {
            try
            {
                var request = await _context.ShopRequests.FindAsync(id);
                if (request == null)
                {
                    return Json(new { success = false, message = "الطلب غير موجود" });
                }

                _context.ShopRequests.Remove(request);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم الحذف النهائي بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في الحذف النهائي للطلب {id}");
                return Json(new { success = false, message = "حدث خطأ أثناء الحذف النهائي" });
            }
        }

        // POST: ShopRequest/Restore/5 (استعادة من المحذوفات)
        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            try
            {
                var request = await _context.ShopRequests
                    .FirstOrDefaultAsync(r => r.Id == id && r.IsDeleted);

                if (request == null)
                {
                    return Json(new { success = false, message = "الطلب غير موجود" });
                }

                request.IsDeleted = false;
                request.DeletedBy = null;
                request.DeletedAt = null;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم استعادة الطلب بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في استعادة الطلب {id}");
                return Json(new { success = false, message = "حدث خطأ أثناء الاستعادة" });
            }
        }

        // GET: ShopRequest/GetDeletedRequests
        [HttpGet]
        public async Task<IActionResult> GetDeletedRequests()
        {
            try
            {
                var requests = await _context.ShopRequests
                    .Include(r => r.Deleter)
                    .Where(r => r.IsDeleted)
                    .OrderByDescending(r => r.DeletedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.ItemName,
                        r.ItemType,
                        r.Quantity,
                        r.UnitPrice,
                        TotalPrice = r.Quantity * (r.UnitPrice ?? 0),
                        r.Supplier,
                        r.PhoneNumber,
                        r.Notes,
                        r.Priority,
                        r.Status,
                        RequestDate = r.RequestDate.ToString("yyyy-MM-dd"),
                        DeletedAt = r.DeletedAt != null ? r.DeletedAt.Value.ToString("yyyy-MM-dd HH:mm") : null,
                        DeletedBy = r.Deleter != null ? r.Deleter.Username : "Unknown"
                    })
                    .ToListAsync();

                return Json(new { success = true, requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل المحذوفات");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: ShopRequest/GetAllUsersForBulk
        [HttpGet]
        public async Task<IActionResult> GetAllUsersForBulk()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Phone))
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Phone,
                        u.IsAdmin,
                        u.IsOnline
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

        // GET: ShopRequest/GetAllPhonesForBulk
        [HttpGet]
        public async Task<IActionResult> GetAllPhonesForBulk()
        {
            try
            {
                var requests = await _context.ShopRequests
                    .Where(r => !r.IsDeleted && !string.IsNullOrEmpty(r.PhoneNumber))
                    .Select(r => new
                    {
                        r.Id,
                        r.ItemName,
                        r.PhoneNumber,
                        r.Supplier,
                        r.Quantity,
                        r.ItemType,
                        r.Priority,
                        r.Status
                    })
                    .OrderBy(r => r.ItemName)
                    .ToListAsync();

                return Json(new { success = true, phones = requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllPhonesForBulk");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: ShopRequest/GetTodayRequests
        [HttpGet]
        public async Task<IActionResult> GetTodayRequests()
        {
            try
            {
                var today = DateTime.Today;
                var requests = await _context.ShopRequests
                    .Where(r => !r.IsDeleted && r.RequestDate.Date == today.Date)
                    .OrderByDescending(r => r.Priority)
                    .Select(r => new
                    {
                        r.Id,
                        r.ItemName,
                        r.ItemType,
                        r.Quantity,
                        r.UnitPrice,
                        TotalPrice = r.Quantity * (r.UnitPrice ?? 0),
                        r.Supplier,
                        r.PhoneNumber,
                        r.Notes,
                        r.Priority,
                        r.Status,
                        RequestDate = r.RequestDate.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                return Json(new { success = true, requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTodayRequests");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: ShopRequest/GetHistoryRequests
        [HttpGet]
        public async Task<IActionResult> GetHistoryRequests(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var query = _context.ShopRequests
                    .Where(r => !r.IsDeleted);

                if (fromDate.HasValue)
                    query = query.Where(r => r.RequestDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(r => r.RequestDate.Date <= toDate.Value.Date);

                var requests = await query
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new
                    {
                        r.Id,
                        r.ItemName,
                        r.ItemType,
                        r.Quantity,
                        UnitPrice = r.UnitPrice ?? 0,
                        r.Supplier,
                        r.PhoneNumber,
                        r.Notes,
                        r.Priority,
                        r.Status,
                        RequestDate = r.RequestDate.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                return Json(new { success = true, requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHistoryRequests");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: ShopRequest/GetDetails/5
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                var request = await _context.ShopRequests
                    .Include(r => r.Creator)
                    .Include(r => r.Updater)
                    .Include(r => r.Deleter)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    return Json(new { success = false, message = "الطلب غير موجود" });
                }

                var result = new
                {
                    request.Id,
                    request.ItemName,
                    request.ItemType,
                    request.Quantity,
                    request.UnitPrice,
                    TotalPrice = request.Quantity * (request.UnitPrice ?? 0),
                    request.Supplier,
                    request.PhoneNumber,
                    request.Notes,
                    request.Priority,
                    request.Status,
                    RequestDate = request.RequestDate.ToString("yyyy-MM-dd HH:mm"),
                    DueDate = request.DueDate?.ToString("yyyy-MM-dd"),
                    CreatedAt = request.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    CreatedByName = request.Creator?.Username ?? "Unknown",
                    UpdatedAt = request.UpdatedAt?.ToString("yyyy-MM-dd HH:mm"),
                    UpdatedByName = request.Updater?.Username,
                    IsDeleted = request.IsDeleted,
                    DeletedAt = request.DeletedAt?.ToString("yyyy-MM-dd HH:mm"),
                    DeletedByName = request.Deleter?.Username
                };

                return Json(new { success = true, request = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحميل تفاصيل الطلب {id}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: ShopRequest/GetDeletedDetails/5
        [HttpGet]
        public async Task<IActionResult> GetDeletedDetails(int id)
        {
            try
            {
                var request = await _context.ShopRequests
                    .Include(r => r.Creator)
                    .Include(r => r.Updater)
                    .Include(r => r.Deleter)
                    .FirstOrDefaultAsync(r => r.Id == id && r.IsDeleted);

                if (request == null)
                {
                    return Json(new { success = false, message = "الطلب غير موجود" });
                }

                var result = new
                {
                    request.Id,
                    request.ItemName,
                    request.ItemType,
                    request.Quantity,
                    request.UnitPrice,
                    TotalPrice = request.Quantity * (request.UnitPrice ?? 0),
                    request.Supplier,
                    request.PhoneNumber,
                    request.Notes,
                    request.Priority,
                    request.Status,
                    RequestDate = request.RequestDate.ToString("yyyy-MM-dd HH:mm"),
                    DueDate = request.DueDate?.ToString("yyyy-MM-dd"),
                    CreatedAt = request.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    CreatedByName = request.Creator?.Username ?? "Unknown",
                    UpdatedAt = request.UpdatedAt?.ToString("yyyy-MM-dd HH:mm"),
                    UpdatedByName = request.Updater?.Username,
                    DeletedAt = request.DeletedAt?.ToString("yyyy-MM-dd HH:mm"),
                    DeletedByName = request.Deleter?.Username
                };

                return Json(new { success = true, request = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحميل تفاصيل الطلب المحذوف {id}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: ShopRequest/GetTodaySummary
        [HttpGet]
        public async Task<IActionResult> GetTodaySummary()
        {
            try
            {
                var today = DateTime.Today;
                var requests = await _context.ShopRequests
                    .Where(r => !r.IsDeleted && r.RequestDate.Date == today.Date)
                    .ToListAsync();

                var summary = new
                {
                    TotalCount = requests.Count,
                    TotalCost = requests.Sum(r => r.Quantity * (r.UnitPrice ?? 0)),
                    ByPriority = new
                    {
                        Urgent = requests.Count(r => r.Priority == 3),
                        Important = requests.Count(r => r.Priority == 2),
                        Normal = requests.Count(r => r.Priority == 1)
                    },
                    ByType = requests.GroupBy(r => r.ItemType)
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .ToList()
                };

                return Json(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل ملخص اليوم");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: ShopRequest/SendBulkWhatsApp
        [HttpPost]
        public async Task<IActionResult> SendBulkWhatsApp([FromBody] SendBulkShopRequestWhatsAppViewModel model)
        {
            try
            {
                var results = new List<ShopRequestBulkWhatsAppResult>();
                int successCount = 0;
                int failCount = 0;

                foreach (var id in model.RequestIds)
                {
                    try
                    {
                        var request = await _context.ShopRequests.FindAsync(id);
                        if (request == null || string.IsNullOrEmpty(request.PhoneNumber))
                        {
                            failCount++;
                            results.Add(new ShopRequestBulkWhatsAppResult
                            {
                                RequestId = id,
                                Success = false,
                                Error = "الطلب غير موجود أو رقم الهاتف غير متوفر"
                            });
                            continue;
                        }

                        var response = await _whatsAppService.SendLongMessage(request.PhoneNumber, model.Message);

                        if (response.Success)
                        {
                            successCount++;
                            results.Add(new ShopRequestBulkWhatsAppResult
                            {
                                RequestId = id,
                                ItemName = request.ItemName,
                                PhoneNumber = request.PhoneNumber,
                                Success = true,
                                MessageId = response.MessageId
                            });
                        }
                        else
                        {
                            failCount++;
                            results.Add(new ShopRequestBulkWhatsAppResult
                            {
                                RequestId = id,
                                ItemName = request.ItemName,
                                PhoneNumber = request.PhoneNumber,
                                Success = false,
                                Error = response.Error
                            });
                        }

                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        results.Add(new ShopRequestBulkWhatsAppResult
                        {
                            RequestId = id,
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

        // POST: ShopRequest/SendWhatsAppImage
        [HttpPost]
        public async Task<IActionResult> SendWhatsAppImage([FromForm] SendWhatsAppImageViewModel model)
        {
            try
            {
                if (model.Image == null || model.Image.Length == 0)
                    return Json(new { success = false, message = "الرجاء اختيار صورة" });

                if (string.IsNullOrEmpty(model.PhoneNumber))
                    return Json(new { success = false, message = "الرجاء إدخال رقم الهاتف" });

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "whatsapp_temp");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"whatsapp_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(fileStream);
                }

                var imageUrl = $"{_configuration["AppSettings:SiteUrl"]}/uploads/whatsapp_temp/{fileName}";
                var caption = model.Caption ?? "🛒 طلب من المحل";

                var response = await _whatsAppService.SendImageMessage(model.PhoneNumber, imageUrl, caption);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return Json(new
                {
                    success = response.Success,
                    message = response.Success ? "✅ تم إرسال الصورة بنجاح" : "❌ فشل الإرسال: " + response.Error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendWhatsAppImage");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // POST: ShopRequest/SendDailyReportAsHtml
        [HttpPost]
        public async Task<IActionResult> SendDailyReportAsHtml([FromBody] SendDailyReportViewModel model)
        {
            try
            {
                var today = DateTime.Today;
                var requests = await _context.ShopRequests
                    .Where(r => !r.IsDeleted && r.RequestDate.Date == today.Date)
                    .OrderByDescending(r => r.Priority)
                    .ToListAsync();

                if (!requests.Any())
                    return Json(new { success = false, message = "لا توجد طلبات لليوم" });

                var html = GenerateReportHtml(requests, model.UserMessage);

                var response = await _whatsAppService.SendLongMessage(model.PhoneNumber, html);

                return Json(new
                {
                    success = response.Success,
                    message = response.Success ? "✅ تم إرسال التقرير بنجاح" : "❌ فشل الإرسال: " + response.Error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendDailyReportAsHtml");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== دوال مساعدة =====
        private List<string> GetItemTypes()
        {
            return new List<string> { "شاحن", "سكرينة", "بطارية", "كابل", "سماعة", "قطعة غيار", "أخرى" };
        }

        private List<string> GetStatuses()
        {
            return new List<string> { "معلق", "قيد التنفيذ", "مكتمل", "ملغي" };
        }

        private Dictionary<int, string> GetPriorities()
        {
            return new Dictionary<int, string>
            {
                { 1, "عادي" },
                { 2, "مهم" },
                { 3, "عاجل" }
            };
        }

        private string GenerateReportHtml(List<ShopRequest> requests, string userMessage)
        {
            var totalCost = requests.Sum(r => r.Quantity * (r.UnitPrice ?? 0));
            var urgentCount = requests.Count(r => r.Priority == 3);
            var importantCount = requests.Count(r => r.Priority == 2);
            var normalCount = requests.Count(r => r.Priority == 1);

            var requestsHtml = new StringBuilder();
            foreach (var r in requests)
            {
                var priorityEmoji = r.Priority == 3 ? "🔴" : r.Priority == 2 ? "🟠" : "🟢";
                requestsHtml.AppendLine($@"
                    <div style='margin-bottom: 10px; padding: 10px; background: #f8f9fa; border-radius: 8px; border-right: 4px solid {(r.Priority == 3 ? "#dc3545" : r.Priority == 2 ? "#ffc107" : "#28a745")};'>
                        <div style='display: flex; justify-content: space-between;'>
                            <span style='font-size: 16px; font-weight: bold;'>{priorityEmoji} {r.ItemName}</span>
                            <span style='font-size: 14px; color: {(r.Priority == 3 ? "#dc3545" : r.Priority == 2 ? "#ffc107" : "#28a745")}; font-weight: bold;'>x{r.Quantity}</span>
                        </div>
                        <div style='margin-top: 5px; font-size: 13px; color: #666;'>
                            <span>📦 {r.ItemType}</span>
                            {(r.Supplier != null ? $"<span style='margin-right: 15px;'>🏢 {r.Supplier}</span>" : "")}
                            {(r.UnitPrice.HasValue ? $"<span style='margin-right: 15px;'>💰 {r.UnitPrice.Value:N0} ج.م</span>" : "")}
                        </div>
                        {(r.Notes != null ? $"<div style='margin-top: 5px; font-size: 12px; color: #999;'>📝 {r.Notes}</div>" : "")}
                    </div>");
            }

            var html = $@"
            <!DOCTYPE html>
            <html dir='rtl'>
            <head>
                <meta charset='UTF-8'>
                <title>تقرير الطلبات</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, sans-serif;
                        margin: 0;
                        padding: 20px;
                        background: white;
                        width: 800px;
                    }}
                    .report-container {{
                        background: white;
                        border-radius: 16px;
                        padding: 20px;
                        box-shadow: 0 5px 20px rgba(0,0,0,0.1);
                    }}
                    .header {{
                        text-align: center;
                        border-bottom: 2px solid #f39c12;
                        padding-bottom: 15px;
                        margin-bottom: 20px;
                    }}
                    .header h1 {{
                        margin: 0;
                        font-size: 24px;
                        color: #f39c12;
                    }}
                    .header .date {{
                        color: #666;
                        font-size: 14px;
                        margin-top: 5px;
                    }}
                    .stats {{
                        display: flex;
                        justify-content: space-around;
                        margin-bottom: 20px;
                        padding: 15px;
                        background: linear-gradient(135deg, #f39c12, #e67e22);
                        border-radius: 12px;
                        color: white;
                    }}
                    .stat-item {{
                        text-align: center;
                    }}
                    .stat-value {{
                        font-size: 20px;
                        font-weight: bold;
                    }}
                    .stat-label {{
                        font-size: 12px;
                        opacity: 0.9;
                    }}
                    .requests-list {{
                        margin-bottom: 20px;
                    }}
                    .user-message {{
                        margin-top: 20px;
                        padding: 15px;
                        background: #fff3e0;
                        border-radius: 8px;
                        border-right: 4px solid #f39c12;
                    }}
                    .user-message p {{
                        margin: 0;
                        color: #856404;
                        font-size: 14px;
                        line-height: 1.6;
                    }}
                    .footer {{
                        margin-top: 20px;
                        text-align: center;
                        font-size: 12px;
                        color: #999;
                        border-top: 1px solid #eee;
                        padding-top: 15px;
                    }}
                </style>
            </head>
            <body>
                <div class='report-container'>
                    <div class='header'>
                        <h1>📋 Mobile Shop - طلبات اليوم</h1>
                        <div class='date'>{DateTime.Now:yyyy/MM/dd}</div>
                    </div>

                    <div class='stats'>
                        <div class='stat-item'>
                            <div class='stat-value'>{requests.Count}</div>
                            <div class='stat-label'>إجمالي الطلبات</div>
                        </div>
                        <div class='stat-item'>
                            <div class='stat-value'>{urgentCount}</div>
                            <div class='stat-label'>عاجل</div>
                        </div>
                        <div class='stat-item'>
                            <div class='stat-value'>{importantCount}</div>
                            <div class='stat-label'>مهم</div>
                        </div>
                        <div class='stat-item'>
                            <div class='stat-value'>{totalCost:N0} ج.م</div>
                            <div class='stat-label'>التكلفة التقديرية</div>
                        </div>
                    </div>

                    <div class='requests-list'>
                        {requestsHtml}
                    </div>

                    {(string.IsNullOrEmpty(userMessage) ? "" : $@"
                    <div class='user-message'>
                        <p>{userMessage}</p>
                    </div>
                    ")}

                    <div class='footer'>
                        <div>📱 Mobile Shop - مركز صيانة معتمد</div>
                        <div>📞 01064211484</div>
                    </div>
                </div>
            </body>
            </html>";

            return html;
        }
    }

    // ViewModels
    public class SendBulkShopRequestWhatsAppViewModel
    {
        public List<int> RequestIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class ShopRequestBulkWhatsAppResult
    {
        public int RequestId { get; set; }
        public string? ItemName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }

    public class SendWhatsAppImageViewModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public IFormFile Image { get; set; } = null!;
        public string? Caption { get; set; }
    }

    public class SendDailyReportViewModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public List<int> SelectedRequestIds { get; set; } = new();
        public string? UserMessage { get; set; }
        public bool SendToAllUsers { get; set; }
    }

    public class ShopRequestFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ItemType { get; set; }
        public string? Status { get; set; }
        public int? Priority { get; set; }
        public string? SearchTerm { get; set; }
    }
}