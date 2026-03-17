using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using MobileShopSystem.Services;
using MobileShopSystem.ViewModels;
using System.Text.Json;
using System.Text;

namespace MobileShopSystem.Controllers
{
    public class RepairController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly WhatsAppService _whatsAppService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<RepairController> _logger;

        public RepairController(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            WhatsAppService whatsAppService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<RepairController> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _whatsAppService = whatsAppService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
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

        // ========== الصفحة الرئيسية ==========
        public IActionResult Index()
        {
            ViewBag.Username = GetCurrentUsername();
            return View();
        }

        // ========== إحصائيات لوحة التحكم ==========
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;
                var now = DateTime.Now;
                var devices = await _context.RepairDevices.Where(d => !d.IsDeleted).ToListAsync();

                var overdueDevices = devices.Count(d =>
                    d.Status != "تم التسليم" && d.PromisedDate.HasValue && d.PromisedDate.Value < now);

                var lowStockParts = await _context.SpareParts
                    .Where(p => !p.IsDeleted && p.Quantity <= p.MinQuantity)
                    .Select(p => new SparePartViewModel
                    {
                        Id = p.Id,
                        PartCode = p.PartCode,
                        PartName = p.PartName,
                        Quantity = p.Quantity,
                        MinQuantity = p.MinQuantity
                    }).ToListAsync();

                var stats = new RepairDashboardStatsViewModel
                {
                    TotalDevices = devices.Count,
                    ReceivedToday = devices.Count(d => d.ReceivedDate.Date == today),
                    InProgress = devices.Count(d => d.Status == "قيد الصيانة"),
                    WaitingParts = devices.Count(d => d.Status == "بانتظار قطع غيار"),
                    Completed = devices.Count(d => d.Status == "تم الاصلاح"),
                    Delivered = devices.Count(d => d.Status == "تم التسليم"),
                    OverdueDevices = overdueDevices,
                    TotalEstimatedCost = devices.Where(d => d.EstimatedCost.HasValue).Sum(d => d.EstimatedCost.Value),
                    TotalFinalCost = devices.Where(d => d.FinalCost.HasValue).Sum(d => d.FinalCost.Value),
                    TotalCollected = devices.Sum(d => d.AdvancePayment),
                    TotalRemaining = devices.Sum(d => d.RemainingAmount),
                    StatusDistribution = new Dictionary<string, int>
                    {
                        { "مستلم", devices.Count(d => d.Status == "مستلم") },
                        { "قيد الصيانة", devices.Count(d => d.Status == "قيد الصيانة") },
                        { "بانتظار قطع غيار", devices.Count(d => d.Status == "بانتظار قطع غيار") },
                        { "تم الاصلاح", devices.Count(d => d.Status == "تم الاصلاح") },
                        { "تم التسليم", devices.Count(d => d.Status == "تم التسليم") }
                    },
                    LowStockParts = lowStockParts
                };

                var recentDevices = await _context.RepairDevices
                    .Where(d => !d.IsDeleted)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(10)
                    .Select(d => new RepairDeviceViewModel
                    {
                        Id = d.Id,
                        DeviceCode = d.DeviceCode,
                        CustomerName = d.CustomerName,
                        CustomerPhone = d.CustomerPhone,
                        DeviceModel = d.DeviceModel,
                        Status = d.Status,
                        ReceivedDate = d.ReceivedDate,
                        EstimatedCost = d.EstimatedCost
                    }).ToListAsync();

                stats.RecentDevices = recentDevices;
                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardStats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== الحصول على جميع الأجهزة مع البحث المحسن ==========
        [HttpGet]
        public async Task<IActionResult> GetAllDevices(string search = "", string status = "all", bool showDeleted = false)
        {
            try
            {
                var query = _context.RepairDevices
                    .Include(d => d.Creator)
                    .Include(d => d.Technician)
                    .Where(d => d.IsDeleted == showDeleted)
                    .AsQueryable();

                // جلب البيانات أولاً
                var devices = await query.ToListAsync();
                var now = DateTime.Now;

                // تطبيق البحث المحلي في الذاكرة للتعامل مع StartsWith بشكل صحيح
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim().ToLower();
                    devices = devices.Where(d =>
                        (d.CustomerName ?? "").ToLower().Contains(search) ||
                        (d.CustomerPhone ?? "").StartsWith(search) ||  // الأهم: البحث بالبادئة
                        (d.CustomerPhone ?? "").Contains(search) ||
                        (d.DeviceCode ?? "").ToLower().Contains(search) ||
                        (d.DeviceBrand ?? "").ToLower().Contains(search) ||
                        (d.DeviceModel ?? "").ToLower().Contains(search) ||
                        (d.DeviceSerial ?? "").ToLower().Contains(search))
                        .ToList();
                }

                // تطبيق تصفية الحالة
                if (status != "all" && status != "overdue")
                    devices = devices.Where(d => d.Status == status).ToList();

                var result = new List<RepairDeviceViewModel>();

                foreach (var d in devices.OrderByDescending(d => d.CreatedAt))
                {
                    var daysInRepair = (now - d.ReceivedDate).Days;
                    var isOverdue = false;
                    var overdueDays = 0;

                    if (d.Status != "تم التسليم" && d.PromisedDate.HasValue && d.PromisedDate.Value < now)
                    {
                        isOverdue = true;
                        overdueDays = (now - d.PromisedDate.Value).Days;
                    }

                    if (status == "overdue" && !isOverdue)
                        continue;

                    string createdByName = d.Creator != null ? d.Creator.Username : "Unknown";
                    string technicianName = d.Technician != null ? d.Technician.Username : "غير معين";

                    result.Add(new RepairDeviceViewModel
                    {
                        Id = d.Id,
                        DeviceCode = d.DeviceCode,
                        CustomerName = d.CustomerName,
                        CustomerPhone = d.CustomerPhone,
                        DeviceType = d.DeviceType ?? "",
                        DeviceBrand = d.DeviceBrand ?? "",
                        DeviceModel = d.DeviceModel ?? "",
                        DeviceSerial = d.DeviceSerial,
                        DeviceColor = d.DeviceColor,
                        ReportedIssue = d.ReportedIssue,
                        EstimatedCost = d.EstimatedCost,
                        FinalCost = d.FinalCost,
                        AdvancePayment = d.AdvancePayment,
                        RemainingAmount = d.RemainingAmount,
                        ReceivedDate = d.ReceivedDate,
                        ReceivedDay = d.ReceivedDay ?? "",
                        ReceivedTime = d.ReceivedTime ?? "",
                        PromisedDate = d.PromisedDate,
                        CompletedDate = d.CompletedDate,
                        DeliveredDate = d.DeliveredDate,
                        Status = d.Status ?? "مستلم",
                        RequiresSpareParts = d.RequiresSpareParts,
                        SparePartsDetails = d.SparePartsDetails,
                        SparePartsCost = d.SparePartsCost,
                        IsWarranty = d.IsWarranty,
                        Notes = d.Notes,
                        CreatedByName = createdByName,
                        TechnicianName = technicianName,
                        CreatedAt = d.CreatedAt,
                        DaysInRepair = daysInRepair,
                        StatusColor = GetStatusColor(d.Status ?? "مستلم"),
                        StatusIcon = GetStatusIcon(d.Status ?? "مستلم"),
                        IsOverdue = isOverdue,
                        OverdueDays = overdueDays,
                        IsDeleted = d.IsDeleted
                    });
                }

                return Json(new { success = true, devices = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllDevices");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== البحث الفوري المحسن ==========
        [HttpGet]
        public async Task<IActionResult> SearchDevices(string term)
        {
            try
            {
                if (string.IsNullOrEmpty(term) || term.Length < 1)
                    return Json(new { success = true, devices = new List<object>() });

                term = term.Trim();

                var devices = await _context.RepairDevices
                    .Include(d => d.Technician)
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();

                var filteredDevices = devices.Where(d =>
                    (d.DeviceCode ?? "").Contains(term) ||
                    (d.CustomerName ?? "").Contains(term) ||
                    (d.CustomerPhone ?? "").StartsWith(term) ||  // الأهم: البحث بالبادئة
                    (d.CustomerPhone ?? "").Contains(term) ||
                    (d.DeviceSerial ?? "").Contains(term) ||
                    (d.DeviceModel ?? "").Contains(term))
                    .OrderByDescending(d => d.ReceivedDate)
                    .Take(30)
                    .ToList();

                var result = filteredDevices.Select(d => new
                {
                    d.Id,
                    d.DeviceCode,
                    d.CustomerName,
                    d.CustomerPhone,
                    DeviceBrand = d.DeviceBrand ?? "",
                    DeviceModel = d.DeviceModel ?? "",
                    Status = d.Status ?? "مستلم",
                    ReceivedDate = d.ReceivedDate.ToString("yyyy/MM/dd"),
                    TechnicianName = d.Technician != null ? d.Technician.Username : "غير معين",
                    StatusColor = GetStatusColor(d.Status ?? "مستلم"),
                    StatusIcon = GetStatusIcon(d.Status ?? "مستلم"),
                    d.EstimatedCost,
                    d.AdvancePayment
                }).ToList();

                return Json(new { success = true, devices = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchDevices");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== البحث عن العميل بالاسم أو رقم الهاتف (محسن) ==========
        [HttpGet]
        public async Task<IActionResult> SearchCustomerByPhone(string term)
        {
            try
            {
                if (string.IsNullOrEmpty(term) || term.Length < 1)
                    return Json(new { success = true, customers = new List<object>() });

                term = term.Trim();

                var devices = await _context.RepairDevices
                    .Include(d => d.Technician)
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();

                // البحث المحلي في الذاكرة للتعامل مع StartsWith بشكل صحيح
                var filteredDevices = devices.Where(d =>
                    (d.CustomerName ?? "").Contains(term) ||                    // اسم العميل يحتوي على
                    (d.CustomerPhone ?? "").StartsWith(term) ||                  // رقم الهاتف يبدأ بـ (مهم جداً)
                    (d.CustomerPhone ?? "").Contains(term))                      // رقم الهاتف يحتوي على
                    .OrderByDescending(d => d.ReceivedDate)
                    .ToList();

                if (!filteredDevices.Any())
                    return Json(new { success = true, customers = new List<object>() });

                // تجميع النتائج حسب العميل
                var groupedCustomers = filteredDevices
                    .GroupBy(d => new { d.CustomerName, d.CustomerPhone })
                    .Select(g => new
                    {
                        CustomerName = g.Key.CustomerName,
                        CustomerPhone = g.Key.CustomerPhone,
                        DeviceCount = g.Count(),
                        LastRepair = g.Max(d => d.ReceivedDate),
                        FirstRepair = g.Min(d => d.ReceivedDate),
                        TotalSpent = g.Sum(d => d.FinalCost ?? 0),
                        Devices = g.Select(d => new
                        {
                            d.Id,
                            d.DeviceCode,
                            DeviceBrand = d.DeviceBrand ?? "",
                            DeviceModel = d.DeviceModel ?? "",
                            Status = d.Status ?? "مستلم",
                            ReceivedDate = d.ReceivedDate.ToString("yyyy/MM/dd"),
                            StatusColor = GetStatusColor(d.Status ?? "مستلم"),
                            d.EstimatedCost,
                            d.AdvancePayment
                        }).Take(5).ToList()
                    })
                    .OrderByDescending(x => x.LastRepair)
                    .Take(20)
                    .ToList();

                return Json(new { success = true, customers = groupedCustomers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchCustomerByPhone");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== الحصول على تاريخ العميل كامل ==========
        [HttpGet]
        public async Task<IActionResult> GetCustomerHistory(string phone)
        {
            try
            {
                if (string.IsNullOrEmpty(phone))
                    return Json(new { success = false, message = "رقم الهاتف مطلوب" });

                var devices = await _context.RepairDevices
                    .Include(d => d.Technician)
                    .Include(d => d.StatusHistory).ThenInclude(h => h.Changer)
                    .Where(d => !d.IsDeleted && d.CustomerPhone == phone)
                    .OrderByDescending(d => d.ReceivedDate)
                    .ToListAsync();

                if (!devices.Any())
                    return Json(new { success = false, message = "لا توجد أجهزة لهذا العميل" });

                var now = DateTime.Now;
                var history = new CustomerHistoryViewModel
                {
                    CustomerName = devices.First().CustomerName,
                    CustomerPhone = phone,
                    TotalDevices = devices.Count,
                    TotalSpent = devices.Sum(d => d.FinalCost ?? 0),
                    FirstRepair = devices.Min(d => d.ReceivedDate),
                    LastRepair = devices.Max(d => d.ReceivedDate),
                    Devices = devices.Select(d => new CustomerDeviceHistory
                    {
                        Id = d.Id,
                        DeviceCode = d.DeviceCode,
                        DeviceBrand = d.DeviceBrand ?? "",
                        DeviceModel = d.DeviceModel ?? "",
                        DeviceSerial = d.DeviceSerial,
                        ReportedIssue = d.ReportedIssue,
                        ReceivedDate = d.ReceivedDate,
                        CompletedDate = d.CompletedDate,
                        DeliveredDate = d.DeliveredDate,
                        Status = d.Status ?? "مستلم",
                        EstimatedCost = d.EstimatedCost,
                        FinalCost = d.FinalCost,
                        AdvancePayment = d.AdvancePayment,
                        RemainingAmount = d.RemainingAmount,
                        TechnicianName = d.Technician?.Username ?? "غير معين",
                        StatusPath = d.StatusHistory?
                            .OrderBy(h => h.ChangedAt)
                            .Select(h => new DeviceStatusPath
                            {
                                Status = h.NewStatus,
                                ChangedAt = h.ChangedAt,
                                ChangedBy = h.Changer?.Username ?? "غير معروف",
                                Notes = h.Notes ?? "",
                                DaysInStatus = (h.ChangedAt - d.ReceivedDate).Days
                            }).ToList() ?? new List<DeviceStatusPath>()
                    }).ToList()
                };

                return Json(new { success = true, history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomerHistory");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== إضافة جهاز جديد ==========
        [HttpPost]
        public async Task<IActionResult> AddDevice([FromBody] CreateRepairDeviceViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                if (string.IsNullOrEmpty(model.CustomerName))
                    return Json(new { success = false, message = "الرجاء إدخال اسم العميل" });
                if (string.IsNullOrEmpty(model.CustomerPhone))
                    return Json(new { success = false, message = "الرجاء إدخال رقم الهاتف" });
                if (string.IsNullOrEmpty(model.ReportedIssue))
                    return Json(new { success = false, message = "الرجاء إدخال العيب" });

                var lastDevice = await _context.RepairDevices.OrderByDescending(d => d.Id).FirstOrDefaultAsync();
                int nextId = (lastDevice?.Id ?? 0) + 1;
                string deviceCode = $"RPR-{DateTime.Now:yyyyMMdd}-{nextId:D4}";

                var now = DateTime.Now;
                var arabicDays = new[] { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
                var dayName = arabicDays[(int)now.DayOfWeek];
                var remainingAmount = (model.EstimatedCost ?? 0) - model.AdvancePayment;

                var device = new RepairDevice
                {
                    DeviceCode = deviceCode,
                    CustomerName = model.CustomerName,
                    CustomerPhone = model.CustomerPhone,
                    DeviceType = model.DeviceType ?? "تليفون",
                    DeviceBrand = model.DeviceBrand ?? "اخرى",
                    DeviceModel = model.DeviceModel ?? "غير محدد",
                    DeviceSerial = model.DeviceSerial,
                    DeviceColor = model.DeviceColor,
                    DevicePassword = model.DevicePassword,
                    DeviceAccessories = model.DeviceAccessories,
                    ReportedIssue = model.ReportedIssue,
                    TechnicianNotes = model.TechnicianNotes,
                    EstimatedCost = model.EstimatedCost,
                    AdvancePayment = model.AdvancePayment,
                    RemainingAmount = remainingAmount,
                    ReceivedDate = now,
                    ReceivedDay = dayName,
                    ReceivedTime = now.ToString("hh:mm tt"),
                    PromisedDate = model.PromisedDate,
                    IsWarranty = model.IsWarranty,
                    WarrantyDetails = model.WarrantyDetails,
                    Notes = model.Notes,
                    Status = "مستلم",
                    CreatedBy = userId.Value,
                    CreatedAt = now,
                    TechnicianId = null,
                    IsDeleted = false
                };

                _context.RepairDevices.Add(device);
                await _context.SaveChangesAsync();

                var history = new RepairStatusHistory
                {
                    DeviceId = device.Id,
                    NewStatus = "مستلم",
                    ChangedBy = userId.Value,
                    ChangedAt = now,
                    Notes = "تم استلام الجهاز"
                };
                _context.RepairStatusHistories.Add(history);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم إضافة الجهاز بنجاح", deviceId = device.Id, deviceCode = device.DeviceCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddDevice");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== الحصول على تفاصيل جهاز ==========
        [HttpGet]
        public async Task<IActionResult> GetDeviceDetails(int id, bool includeDeleted = false)
        {
            try
            {
                var device = await _context.RepairDevices
                    .Include(d => d.Creator)
                    .Include(d => d.Technician)
                    .Include(d => d.StatusHistory).ThenInclude(h => h.Changer)
                    .Include(d => d.Images).ThenInclude(i => i.Uploader)
                    .Include(d => d.SparePartsUsed).ThenInclude(s => s.Part)
                    .Include(d => d.Installments).ThenInclude(i => i.Payments)
                    .Include(d => d.Warranties)
                    .Include(d => d.Ratings)
                    .Include(d => d.WhatsAppMessages)
                    .FirstOrDefaultAsync(d => d.Id == id && (includeDeleted || !d.IsDeleted));

                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                var now = DateTime.Now;
                var daysInRepair = (now - device.ReceivedDate).Days;
                var isOverdue = false;
                var overdueDays = 0;

                if (device.Status != "تم التسليم" && device.PromisedDate.HasValue && device.PromisedDate.Value < now)
                {
                    isOverdue = true;
                    overdueDays = (now - device.PromisedDate.Value).Days;
                }

                var statusHistory = device.StatusHistory?
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new RepairStatusHistoryViewModel
                    {
                        Id = h.Id,
                        OldStatus = h.OldStatus ?? "",
                        NewStatus = h.NewStatus ?? "",
                        ChangedByName = h.Changer?.Username ?? "Unknown",
                        ChangedAt = h.ChangedAt,
                        Notes = h.Notes ?? "",
                        TimeAgo = GetTimeAgo(h.ChangedAt)
                    }).ToList() ?? new List<RepairStatusHistoryViewModel>();

                var images = device.Images?
                    .OrderByDescending(i => i.UploadedAt)
                    .Select(i => new RepairDeviceImageViewModel
                    {
                        Id = i.Id,
                        ImagePath = i.ImagePath,
                        ImageType = i.ImageType ?? "",
                        UploadedByName = i.Uploader?.Username ?? "Unknown",
                        UploadedAt = i.UploadedAt,
                        Notes = i.Notes ?? ""
                    }).ToList() ?? new List<RepairDeviceImageViewModel>();

                var sparePartsUsed = device.SparePartsUsed?
                    .Select(s => new SparePartUsedViewModel
                    {
                        Id = s.Id,
                        PartId = s.PartId,
                        PartName = s.Part?.PartName ?? "",
                        PartCode = s.Part?.PartCode ?? "",
                        Quantity = s.Quantity,
                        Price = s.Price,
                        UsedAt = s.UsedAt,
                        Notes = s.Notes ?? ""
                    }).ToList() ?? new List<SparePartUsedViewModel>();

                var installments = device.Installments?
                    .Select(i => new RepairInstallmentViewModel
                    {
                        Id = i.Id,
                        TotalAmount = i.TotalAmount,
                        DownPayment = i.DownPayment,
                        RemainingAmount = i.RemainingAmount,
                        NumberOfInstallments = i.NumberOfInstallments,
                        InstallmentAmount = i.InstallmentAmount,
                        StartDate = i.StartDate,
                        Status = i.Status ?? "نشط",
                        PaidInstallments = i.Payments?.Count(p => p.IsPaid) ?? 0,
                        Payments = i.Payments?.Select(p => new RepairInstallmentPaymentViewModel
                        {
                            Id = p.Id,
                            Amount = p.Amount,
                            DueDate = p.DueDate,
                            PaidDate = p.PaidDate,
                            IsPaid = p.IsPaid,
                            PaymentMethod = p.PaymentMethod ?? "",
                            Notes = p.Notes ?? ""
                        }).ToList()
                    }).ToList() ?? new List<RepairInstallmentViewModel>();

                var warranty = device.Warranties?.OrderByDescending(w => w.CreatedAt).FirstOrDefault();
                WarrantyViewModel warrantyVM = null;
                if (warranty != null)
                {
                    warrantyVM = new WarrantyViewModel
                    {
                        Id = warranty.Id,
                        WarrantyNumber = warranty.WarrantyNumber,
                        StartDate = warranty.StartDate,
                        EndDate = warranty.EndDate,
                        WarrantyType = warranty.WarrantyType,
                        Coverage = warranty.Coverage ?? "",
                        Cost = warranty.Cost,
                        IsActive = warranty.IsActive
                    };
                }

                var rating = device.Ratings?.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
                RepairRatingViewModel ratingVM = null;
                if (rating != null)
                {
                    ratingVM = new RepairRatingViewModel
                    {
                        Id = rating.Id,
                        CustomerSatisfaction = rating.CustomerSatisfaction,
                        TechnicianRating = rating.TechnicianRating,
                        PriceRating = rating.PriceRating,
                        Comment = rating.Comment ?? "",
                        CreatedAt = rating.CreatedAt
                    };
                }

                var whatsAppMessages = device.WhatsAppMessages?
                    .OrderByDescending(w => w.SentAt)
                    .Select(w => new WhatsAppMessageLogViewModel
                    {
                        Id = w.Id,
                        CustomerPhone = w.CustomerPhone,
                        MessageType = w.MessageType,
                        Message = w.Message,
                        SentAt = w.SentAt,
                        IsSuccess = w.IsSuccess,
                        MessageId = w.MessageId,
                        Error = w.Error,
                        TimeAgo = GetTimeAgo(w.SentAt)
                    }).ToList() ?? new List<WhatsAppMessageLogViewModel>();

                var viewModel = new RepairDeviceViewModel
                {
                    Id = device.Id,
                    DeviceCode = device.DeviceCode,
                    CustomerName = device.CustomerName,
                    CustomerPhone = device.CustomerPhone,
                    DeviceType = device.DeviceType ?? "",
                    DeviceBrand = device.DeviceBrand ?? "",
                    DeviceModel = device.DeviceModel ?? "",
                    DeviceSerial = device.DeviceSerial,
                    DeviceColor = device.DeviceColor,
                    DevicePassword = device.DevicePassword,
                    DeviceAccessories = device.DeviceAccessories,
                    ReportedIssue = device.ReportedIssue,
                    TechnicianNotes = device.TechnicianNotes,
                    EstimatedCost = device.EstimatedCost,
                    FinalCost = device.FinalCost,
                    AdvancePayment = device.AdvancePayment,
                    RemainingAmount = device.RemainingAmount,
                    ReceivedDate = device.ReceivedDate,
                    ReceivedDay = device.ReceivedDay ?? "",
                    ReceivedTime = device.ReceivedTime ?? "",
                    PromisedDate = device.PromisedDate,
                    CompletedDate = device.CompletedDate,
                    DeliveredDate = device.DeliveredDate,
                    Status = device.Status ?? "مستلم",
                    RequiresSpareParts = device.RequiresSpareParts,
                    SparePartsDetails = device.SparePartsDetails,
                    SparePartsCost = device.SparePartsCost,
                    IsWarranty = device.IsWarranty,
                    WarrantyDetails = device.WarrantyDetails,
                    Notes = device.Notes,
                    CreatedByName = device.Creator?.Username ?? "Unknown",
                    TechnicianName = device.Technician?.Username ?? "غير معين",
                    CreatedAt = device.CreatedAt,
                    DaysInRepair = daysInRepair,
                    StatusColor = GetStatusColor(device.Status ?? "مستلم"),
                    StatusIcon = GetStatusIcon(device.Status ?? "مستلم"),
                    IsOverdue = isOverdue,
                    OverdueDays = overdueDays,
                    IsDeleted = device.IsDeleted,
                    StatusHistory = statusHistory,
                    Images = images,
                    SparePartsUsed = sparePartsUsed,
                    Installments = installments,
                    Warranty = warrantyVM,
                    Rating = ratingVM,
                    WhatsAppMessages = whatsAppMessages
                };

                return Json(new { success = true, device = viewModel });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDeviceDetails");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== تغيير حالة الجهاز ==========
        [HttpPost]
        public async Task<IActionResult> ChangeStatus([FromBody] ChangeRepairStatusViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices
                    .Include(d => d.Creator)
                    .Include(d => d.Technician)
                    .Include(d => d.SparePartsUsed).ThenInclude(s => s.Part)
                    .FirstOrDefaultAsync(d => d.Id == model.Id && !d.IsDeleted);

                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                var oldStatus = device.Status;
                var now = DateTime.Now;

                device.Status = model.Status;
                device.UpdatedBy = userId;
                device.UpdatedAt = now;

                if (model.Status == "تم الاصلاح")
                {
                    device.CompletedDate = now;
                    if (model.FinalCost.HasValue)
                    {
                        device.FinalCost = model.FinalCost.Value;
                        device.RemainingAmount = device.FinalCost.Value - device.AdvancePayment;
                    }
                }
                else if (model.Status == "تم التسليم")
                {
                    device.DeliveredDate = now;
                }

                if (model.AdvancePayment.HasValue)
                {
                    device.AdvancePayment = model.AdvancePayment.Value;
                    device.RemainingAmount = (device.FinalCost ?? device.EstimatedCost ?? 0) - device.AdvancePayment;
                }

                var history = new RepairStatusHistory
                {
                    DeviceId = device.Id,
                    OldStatus = oldStatus,
                    NewStatus = model.Status,
                    ChangedBy = userId.Value,
                    ChangedAt = now,
                    Notes = model.Notes
                };
                _context.RepairStatusHistories.Add(history);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم تغيير حالة الجهاز بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChangeStatus");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== تحديث بيانات الجهاز ==========
        [HttpPost]
        public async Task<IActionResult> UpdateDevice([FromBody] UpdateRepairDeviceViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices.FindAsync(model.Id);
                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                device.CustomerName = model.CustomerName;
                device.CustomerPhone = model.CustomerPhone;
                device.DeviceType = model.DeviceType;
                device.DeviceBrand = model.DeviceBrand;
                device.DeviceModel = model.DeviceModel;
                device.DeviceSerial = model.DeviceSerial;
                device.DeviceColor = model.DeviceColor;
                device.DevicePassword = model.DevicePassword;
                device.DeviceAccessories = model.DeviceAccessories;
                device.ReportedIssue = model.ReportedIssue;
                device.TechnicianNotes = model.TechnicianNotes;
                device.EstimatedCost = model.EstimatedCost;
                device.FinalCost = model.FinalCost;
                device.AdvancePayment = model.AdvancePayment;
                device.RemainingAmount = (model.FinalCost ?? model.EstimatedCost ?? 0) - model.AdvancePayment;
                device.PromisedDate = model.PromisedDate;
                device.RequiresSpareParts = model.RequiresSpareParts;
                device.SparePartsDetails = model.SparePartsDetails;
                device.SparePartsCost = model.SparePartsCost;
                device.IsWarranty = model.IsWarranty;
                device.WarrantyDetails = model.WarrantyDetails;
                device.Notes = model.Notes;
                device.UpdatedBy = userId;
                device.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ تم تحديث بيانات الجهاز بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateDevice");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== تعيين مهندس للجهاز ==========
        [HttpPost]
        public async Task<IActionResult> AssignTechnician([FromBody] AssignTechnicianViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices.FindAsync(model.DeviceId);
                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                var technician = await _context.Users.FindAsync(model.TechnicianId);
                if (technician == null)
                    return Json(new { success = false, message = "المهندس غير موجود" });

                device.TechnicianId = model.TechnicianId;
                device.UpdatedBy = userId;
                device.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم تعيين المهندس بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignTechnician");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== إضافة قطع غيار ==========
        [HttpPost]
        public async Task<IActionResult> AddSparePart([FromBody] AddSparePartViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices.FindAsync(model.RepairId);
                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                var part = await _context.SpareParts.FindAsync(model.PartId);
                if (part == null)
                    return Json(new { success = false, message = "قطعة الغيار غير موجودة" });

                if (part.Quantity < model.Quantity)
                    return Json(new { success = false, message = $"الكمية المطلوبة ({model.Quantity}) أكبر من المتوفر ({part.Quantity})" });

                var usedPart = new RepairSparePartUsed
                {
                    RepairId = model.RepairId,
                    PartId = model.PartId,
                    Quantity = model.Quantity,
                    Price = part.SellingPrice,
                    UsedAt = DateTime.Now,
                    Notes = model.Notes
                };

                part.Quantity -= model.Quantity;
                part.UpdatedAt = DateTime.Now;

                device.RequiresSpareParts = true;
                device.SparePartsDetails += $"\n- {part.PartName} (كمية: {model.Quantity})";
                device.SparePartsCost += part.SellingPrice * model.Quantity;

                _context.RepairSparePartsUsed.Add(usedPart);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم إضافة قطعة الغيار بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSparePart");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== دفع قسط ==========
        [HttpPost]
        public async Task<IActionResult> PayInstallment([FromBody] PayInstallmentViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var payment = await _context.RepairInstallmentPayments
                    .Include(p => p.Installment).ThenInclude(i => i.Device)
                    .FirstOrDefaultAsync(p => p.Id == model.InstallmentPaymentId);

                if (payment == null)
                    return Json(new { success = false, message = "الدفعة غير موجودة" });

                if (payment.IsPaid)
                    return Json(new { success = false, message = "❌ هذه الدفعة مدفوعة بالفعل" });

                if (model.Amount < payment.Amount)
                    return Json(new { success = false, message = $"❌ المبلغ المدفوع ({model.Amount}) أقل من قيمة القسط ({payment.Amount})" });

                payment.IsPaid = true;
                payment.PaidDate = DateTime.Now;
                payment.PaymentMethod = model.PaymentMethod;
                payment.Notes = model.Notes;

                var installment = payment.Installment;
                installment.RemainingAmount -= payment.Amount;

                if (installment.RemainingAmount <= 0)
                    installment.Status = "مكتمل";

                var device = installment.Device;
                device.AdvancePayment += payment.Amount;
                device.RemainingAmount = (device.FinalCost ?? device.EstimatedCost ?? 0) - device.AdvancePayment;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم دفع القسط بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PayInstallment");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== إنشاء ضمان ==========
        [HttpPost]
        public async Task<IActionResult> CreateWarranty([FromBody] CreateWarrantyViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices.FindAsync(model.DeviceId);
                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                var warrantyNumber = $"WRN-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                var warranty = new Warranty
                {
                    DeviceId = model.DeviceId,
                    WarrantyNumber = warrantyNumber,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    WarrantyType = model.WarrantyType,
                    Coverage = model.Coverage,
                    Cost = model.Cost,
                    IsActive = true,
                    Notes = model.Notes
                };

                _context.Warranties.Add(warranty);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم إنشاء الضمان بنجاح", warrantyNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateWarranty");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== إضافة تقييم ==========
        [HttpPost]
        public async Task<IActionResult> AddRating([FromBody] AddRatingViewModel model)
        {
            try
            {
                var rating = new RepairRating
                {
                    DeviceId = model.DeviceId,
                    CustomerSatisfaction = model.CustomerSatisfaction,
                    TechnicianRating = model.TechnicianRating,
                    PriceRating = model.PriceRating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now
                };

                _context.RepairRatings.Add(rating);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم إضافة التقييم بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddRating");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== إرسال رسالة واتساب ==========
        [HttpPost]
        public async Task<IActionResult> SendWhatsAppMessage([FromBody] SendWhatsAppViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices
                    .Include(d => d.Technician)
                    .FirstOrDefaultAsync(d => d.Id == model.DeviceId && !d.IsDeleted);

                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                WhatsAppResponse response;
                string messageType = "";

                switch (model.MessageType?.ToLower())
                {
                    case "received":
                        response = await _whatsAppService.SendDeviceReceivedMessage(
                            device.CustomerPhone,
                            device.CustomerName,
                            $"{device.DeviceBrand} {device.DeviceModel}",
                            device.DeviceCode,
                            device.ReceivedDate);
                        messageType = "استلام";
                        break;

                    case "completed":
                        response = await _whatsAppService.SendRepairCompletedMessage(
                            device.CustomerPhone,
                            device.CustomerName,
                            $"{device.DeviceBrand} {device.DeviceModel}",
                            device.FinalCost ?? device.EstimatedCost ?? 0,
                            device.AdvancePayment,
                            device.RemainingAmount);
                        messageType = "اكتمال";
                        break;

                    case "reminder":
                        response = await _whatsAppService.SendReminderMessage(
                            device.CustomerPhone,
                            device.CustomerName,
                            $"{device.DeviceBrand} {device.DeviceModel}",
                            device.RemainingAmount,
                            device.PromisedDate);
                        messageType = "تذكير";
                        break;

                    default:
                        response = await _whatsAppService.SendLongMessage(device.CustomerPhone, model.CustomMessage);
                        messageType = "مخصص";
                        break;
                }

                var messageLog = new WhatsAppMessageLog
                {
                    DeviceId = device.Id,
                    CustomerPhone = device.CustomerPhone,
                    MessageType = messageType,
                    Message = model.CustomMessage ?? "",
                    SentAt = DateTime.Now,
                    IsSuccess = response.Success,
                    MessageId = response.MessageId,
                    Error = response.Error
                };
                _context.WhatsAppMessageLogs.Add(messageLog);
                await _context.SaveChangesAsync();

                return Json(new { success = response.Success, message = response.Success ? "✅ تم إرسال الرسالة" : "❌ فشل الإرسال: " + response.Error });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendWhatsAppMessage");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== إرسال رسالة جماعية ==========
        [HttpPost]
        public async Task<IActionResult> SendBulkWhatsAppMessage([FromBody] SendBulkWhatsAppViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var results = new List<BulkWhatsAppResult>();
                int successCount = 0;
                int failCount = 0;

                foreach (var phone in model.PhoneNumbers)
                {
                    try
                    {
                        var response = await _whatsAppService.SendLongMessage(phone, model.Message);

                        if (response.Success)
                        {
                            successCount++;
                            results.Add(new BulkWhatsAppResult { Phone = phone, Success = true, MessageId = response.MessageId });
                        }
                        else
                        {
                            failCount++;
                            results.Add(new BulkWhatsAppResult { Phone = phone, Success = false, Error = response.Error });
                        }

                        // انتظار نصف ثانية بين كل رسالة
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        results.Add(new BulkWhatsAppResult { Phone = phone, Success = false, Error = ex.Message });
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

        // ========== الحصول على جميع أرقام الهواتف للرسائل الجماعية ==========
        [HttpGet]
        public async Task<IActionResult> GetAllPhonesForBulk()
        {
            try
            {
                var phones = await _context.RepairDevices
                    .Where(d => !d.IsDeleted && !string.IsNullOrEmpty(d.CustomerPhone))
                    .Select(d => new
                    {
                        d.Id,
                        d.CustomerName,
                        d.CustomerPhone,
                        d.DeviceCode,
                        DeviceBrand = d.DeviceBrand ?? "",
                        DeviceModel = d.DeviceModel ?? "",
                        d.Status
                    })
                    .OrderBy(d => d.CustomerName)
                    .ToListAsync();

                return Json(new { success = true, phones });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllPhonesForBulk");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== رفع صورة ==========
        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                if (model.Image == null || model.Image.Length == 0)
                    return Json(new { success = false, message = "الرجاء اختيار صورة" });

                var device = await _context.RepairDevices.FindAsync(model.DeviceId);
                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "repair");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{device.DeviceCode}_{model.ImageType}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(fileStream);
                }

                var image = new RepairDeviceImage
                {
                    DeviceId = model.DeviceId,
                    ImagePath = $"/uploads/repair/{fileName}",
                    ImageType = model.ImageType,
                    UploadedBy = userId.Value,
                    UploadedAt = DateTime.Now,
                    Notes = model.Notes
                };

                _context.RepairDeviceImages.Add(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم رفع الصورة بنجاح", imagePath = image.ImagePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadImage");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== حذف جهاز (ناعم) ==========
        [HttpPost]
        public async Task<IActionResult> SoftDeleteDevice(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices.FindAsync(id);
                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                device.IsDeleted = true;
                device.DeletedBy = userId;
                device.DeletedAt = DateTime.Now;
                device.UpdatedBy = userId;
                device.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var history = new RepairStatusHistory
                {
                    DeviceId = device.Id,
                    OldStatus = device.Status,
                    NewStatus = device.Status,
                    ChangedBy = userId.Value,
                    ChangedAt = DateTime.Now,
                    Notes = "تم حذف الجهاز"
                };
                _context.RepairStatusHistories.Add(history);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم حذف الجهاز بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SoftDeleteDevice");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== استعادة جهاز محذوف ==========
        [HttpPost]
        public async Task<IActionResult> RestoreDevice(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices.FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted);
                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود أو غير محذوف" });

                device.IsDeleted = false;
                device.DeletedBy = null;
                device.DeletedAt = null;
                device.UpdatedBy = userId;
                device.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var history = new RepairStatusHistory
                {
                    DeviceId = device.Id,
                    OldStatus = device.Status,
                    NewStatus = device.Status,
                    ChangedBy = userId.Value,
                    ChangedAt = DateTime.Now,
                    Notes = "تم استعادة الجهاز"
                };
                _context.RepairStatusHistories.Add(history);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم استعادة الجهاز بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RestoreDevice");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== حذف جهاز نهائي ==========
        [HttpPost]
        public async Task<IActionResult> HardDeleteDevice(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var device = await _context.RepairDevices
                    .Include(d => d.StatusHistory)
                    .Include(d => d.Images)
                    .Include(d => d.SparePartsUsed)
                    .Include(d => d.Installments).ThenInclude(i => i.Payments)
                    .Include(d => d.Warranties)
                    .Include(d => d.Ratings)
                    .Include(d => d.WhatsAppMessages)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                if (device.Images != null)
                {
                    foreach (var image in device.Images)
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                            System.IO.File.Delete(imagePath);
                    }
                }

                if (device.StatusHistory != null)
                    _context.RepairStatusHistories.RemoveRange(device.StatusHistory);
                if (device.Images != null)
                    _context.RepairDeviceImages.RemoveRange(device.Images);
                if (device.SparePartsUsed != null)
                    _context.RepairSparePartsUsed.RemoveRange(device.SparePartsUsed);
                if (device.Installments != null)
                {
                    foreach (var installment in device.Installments)
                        if (installment.Payments != null)
                            _context.RepairInstallmentPayments.RemoveRange(installment.Payments);
                    _context.RepairInstallments.RemoveRange(device.Installments);
                }
                if (device.Warranties != null)
                    _context.Warranties.RemoveRange(device.Warranties);
                if (device.Ratings != null)
                    _context.RepairRatings.RemoveRange(device.Ratings);
                if (device.WhatsAppMessages != null)
                    _context.WhatsAppMessageLogs.RemoveRange(device.WhatsAppMessages);

                _context.RepairDevices.Remove(device);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم حذف الجهاز نهائياً" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HardDeleteDevice");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ========== الحصول على الأجهزة المحذوفة ==========
        [HttpGet]
        public async Task<IActionResult> GetDeletedDevices()
        {
            try
            {
                var devices = await _context.RepairDevices
                    .Include(d => d.Creator)
                    .Include(d => d.Technician)
                    .Where(d => d.IsDeleted)
                    .OrderByDescending(d => d.DeletedAt)
                    .Select(d => new
                    {
                        d.Id,
                        d.DeviceCode,
                        d.CustomerName,
                        d.CustomerPhone,
                        DeviceBrand = d.DeviceBrand ?? "",
                        DeviceModel = d.DeviceModel ?? "",
                        d.DeviceSerial,
                        d.ReportedIssue,
                        Status = d.Status ?? "مستلم",
                        d.EstimatedCost,
                        d.AdvancePayment,
                        d.RemainingAmount,
                        ReceivedDate = d.ReceivedDate.ToString("yyyy-MM-dd HH:mm"),
                        DeletedAt = d.DeletedAt.HasValue ? d.DeletedAt.Value.ToString("yyyy-MM-dd HH:mm") : "",
                        DeletedBy = d.DeletedBy.HasValue ?
                            _context.Users.Where(u => u.Id == d.DeletedBy.Value).Select(u => u.Username).FirstOrDefault() :
                            "غير معروف",
                        StatusColor = GetStatusColor(d.Status ?? "مستلم"),
                        StatusIcon = GetStatusIcon(d.Status ?? "مستلم")
                    })
                    .ToListAsync();

                return Json(new { success = true, devices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDeletedDevices");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== الحصول على قطع الغيار ==========
        [HttpGet]
        public async Task<IActionResult> GetSpareParts(string search = "", bool lowStockOnly = false)
        {
            try
            {
                var query = _context.SpareParts.Where(p => !p.IsDeleted).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(p => p.PartCode.Contains(search) || p.PartName.Contains(search));

                if (lowStockOnly)
                    query = query.Where(p => p.Quantity <= p.MinQuantity);

                var rawParts = await query
                    .OrderBy(p => p.PartName)
                    .ToListAsync();
                var parts = rawParts.Select(p => new SparePartViewModel
                {
                    Id = p.Id,
                    PartCode = p.PartCode,
                    PartName = p.PartName,
                    CompatibleModels = JsonSerializer.Deserialize<string[]>(p.CompatibleModels ?? "[]"),
                    Quantity = p.Quantity,
                    MinQuantity = p.MinQuantity,
                    Cost = p.Cost,
                    SellingPrice = p.SellingPrice,
                    Supplier = p.Supplier,
                    Location = p.Location
                }).ToList();

                return Json(new { success = true, parts = parts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSpareParts");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== الحصول على سجل الواتساب ==========
        [HttpGet]
        public async Task<IActionResult> GetWhatsAppLogs(int deviceId)
        {
            try
            {
                var logs = await _context.WhatsAppMessageLogs
                    .Where(l => l.DeviceId == deviceId)
                    .OrderByDescending(l => l.SentAt)
                    .Select(l => new WhatsAppMessageLogViewModel
                    {
                        Id = l.Id,
                        CustomerPhone = l.CustomerPhone,
                        MessageType = l.MessageType,
                        Message = l.Message,
                        SentAt = l.SentAt,
                        IsSuccess = l.IsSuccess,
                        MessageId = l.MessageId,
                        Error = l.Error,
                        TimeAgo = GetTimeAgo(l.SentAt)
                    })
                    .ToListAsync();

                return Json(new { success = true, logs = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetWhatsAppLogs");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== تقرير أداء الفنيين ==========
        [HttpGet]
        public async Task<IActionResult> TechnicianPerformanceReport(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var report = await _context.RepairDevices
                    .Include(d => d.Technician)
                    .Include(d => d.Ratings)
                    .Where(d => d.CreatedAt >= fromDate && d.CreatedAt <= toDate && !d.IsDeleted)
                    .GroupBy(d => d.TechnicianId)
                    .Select(g => new TechnicianPerformanceReportViewModel
                    {
                        TechnicianName = g.First().Technician != null ? g.First().Technician.Username : "غير معين",
                        TotalDevices = g.Count(),
                        CompletedDevices = g.Count(d => d.Status == "تم التسليم"),
                        AverageDays = g.Where(d => d.DeliveredDate.HasValue)
                                       .Average(d => (d.DeliveredDate.Value - d.ReceivedDate).Days),
                        TotalRevenue = g.Sum(d => d.FinalCost ?? 0),
                        CustomerRating = g.SelectMany(d => d.Ratings)
                                          .Where(r => r.TechnicianRating.HasValue)
                                          .Average(r => (double?)r.TechnicianRating) ?? 0,
                        DevicesInProgress = g.Count(d => d.Status == "قيد الصيانة")
                    })
                    .OrderByDescending(r => r.TotalRevenue)
                    .ToListAsync();

                return Json(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TechnicianPerformanceReport");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== تقرير الإيرادات ==========
        [HttpGet]
        public async Task<IActionResult> RevenueReport(DateTime fromDate, DateTime toDate, string period = "day")
        {
            try
            {
                var devices = await _context.RepairDevices
                    .Include(d => d.SparePartsUsed)
                    .Where(d => d.DeliveredDate.HasValue && d.DeliveredDate.Value >= fromDate &&
                               d.DeliveredDate.Value <= toDate && !d.IsDeleted)
                    .ToListAsync();

                var report = new List<RevenueReportViewModel>();

                if (period == "day")
                {
                    report = devices
                        .GroupBy(d => d.DeliveredDate.Value.Date)
                        .Select(g => new RevenueReportViewModel
                        {
                            Period = "يومي",
                            Date = g.Key,
                            DevicesCount = g.Count(),
                            Revenue = g.Sum(d => d.FinalCost ?? 0),
                            Expenses = g.Sum(d => d.SparePartsUsed.Sum(s => s.Price * s.Quantity)),
                            Profit = g.Sum(d => (d.FinalCost ?? 0) - d.SparePartsUsed.Sum(s => s.Price * s.Quantity))
                        })
                        .OrderBy(r => r.Date)
                        .ToList();
                }
                else if (period == "month")
                {
                    report = devices
                        .GroupBy(d => new { d.DeliveredDate.Value.Year, d.DeliveredDate.Value.Month })
                        .Select(g => new RevenueReportViewModel
                        {
                            Period = "شهري",
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            DevicesCount = g.Count(),
                            Revenue = g.Sum(d => d.FinalCost ?? 0),
                            Expenses = g.Sum(d => d.SparePartsUsed.Sum(s => s.Price * s.Quantity)),
                            Profit = g.Sum(d => (d.FinalCost ?? 0) - d.SparePartsUsed.Sum(s => s.Price * s.Quantity))
                        })
                        .OrderBy(r => r.Date)
                        .ToList();
                }

                return Json(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RevenueReport");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== تقرير العملاء (محسن) ==========
        [HttpGet]
        public async Task<IActionResult> CustomerReport(string searchTerm = null)
        {
            try
            {
                var devices = await _context.RepairDevices
                    .Include(d => d.Ratings)
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    devices = devices.Where(d =>
                        (d.CustomerName ?? "").Contains(searchTerm) ||
                        (d.CustomerPhone ?? "").StartsWith(searchTerm) ||
                        (d.CustomerPhone ?? "").Contains(searchTerm))
                        .ToList();
                }

                var report = devices
                    .GroupBy(d => new { d.CustomerName, d.CustomerPhone })
                    .Select(g => new CustomerReportViewModel
                    {
                        CustomerPhone = g.Key.CustomerPhone,
                        CustomerName = g.Key.CustomerName,
                        TotalDevices = g.Count(),
                        TotalSpent = g.Sum(d => d.FinalCost ?? 0),
                        CompletedDevices = g.Count(d => d.Status == "تم التسليم"),
                        PendingDevices = g.Count(d => d.Status != "تم التسليم"),
                        LastRepairDate = g.Max(d => d.DeliveredDate ?? d.ReceivedDate),
                        Devices = g.OrderByDescending(d => d.ReceivedDate)
                                  .Take(5)
                                  .Select(d => new RepairDeviceViewModel
                                  {
                                      Id = d.Id,
                                      DeviceCode = d.DeviceCode,
                                      DeviceModel = d.DeviceModel,
                                      Status = d.Status,
                                      ReceivedDate = d.ReceivedDate,
                                      FinalCost = d.FinalCost
                                  }).ToList()
                    })
                    .OrderByDescending(r => r.TotalSpent)
                    .Take(50)
                    .ToList();

                return Json(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CustomerReport");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== تقرير الأجهزة ==========
        [HttpGet]
        public async Task<IActionResult> DevicesReport(DateTime fromDate, DateTime toDate, string status = "", string searchTerm = "")
        {
            try
            {
                var query = _context.RepairDevices
                    .Include(d => d.Creator)
                    .Include(d => d.Technician)
                    .Where(d => !d.IsDeleted && d.ReceivedDate.Date >= fromDate.Date && d.ReceivedDate.Date <= toDate.Date)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(d => d.Status == status);

                var devices = await query.ToListAsync();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    devices = devices.Where(d =>
                        (d.CustomerName ?? "").Contains(searchTerm) ||
                        (d.CustomerPhone ?? "").Contains(searchTerm) ||
                        (d.DeviceCode ?? "").Contains(searchTerm) ||
                        (d.DeviceModel ?? "").Contains(searchTerm))
                        .ToList();
                }

                var result = devices
                    .OrderByDescending(d => d.ReceivedDate)
                    .Select(d => new
                    {
                        d.Id,
                        d.DeviceCode,
                        d.CustomerName,
                        d.CustomerPhone,
                        DeviceBrand = d.DeviceBrand ?? "",
                        DeviceModel = d.DeviceModel ?? "",
                        Status = d.Status ?? "مستلم",
                        d.EstimatedCost,
                        d.FinalCost,
                        d.AdvancePayment,
                        d.RemainingAmount,
                        ReceivedDate = d.ReceivedDate,
                        StatusColor = GetStatusColor(d.Status ?? "مستلم"),
                        TechnicianName = d.Technician != null ? d.Technician.Username : "غير معين"
                    })
                    .ToList();

                return Json(new { success = true, devices = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DevicesReport");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== إنشاء PDF للجهاز ==========
        [HttpGet]
        public async Task<IActionResult> GenerateDevicePDF(int id)
        {
            try
            {
                var device = await _context.RepairDevices
                    .Include(d => d.Creator)
                    .Include(d => d.Technician)
                    .Include(d => d.StatusHistory).ThenInclude(h => h.Changer)
                    .Include(d => d.SparePartsUsed).ThenInclude(s => s.Part)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (device == null)
                    return Json(new { success = false, message = "الجهاز غير موجود" });

                var html = GenerateDevicePDFHtml(device);
                return Json(new { success = true, html });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateDevicePDF");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GenerateDevicePDFHtml(RepairDevice device)
        {
            var now = DateTime.Now;
            var shopName = "📱 Mobile Shop";
            var shopPhone = "📞 01064211484";

            var statusHistoryHtml = "";
            if (device.StatusHistory != null && device.StatusHistory.Any())
            {
                foreach (var h in device.StatusHistory.OrderByDescending(h => h.ChangedAt).Take(5))
                {
                    statusHistoryHtml += $@"
                        <div style='margin: 1mm 0; padding: 0.5mm 0; border-bottom: 1px dotted #ccc; font-size: 8px; display: flex; justify-content: space-between;'>
                            <span style='background: {GetStatusColorForPDF(h.NewStatus)}; color: white; padding: 0.5mm 1mm; border-radius: 1mm;'>{h.NewStatus}</span>
                            <span style='color: #666;'>{h.ChangedAt:yyyy/MM/dd}</span>
                        </div>";
                }
            }

            var sparePartsHtml = "";
            if (device.SparePartsUsed != null && device.SparePartsUsed.Any())
            {
                sparePartsHtml = "<div style='margin-top: 1mm;'>";
                foreach (var s in device.SparePartsUsed)
                {
                    sparePartsHtml += $@"
                        <div style='display: flex; justify-content: space-between; padding: 0.5mm 0; border-bottom: 1px dotted #ccc; font-size: 8px;'>
                            <span>{s.Part?.PartName} x{s.Quantity}</span>
                            <span>{(s.Price * s.Quantity):N0} ج.م</span>
                        </div>";
                }
                sparePartsHtml += "</div>";
            }

            var html = $@"<!DOCTYPE html>
<html dir='rtl'>
<head>
    <meta charset='UTF-8'>
    <title>طباعة جهاز</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; margin: 0; padding: 0; background: #fff; }}
        .print-80mm {{ width: 80mm; padding: 2mm; margin: 0 auto; }}
        .header {{ text-align: center; border-bottom: 1px solid #4361ee; padding-bottom: 1mm; margin-bottom: 2mm; }}
        .title {{ font-size: 14px; font-weight: bold; color: #4361ee; }}
        .section-title {{ font-weight: bold; color: #4361ee; margin: 2mm 0 1mm 0; font-size: 10px; }}
        .info-row {{ display: flex; justify-content: space-between; padding: 0.5mm 0; border-bottom: 1px dotted #eee; font-size: 9px; }}
        .info-label {{ font-weight: bold; color: #333; }}
        .footer {{ margin-top: 2mm; text-align: center; font-size: 7px; color: #666; border-top: 1px solid #eee; padding-top: 1mm; }}
        .badge {{ display: inline-block; padding: 0.5mm 1mm; border-radius: 1mm; color: white; font-size: 7px; }}
    </style>
</head>
<body>
    <div class='print-80mm'>
        <div class='header'>
            <div class='title'>{shopName}</div>
            <div style='font-size: 8px;'>مركز صيانة معتمد</div>
            <div style='font-size: 7px;'>{shopPhone}</div>
        </div>

        <div class='section-title'>📱 بيانات الجهاز</div>
        <div class='info-row'><span class='info-label'>كود:</span><span>{device.DeviceCode}</span></div>
        <div class='info-row'><span class='info-label'>العميل:</span><span>{device.CustomerName}</span></div>
        <div class='info-row'><span class='info-label'>الهاتف:</span><span>{device.CustomerPhone}</span></div>
        <div class='info-row'><span class='info-label'>الجهاز:</span><span>{device.DeviceBrand} {device.DeviceModel}</span></div>
        <div class='info-row'><span class='info-label'>المهندس:</span><span>{(device.Technician != null ? device.Technician.Username : "غير معين")}</span></div>
        <div class='info-row'><span class='info-label'>التاريخ:</span><span>{device.ReceivedDate:yyyy/MM/dd}</span></div>
        <div class='info-row'><span class='info-label'>الحالة:</span><span class='badge' style='background: {GetStatusColorForPDF(device.Status ?? "مستلم")};'>{device.Status}</span></div>

        <div class='section-title'>⚠️ العيب</div>
        <div style='background: #fff5f5; padding: 1mm; border-radius: 1mm; margin: 1mm 0; font-size: 8px; word-wrap: break-word;'>
            {device.ReportedIssue}
        </div>

        <div class='section-title'>💰 المالية</div>
        <div class='info-row'><span class='info-label'>تقديري:</span><span>{(device.EstimatedCost?.ToString("N0") ?? "0")} ج.م</span></div>
        <div class='info-row'><span class='info-label'>نهائي:</span><span>{(device.FinalCost?.ToString("N0") ?? "0")} ج.م</span></div>
        <div class='info-row'><span class='info-label'>مدفوع:</span><span>{device.AdvancePayment:N0} ج.م</span></div>
        <div class='info-row' style='font-weight: bold; color: {(device.RemainingAmount > 0 ? "#dc3545" : "#28a745")};'>
            <span class='info-label'>متبقي:</span><span>{device.RemainingAmount:N0} ج.م</span>
        </div>

        {(device.SparePartsUsed != null && device.SparePartsUsed.Any() ? $@"
        <div class='section-title'>🔧 قطع الغيار</div>
        {sparePartsHtml}
        " : "")}

        <div class='section-title'>📋 السجل</div>
        {statusHistoryHtml}

        <div class='footer'>
            <div>{now:yyyy/MM/dd HH:mm}</div>
            <div>شكراً لتعاملكم معنا 🌟</div>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        // ========== إنشاء PDF للتقرير الشامل ==========
        [HttpGet]
        public async Task<IActionResult> GenerateAllDevicesPDF(string status = "all", bool showDeleted = false)
        {
            try
            {
                var query = _context.RepairDevices
                    .Include(d => d.Technician)
                    .Where(d => d.IsDeleted == showDeleted)
                    .AsQueryable();

                if (status != "all" && status != "overdue")
                    query = query.Where(d => d.Status == status);
                else if (status == "overdue")
                {
                    var now = DateTime.Now;
                    var devices = await query.ToListAsync();
                    var overdueDevices = devices.Where(d =>
                        d.Status != "تم التسليم" && d.PromisedDate.HasValue && d.PromisedDate.Value < now).ToList();
                    var html = GenerateAllDevicesPDFHtml(overdueDevices, "المتأخرة", now);
                    return Json(new { success = true, html });
                }

                var allDevices = await query.OrderByDescending(d => d.ReceivedDate).Take(50).ToListAsync();
                var nowDate = DateTime.Now;
                var statusTitle = status == "all" ? "جميع الأجهزة" : status;
                var htmlResult = GenerateAllDevicesPDFHtml(allDevices, statusTitle, nowDate);
                return Json(new { success = true, html = htmlResult });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateAllDevicesPDF");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GenerateAllDevicesPDFHtml(List<RepairDevice> devices, string title, DateTime now)
        {
            var shopName = "📱 Mobile Shop";
            var shopPhone = "📞 01064211484";

            var devicesHtml = "";
            foreach (var d in devices.Take(30))
            {
                var isOverdue = d.Status != "تم التسليم" && d.PromisedDate.HasValue && d.PromisedDate.Value < now;
                var overdueMark = isOverdue ? " ⚠️" : "";

                devicesHtml += $@"
                    <div style='margin: 1mm 0; padding: 1mm; border-bottom: 1px solid #eee;'>
                        <div style='display: flex; justify-content: space-between; font-size: 8px;'>
                            <span style='font-weight: bold;'>{d.CustomerName}</span>
                            <span style='color: #666;'>{d.DeviceCode}</span>
                        </div>
                        <div style='display: flex; justify-content: space-between; font-size: 7px; margin-top: 0.5mm;'>
                            <span>{d.DeviceBrand} {d.DeviceModel}</span>
                            <span style='background: {GetStatusColorForPDF(d.Status ?? "مستلم")}; color: white; padding: 0.5mm 1mm; border-radius: 1mm;'>{d.Status}{overdueMark}</span>
                        </div>
                        <div style='display: flex; justify-content: space-between; font-size: 7px; margin-top: 0.5mm;'>
                            <span>📞 {d.CustomerPhone}</span>
                            <span>💰 {(d.EstimatedCost?.ToString("N0") ?? "0")} ج.م</span>
                        </div>
                    </div>";
            }

            if (devices.Count > 30)
            {
                devicesHtml += $"<div style='text-align: center; padding: 1mm; color: #666; font-size: 7px;'>... و {devices.Count - 30} أجهزة أخرى</div>";
            }

            var totalEstimated = devices.Sum(d => d.EstimatedCost ?? 0);
            var totalPaid = devices.Sum(d => d.AdvancePayment);

            var html = $@"<!DOCTYPE html>
<html dir='rtl'>
<head>
    <meta charset='UTF-8'>
    <title>تقرير شامل</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; margin: 0; padding: 0; background: #fff; }}
        .print-80mm {{ width: 80mm; padding: 2mm; margin: 0 auto; }}
        .header {{ text-align: center; border-bottom: 1px solid #4361ee; padding-bottom: 1mm; margin-bottom: 2mm; }}
        .title {{ font-size: 14px; font-weight: bold; color: #4361ee; }}
        .stats {{ display: flex; justify-content: space-around; margin: 2mm 0; padding: 1mm; background: #f8f9fa; border-radius: 1mm; }}
        .stat-item {{ text-align: center; }}
        .stat-value {{ font-size: 12px; font-weight: bold; }}
        .stat-label {{ font-size: 6px; color: #666; }}
        .footer {{ margin-top: 2mm; text-align: center; font-size: 6px; color: #666; border-top: 1px solid #eee; padding-top: 1mm; }}
    </style>
</head>
<body>
    <div class='print-80mm'>
        <div class='header'>
            <div class='title'>{shopName}</div>
            <div style='font-size: 8px;'>تقرير {title}</div>
            <div style='font-size: 7px;'>{shopPhone}</div>
        </div>

        <div class='stats'>
            <div class='stat-item'>
                <div class='stat-value'>{devices.Count}</div>
                <div class='stat-label'>إجمالي</div>
            </div>
            <div class='stat-item'>
                <div class='stat-value'>{totalEstimated:N0}</div>
                <div class='stat-label'>تقديري</div>
            </div>
            <div class='stat-item'>
                <div class='stat-value'>{totalPaid:N0}</div>
                <div class='stat-label'>مدفوع</div>
            </div>
        </div>

        <div style='margin-top: 2mm;'>
            {devicesHtml}
        </div>

        <div class='footer'>
            <div>{now:yyyy/MM/dd HH:mm}</div>
            <div>شكراً لتعاملكم معنا 🌟</div>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        // ========== طباعة فاتورة ==========
        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            try
            {
                var device = await _context.RepairDevices
                    .Include(d => d.Creator)
                    .Include(d => d.Technician)
                    .Include(d => d.SparePartsUsed).ThenInclude(s => s.Part)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (device == null)
                    return NotFound();

                ViewBag.Device = device;
                ViewBag.Username = GetCurrentUsername();
                return View("Invoice");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PrintInvoice");
                return NotFound();
            }
        }

        // ========== دوال مساعدة ==========
        private string GetStatusColor(string status)
        {
            return status switch
            {
                "مستلم" => "badge-info",
                "قيد الصيانة" => "badge-warning",
                "بانتظار قطع غيار" => "badge-danger",
                "تم الاصلاح" => "badge-success",
                "تم التسليم" => "badge-secondary",
                _ => "badge-secondary"
            };
        }

        private string GetStatusIcon(string status)
        {
            return status switch
            {
                "مستلم" => "fa-box-open",
                "قيد الصيانة" => "fa-tools",
                "بانتظار قطع غيار" => "fa-clock",
                "تم الاصلاح" => "fa-check-circle",
                "تم التسليم" => "fa-check-double",
                _ => "fa-question"
            };
        }

        private string GetStatusColorForPDF(string status)
        {
            return status switch
            {
                "مستلم" => "#17a2b8",
                "قيد الصيانة" => "#ffc107",
                "بانتظار قطع غيار" => "#dc3545",
                "تم الاصلاح" => "#28a745",
                "تم التسليم" => "#6c757d",
                _ => "#6c757d"
            };
        }

        private string GetTimeAgo(DateTime? dateTime)
        {
            try
            {
                if (!dateTime.HasValue)
                    return "-";

                var timeSpan = DateTime.Now - dateTime.Value;

                if (timeSpan.TotalMinutes < 1)
                    return "الآن";
                if (timeSpan.TotalMinutes < 60)
                    return $"منذ {Math.Floor(timeSpan.TotalMinutes)} دقيقة";
                if (timeSpan.TotalHours < 24)
                    return $"منذ {Math.Floor(timeSpan.TotalHours)} ساعة";
                if (timeSpan.TotalDays < 30)
                    return $"منذ {Math.Floor(timeSpan.TotalDays)} يوم";
                if (timeSpan.TotalDays < 365)
                    return $"منذ {Math.Floor(timeSpan.TotalDays / 30)} شهر";

                return $"منذ {Math.Floor(timeSpan.TotalDays / 365)} سنة";
            }
            catch
            {
                return "-";
            }
        }
    }

    // ========== ViewModels إضافية ==========
    public class CustomerHistoryViewModel
    {
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public List<CustomerDeviceHistory> Devices { get; set; }
        public int TotalDevices { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime FirstRepair { get; set; }
        public DateTime LastRepair { get; set; }
    }

    public class CustomerDeviceHistory
    {
        public int Id { get; set; }
        public string DeviceCode { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceSerial { get; set; }
        public string ReportedIssue { get; set; }
        public DateTime ReceivedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public string Status { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? FinalCost { get; set; }
        public decimal AdvancePayment { get; set; }
        public decimal RemainingAmount { get; set; }
        public string TechnicianName { get; set; }
        public List<DeviceStatusPath> StatusPath { get; set; }
    }

    public class DeviceStatusPath
    {
        public string Status { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
        public string Notes { get; set; }
        public int DaysInStatus { get; set; }
    }

    public class AssignTechnicianViewModel
    {
        public int DeviceId { get; set; }
        public int TechnicianId { get; set; }
    }

    public class SendBulkWhatsAppViewModel
    {
        public List<string> PhoneNumbers { get; set; }
        public string Message { get; set; }
    }

    public class BulkWhatsAppResult
    {
        public string Phone { get; set; }
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
    }
}