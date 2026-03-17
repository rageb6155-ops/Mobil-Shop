using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using MobileShopSystem.ViewModels;

namespace MobileShopSystem.Controllers
{
    public class CustomersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomersController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
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

        // ===== لوحة التحكم الرئيسية =====
        public IActionResult Index()
        {
            ViewBag.Username = GetCurrentUsername();
            return View();
        }

        // ===== الحصول على إحصائيات لوحة التحكم =====
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.Transactions)
                    .Where(c => !c.IsDeleted)
                    .ToListAsync();

                var now = DateTime.Now;
                var nextWeek = now.AddDays(7);

                // إحصائيات عامة
                var totalCustomers = customers.Count;
                var customersWithDebt = customers.Count(c => c.CurrentDebt > 0);
                var totalDebts = customers.Sum(c => c.CurrentDebt);
                var totalPaid = customers.Sum(c => c.Transactions.Sum(t => t.PaidAmount));

                // الديون المتأخرة
                var overdueTransactions = await _context.CustomerTransactions
                    .Where(t => t.Status == "نشط" && t.DueDate < now)
                    .CountAsync();

                // الأقساط القادمة
                var upcomingInstallments = await _context.Installments
                    .Where(i => !i.IsPaid && i.DueDate <= nextWeek && i.DueDate >= now)
                    .CountAsync();

                // أكبر المدينين (Top 5)
                var topDebtors = customers
                    .OrderByDescending(c => c.CurrentDebt)
                    .Take(5)
                    .Select(c => new CustomerViewModel
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        CurrentDebt = c.CurrentDebt,
                        PhoneNumber = c.PhoneNumber,
                        DebtPercentage = c.MaxDebtLimit.HasValue ? (c.CurrentDebt / c.MaxDebtLimit.Value) * 100 : 0
                    }).ToList();

                // أفضل المسددين (أعلى نسبة دفع)
                var bestPayers = customers
                    .Where(c => c.Transactions.Any())
                    .Select(c => new CustomerViewModel
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        TotalPaid = c.Transactions.Sum(t => t.PaidAmount),
                        TotalTransactions = c.Transactions.Sum(t => t.Amount),
                        DebtPercentage = c.Transactions.Sum(t => t.Amount) > 0 ?
                            (c.Transactions.Sum(t => t.PaidAmount) / c.Transactions.Sum(t => t.Amount)) * 100 : 0,
                        PhoneNumber = c.PhoneNumber
                    })
                    .OrderByDescending(c => c.DebtPercentage)
                    .Take(5)
                    .ToList();

                var stats = new DashboardStatsViewModel
                {
                    TotalCustomers = totalCustomers,
                    ActiveCustomers = customers.Count(c => c.IsActive),
                    CustomersWithDebt = customersWithDebt,
                    TotalDebts = totalDebts,
                    TotalPaid = totalPaid,
                    RemainingDebts = totalDebts - totalPaid,
                    OverdueTransactions = overdueTransactions,
                    UpcomingInstallments = upcomingInstallments,
                    TopDebtors = topDebtors,
                    BestPayers = bestPayers
                };

                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== الحصول على جميع العملاء مع تحسينات =====
        [HttpGet]
        public async Task<IActionResult> GetAllCustomers(string? search, bool showDeleted = false)
        {
            try
            {
                var query = _context.Customers
                    .Include(c => c.Creator)
                    .Include(c => c.Transactions)
                    .Where(c => c.IsDeleted == showDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim();
                    query = query.Where(c =>
                        c.FullName.Contains(search) ||
                        c.PhoneNumber.Contains(search) ||
                        c.CustomerCode.Contains(search) ||
                        c.IDNumber.Contains(search));
                }

                var customers = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var result = customers.Select(c =>
                {
                    var totalTransactions = c.Transactions.Sum(t => t.Amount);
                    var totalPaid = c.Transactions.Sum(t => t.PaidAmount);
                    var debtPercentage = totalTransactions > 0 ? (totalPaid / totalTransactions) * 100 : 0;

                    // تصنيف العميل
                    string category = "عادي";
                    if (c.CurrentDebt > 0)
                    {
                        if (c.MaxDebtLimit.HasValue && c.CurrentDebt > c.MaxDebtLimit.Value * 0.8m)
                            category = "⚠️ خطر (دين مرتفع)";
                        else if (c.CurrentDebt > 1000)
                            category = "مدين";
                        else
                            category = "مدين بسيط";
                    }
                    else if (debtPercentage > 80)
                        category = "🌟 ممتاز";

                    // رسالة تحذير
                    string warning = "";
                    if (c.MaxDebtLimit.HasValue && c.CurrentDebt > c.MaxDebtLimit.Value * 0.9m)
                        warning = "⚠️ تحذير: اقتربت من حد الدين الأقصى!";
                    else if (c.MaxDebtLimit.HasValue && c.CurrentDebt > c.MaxDebtLimit.Value * 0.7m)
                        warning = "⚠️ تنبيه: الدين مرتفع";

                    return new CustomerViewModel
                    {
                        Id = c.Id,
                        CustomerCode = c.CustomerCode,
                        FullName = c.FullName,
                        PhoneNumber = c.PhoneNumber,
                        AlternativePhone = c.AlternativePhone,
                        IDNumber = c.IDNumber,
                        Address = c.Address,
                        Email = c.Email,
                        Notes = c.Notes,
                        CustomerType = c.CustomerType,
                        MaxDebtLimit = c.MaxDebtLimit,
                        CurrentDebt = c.CurrentDebt,
                        CreatedByName = c.Creator != null ? c.Creator.Username : "Unknown",
                        CreatedAt = c.CreatedAt,
                        IsActive = c.IsActive,
                        TransactionsCount = c.Transactions.Count,
                        TotalTransactions = totalTransactions,
                        TotalPaid = totalPaid,
                        CustomerCategory = category,
                        DebtPercentage = debtPercentage,
                        WarningMessage = warning
                    };
                }).ToList();

                return Json(new { success = true, customers = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== البحث الفوري عن العملاء =====
        [HttpGet]
        public async Task<IActionResult> SearchCustomers(string term)
        {
            try
            {
                if (string.IsNullOrEmpty(term))
                    return Json(new { success = true, customers = new List<CustomerViewModel>() });

                var customers = await _context.Customers
                    .Include(c => c.Transactions)
                    .Where(c => !c.IsDeleted &&
                               (c.FullName.Contains(term) ||
                                c.PhoneNumber.Contains(term) ||
                                c.CustomerCode.Contains(term)))
                    .OrderBy(c => c.FullName)
                    .Take(10)
                    .Select(c => new CustomerViewModel
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        PhoneNumber = c.PhoneNumber,
                        CustomerCode = c.CustomerCode,
                        CurrentDebt = c.CurrentDebt
                    })
                    .ToListAsync();

                return Json(new { success = true, customers = customers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== إضافة عميل جديد =====
        [HttpPost]
        public async Task<IActionResult> AddCustomer([FromBody] CreateCustomerViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                // التحقق من صحة البيانات
                if (string.IsNullOrEmpty(model.FullName))
                    return Json(new { success = false, message = "الرجاء إدخال اسم العميل" });

                if (string.IsNullOrEmpty(model.PhoneNumber))
                    return Json(new { success = false, message = "الرجاء إدخال رقم الهاتف" });

                // التحقق من تكرار رقم الهاتف
                var existingPhone = await _context.Customers
                    .AnyAsync(c => c.PhoneNumber == model.PhoneNumber && !c.IsDeleted);

                if (existingPhone)
                    return Json(new { success = false, message = "رقم الهاتف موجود بالفعل" });

                // إنشاء كود عميل فريد
                var lastCustomer = await _context.Customers
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                int nextId = (lastCustomer?.Id ?? 0) + 1;
                string customerCode = "CUS-" + nextId.ToString("D6");

                var customer = new Customer
                {
                    CustomerCode = customerCode,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    AlternativePhone = model.AlternativePhone,
                    IDNumber = model.IDNumber,
                    Address = model.Address,
                    Email = model.Email,
                    Notes = model.Notes,
                    CustomerType = model.CustomerType,
                    MaxDebtLimit = model.MaxDebtLimit,
                    CreatedBy = userId.Value
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // تسجيل العملية
                await LogActivity(userId.Value, "إضافة عميل", $"تم إضافة العميل {customer.FullName}");

                return Json(new { success = true, message = "✅ تم إضافة العميل بنجاح", customerId = customer.Id });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ خطأ في قاعدة البيانات: " + innerException });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== تحديث بيانات عميل =====
        [HttpPost]
        public async Task<IActionResult> UpdateCustomer([FromBody] CustomerViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var customer = await _context.Customers.FindAsync(model.Id);
                if (customer == null)
                    return Json(new { success = false, message = "العميل غير موجود" });

                // التحقق من تكرار رقم الهاتف
                var existingPhone = await _context.Customers
                    .AnyAsync(c => c.PhoneNumber == model.PhoneNumber && c.Id != model.Id && !c.IsDeleted);

                if (existingPhone)
                    return Json(new { success = false, message = "رقم الهاتف موجود بالفعل" });

                var oldName = customer.FullName;

                customer.FullName = model.FullName;
                customer.PhoneNumber = model.PhoneNumber;
                customer.AlternativePhone = model.AlternativePhone;
                customer.IDNumber = model.IDNumber;
                customer.Address = model.Address;
                customer.Email = model.Email;
                customer.Notes = model.Notes;
                customer.CustomerType = model.CustomerType;
                customer.MaxDebtLimit = model.MaxDebtLimit;
                customer.IsActive = model.IsActive;
                customer.UpdatedBy = userId;
                customer.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await LogActivity(userId.Value, "تحديث عميل", $"تم تحديث بيانات العميل {oldName}");

                return Json(new { success = true, message = "✅ تم تحديث بيانات العميل بنجاح" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ خطأ في قاعدة البيانات: " + innerException });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== حذف عميل (ناعم) =====
        [HttpPost]
        public async Task<IActionResult> SoftDeleteCustomer(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var customer = await _context.Customers
                    .Include(c => c.Transactions)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                    return Json(new { success = false, message = "العميل غير موجود" });

                // التحقق من وجود ديون نشطة
                if (customer.CurrentDebt > 0)
                    return Json(new { success = false, message = $"❌ لا يمكن حذف عميل عليه ديون مستحقة (المتبقي: {customer.CurrentDebt} ج.م)" });

                customer.IsDeleted = true;
                customer.DeletedBy = userId;
                customer.DeletedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await LogActivity(userId.Value, "حذف عميل", $"تم حذف العميل {customer.FullName}");

                return Json(new { success = true, message = "✅ تم حذف العميل بنجاح" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ خطأ في قاعدة البيانات: " + innerException });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== حذف عميل نهائي (مع كل المعاملات) =====
        [HttpPost]
        public async Task<IActionResult> HardDeleteCustomer(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var customer = await _context.Customers
                    .Include(c => c.Transactions)
                        .ThenInclude(t => t.Installments)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                    return Json(new { success = false, message = "العميل غير موجود" });

                // حذف جميع الأقساط المرتبطة
                foreach (var transaction in customer.Transactions)
                {
                    if (transaction.Installments.Any())
                    {
                        _context.Installments.RemoveRange(transaction.Installments);
                    }
                }

                // حذف جميع المعاملات
                _context.CustomerTransactions.RemoveRange(customer.Transactions);

                // حذف العميل
                _context.Customers.Remove(customer);

                await _context.SaveChangesAsync();

                await LogActivity(userId.Value, "حذف نهائي", $"تم حذف العميل {customer.FullName} نهائياً مع كل معاملاته");

                return Json(new { success = true, message = "✅ تم حذف العميل نهائياً بنجاح" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ خطأ في قاعدة البيانات: " + innerException });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== استعادة عميل محذوف =====
        [HttpPost]
        public async Task<IActionResult> RestoreCustomer(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);

                if (customer == null)
                    return Json(new { success = false, message = "العميل غير موجود" });

                customer.IsDeleted = false;
                customer.DeletedBy = null;
                customer.DeletedAt = null;
                customer.UpdatedBy = userId;
                customer.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await LogActivity(userId.Value, "استعادة عميل", $"تم استعادة العميل {customer.FullName}");

                return Json(new { success = true, message = "✅ تم استعادة العميل بنجاح" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ خطأ في قاعدة البيانات: " + innerException });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== الحصول على تفاصيل عميل مع تحليلات =====
        [HttpGet]
        public async Task<IActionResult> GetCustomerDetails(int id)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.Creator)
                    .Include(c => c.Transactions)
                        .ThenInclude(t => t.Sale)
                    .Include(c => c.Transactions)
                        .ThenInclude(t => t.Installments)
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (customer == null)
                    return Json(new { success = false, message = "العميل غير موجود" });

                var now = DateTime.Now;
                var totalTransactions = customer.Transactions.Sum(t => t.Amount);
                var totalPaid = customer.Transactions.Sum(t => t.PaidAmount);
                var paidPercentage = totalTransactions > 0 ? (totalPaid / totalTransactions) * 100 : 0;

                // تصنيف العميل
                string category = "عادي";
                if (customer.CurrentDebt > 0)
                {
                    if (customer.MaxDebtLimit.HasValue && customer.CurrentDebt > customer.MaxDebtLimit.Value * 0.8m)
                        category = "خطر (دين مرتفع)";
                    else if (customer.CurrentDebt > 1000)
                        category = "مدين";
                    else
                        category = "مدين بسيط";
                }
                else if (paidPercentage > 80)
                    category = "ممتاز";

                // حالة الدين
                string debtStatus = "لا يوجد ديون";
                if (customer.CurrentDebt > 0)
                {
                    if (customer.MaxDebtLimit.HasValue)
                    {
                        var percentage = (customer.CurrentDebt / customer.MaxDebtLimit.Value) * 100;
                        debtStatus = $"دين: {percentage:F1}% من الحد المسموح";
                    }
                    else
                    {
                        debtStatus = $"دين مستحق: {customer.CurrentDebt} ج.م";
                    }
                }

                var viewModel = new CustomerSummaryViewModel
                {
                    Id = customer.Id,
                    FullName = customer.FullName,
                    PhoneNumber = customer.PhoneNumber,
                    AlternativePhone = customer.AlternativePhone,
                    IDNumber = customer.IDNumber,
                    Address = customer.Address,
                    Email = customer.Email,
                    Notes = customer.Notes,
                    CustomerType = customer.CustomerType,
                    MaxDebtLimit = customer.MaxDebtLimit,
                    CurrentDebt = customer.CurrentDebt,
                    TotalDebts = totalTransactions,
                    TotalPaid = totalPaid,
                    PaidPercentage = paidPercentage,
                    ActiveTransactions = customer.Transactions.Count(t => t.Status == "نشط"),
                    OverdueTransactions = customer.Transactions.Count(t => t.Status == "متأخر" ||
                        (t.Status == "نشط" && t.DueDate < now)),
                    CompletedTransactions = customer.Transactions.Count(t => t.Status == "مكتمل"),
                    AverageDebt = customer.Transactions.Where(t => t.TransactionType == "دين")
                        .Average(t => (decimal?)t.Amount) ?? 0,
                    CustomerCategory = category,
                    DebtStatus = debtStatus,
                    RecentTransactions = customer.Transactions
                        .OrderByDescending(t => t.TransactionDate)
                        .Take(20)
                        .Select(t =>
                        {
                            var isOverdue = t.Status == "نشط" && t.DueDate < now;
                            var overdueDays = isOverdue ? (now - t.DueDate.Value).Days : 0;
                            var paidPerc = t.Amount > 0 ? (t.PaidAmount / t.Amount) * 100 : 0;
                            var paidInstallments = t.Installments.Count(i => i.IsPaid);
                            var totalInstallments = t.Installments.Count;

                            return new CustomerTransactionViewModel
                            {
                                Id = t.Id,
                                TransactionNumber = t.TransactionNumber,
                                TransactionType = t.TransactionType,
                                TransactionDate = t.TransactionDate,
                                Amount = t.Amount,
                                PaidAmount = t.PaidAmount,
                                RemainingAmount = t.RemainingAmount,
                                PaidPercentage = paidPerc,
                                DueDate = t.DueDate,
                                IsOverdue = isOverdue,
                                OverdueDays = overdueDays,
                                Status = isOverdue ? "متأخر" : t.Status,
                                IsInstallment = t.IsInstallment,
                                InstallmentCount = t.InstallmentCount,
                                InstallmentPaidCount = t.InstallmentPaidCount,
                                InstallmentProgress = totalInstallments > 0 ? (paidInstallments * 100 / totalInstallments) : 0,
                                SaleNumber = t.Sale != null ? t.Sale.SaleNumber : null,
                                CreatedByName = t.Creator != null ? t.Creator.Username : "Unknown",
                                Installments = t.Installments.Select(i => new InstallmentViewModel
                                {
                                    Id = i.Id,
                                    InstallmentNumber = i.InstallmentNumber,
                                    DueDate = i.DueDate,
                                    Amount = i.Amount,
                                    PaidAmount = i.PaidAmount,
                                    PaidDate = i.PaidDate,
                                    IsPaid = i.IsPaid,
                                    IsOverdue = !i.IsPaid && i.DueDate < now,
                                    OverdueDays = !i.IsPaid && i.DueDate < now ? (now - i.DueDate).Days : 0,
                                    PaymentMethod = i.PaymentMethod
                                }).ToList()
                            };
                        }).ToList()
                };

                return Json(new { success = true, customer = viewModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== إضافة معاملة جديدة (مع تحسينات الدفع) =====
        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] CreateTransactionViewModel model)
        {
            // بدء معاملة قاعدة بيانات
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var customer = await _context.Customers.FindAsync(model.CustomerId);
                if (customer == null)
                    return Json(new { success = false, message = "العميل غير موجود" });

                // التحقق من صحة المبلغ
                if (model.Amount <= 0)
                    return Json(new { success = false, message = "❌ المبلغ يجب أن يكون أكبر من صفر" });

                // التحقق من حد الدين الأقصى (مع إمكانية التجاوز)
                bool limitExceeded = false;
                decimal newTotalDebt = customer.CurrentDebt;
                decimal limitPercentage = 0;

                if (model.TransactionType == "دين" && customer.MaxDebtLimit.HasValue)
                {
                    newTotalDebt = customer.CurrentDebt + model.Amount;
                    if (newTotalDebt > customer.MaxDebtLimit.Value)
                    {
                        limitExceeded = true;
                        limitPercentage = (newTotalDebt / customer.MaxDebtLimit.Value) * 100;
                    }
                }

                // إنشاء رقم معاملة فريد
                var lastTransaction = await _context.CustomerTransactions
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync();

                int nextId = (lastTransaction?.Id ?? 0) + 1;
                string transactionNumber = "TRX-" + nextId.ToString("D8");

                decimal remainingAmount = 0;
                string message = "";
                CustomerTransaction? customerTransaction = null;

                // معالجة حسب نوع المعاملة
                if (model.TransactionType == "دين")
                {
                    remainingAmount = model.Amount - model.PaidAmount;

                    var newTransaction = new CustomerTransaction
                    {
                        TransactionNumber = transactionNumber,
                        CustomerId = model.CustomerId,
                        SaleId = model.SaleId,
                        TransactionType = model.TransactionType,
                        Amount = model.Amount,
                        PaidAmount = model.PaidAmount,
                        RemainingAmount = remainingAmount,
                        DueDate = model.DueDate,
                        Notes = model.Notes,
                        CreatedBy = userId.Value,
                        IsInstallment = model.IsInstallment,
                        Status = remainingAmount > 0 ? "نشط" : "مكتمل"
                    };

                    customer.CurrentDebt += remainingAmount;

                    // رسالة أساسية
                    message = $"✅ تم إضافة دين جديد بمبلغ {model.Amount} ج.م";

                    if (model.PaidAmount > 0)
                    {
                        message += $"، وتم دفع {model.PaidAmount} ج.م مقدمًا";
                    }
                    if (remainingAmount > 0)
                    {
                        message += $"، المتبقي: {remainingAmount} ج.م";
                    }

                    // إضافة تحذير إذا تم تجاوز الحد
                    if (limitExceeded)
                    {
                        message += $"\n\n⚠️ تحذير: تم تجاوز حد الدين الأقصى! (الحد: {customer.MaxDebtLimit.Value} ج.م، النسبة: {limitPercentage:F1}%)";

                        // إضافة ملاحظة في المعاملة
                        newTransaction.Notes = (newTransaction.Notes + $"\n⚠️ تجاوز حد الدين: {limitPercentage:F1}%").Trim();
                    }

                    _context.CustomerTransactions.Add(newTransaction);
                    customerTransaction = newTransaction;
                }
                else if (model.TransactionType == "دفعة")
                {
                    // التحقق من أن المبلغ المدفوع لا يتجاوز الدين الكلي
                    if (model.Amount > customer.CurrentDebt)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"❌ المبلغ المدفوع ({model.Amount} ج.م) أكبر من الدين المستحق ({customer.CurrentDebt} ج.م)"
                        });
                    }

                    // البحث عن الديون النشطة للعميل
                    var activeDebts = await _context.CustomerTransactions
                        .Where(t => t.CustomerId == model.CustomerId &&
                                   t.TransactionType == "دين" &&
                                   t.Status == "نشط" &&
                                   t.RemainingAmount > 0)
                        .OrderBy(t => t.DueDate) // الأقدم أولاً
                        .ToListAsync();

                    if (!activeDebts.Any())
                    {
                        return Json(new { success = false, message = "❌ لا يوجد ديون مستحقة على هذا العميل" });
                    }

                    decimal remainingPayment = model.Amount;
                    decimal totalPaid = 0;
                    List<string> paidDetails = new List<string>();

                    // توزيع المبلغ على الديون (الأقدم أولاً)
                    foreach (var debt in activeDebts)
                    {
                        if (remainingPayment <= 0) break;

                        decimal amountToPay = Math.Min(remainingPayment, debt.RemainingAmount);

                        debt.PaidAmount += amountToPay;
                        debt.RemainingAmount -= amountToPay;
                        debt.Status = debt.RemainingAmount <= 0 ? "مكتمل" : "نشط";

                        remainingPayment -= amountToPay;
                        totalPaid += amountToPay;

                        paidDetails.Add($"دين {debt.TransactionNumber}: {amountToPay} ج.م");

                        // تحديث دين العميل
                        customer.CurrentDebt -= amountToPay;
                    }

                    // تسجيل معاملة الدفعة
                    var paymentTransaction = new CustomerTransaction
                    {
                        TransactionNumber = transactionNumber,
                        CustomerId = model.CustomerId,
                        SaleId = model.SaleId,
                        TransactionType = "دفعة",
                        Amount = model.Amount,
                        PaidAmount = totalPaid,
                        RemainingAmount = 0,
                        DueDate = DateTime.Now,
                        Notes = $"تم توزيع المبلغ على الديون: {string.Join("، ", paidDetails)}",
                        CreatedBy = userId.Value,
                        IsInstallment = false,
                        Status = "مكتمل"
                    };

                    _context.CustomerTransactions.Add(paymentTransaction);
                    customerTransaction = paymentTransaction;

                    var remainingAfterPayment = activeDebts.Sum(d => d.RemainingAmount);
                    message = $"✅ تم دفع {totalPaid} ج.م بنجاح";

                    if (remainingAfterPayment > 0)
                    {
                        message += $"، المتبقي على العميل: {remainingAfterPayment} ج.م";
                    }
                    else
                    {
                        message += " 🎉 تم سداد جميع الديون!";
                    }
                }
                else if (model.TransactionType == "تسوية")
                {
                    remainingAmount = Math.Abs(model.Amount - model.PaidAmount);

                    var newTransaction = new CustomerTransaction
                    {
                        TransactionNumber = transactionNumber,
                        CustomerId = model.CustomerId,
                        SaleId = model.SaleId,
                        TransactionType = "تسوية",
                        Amount = Math.Abs(model.Amount),
                        PaidAmount = model.PaidAmount,
                        RemainingAmount = 0,
                        DueDate = model.DueDate,
                        Notes = model.Notes,
                        CreatedBy = userId.Value,
                        IsInstallment = false,
                        Status = "مكتمل"
                    };

                    if (model.Amount > 0)
                    {
                        customer.CurrentDebt += model.Amount;
                        message = $"✅ تمت تسوية بزيادة {model.Amount} ج.م، إجمالي الدين الآن: {customer.CurrentDebt} ج.م";
                    }
                    else if (model.Amount < 0)
                    {
                        var absAmount = Math.Abs(model.Amount);
                        customer.CurrentDebt = Math.Max(0, customer.CurrentDebt - absAmount);
                        message = $"✅ تمت تسوية بنقص {absAmount} ج.م، إجمالي الدين الآن: {customer.CurrentDebt} ج.م";
                    }

                    _context.CustomerTransactions.Add(newTransaction);
                    customerTransaction = newTransaction;
                }

                await _context.SaveChangesAsync();

                // إذا كانت المعاملة مقسمة لأقساط (بعد حفظ المعاملة)
                if (model.IsInstallment && model.InstallmentCount.HasValue && model.InstallmentPeriod.HasValue && customerTransaction != null)
                {
                    var installmentAmount = model.Amount / model.InstallmentCount.Value;
                    var dueDate = model.DueDate ?? DateTime.Now;

                    for (int i = 1; i <= model.InstallmentCount.Value; i++)
                    {
                        var installmentDueDate = dueDate.AddDays(i * model.InstallmentPeriod.Value);
                        var installment = new Installment
                        {
                            TransactionId = customerTransaction.Id,
                            InstallmentNumber = i,
                            DueDate = installmentDueDate,
                            Amount = Math.Round(installmentAmount, 2)
                        };
                        _context.Installments.Add(installment);
                    }

                    await _context.SaveChangesAsync();
                    message += $"، مقسمة على {model.InstallmentCount.Value} أقساط";
                }

                await dbTransaction.CommitAsync();

                // جلب البيانات المحدثة للعميل
                var updatedCustomer = await _context.Customers
                    .Where(c => c.Id == model.CustomerId)
                    .Select(c => new
                    {
                        c.CurrentDebt,
                        c.FullName,
                        c.PhoneNumber,
                        c.MaxDebtLimit
                    })
                    .FirstOrDefaultAsync();

                // تسجيل النشاط
                await LogActivity(userId.Value, "إضافة معاملة",
                    $"تمت إضافة معاملة للعميل {updatedCustomer?.FullName}: {model.TransactionType} بمبلغ {model.Amount} ج.م");

                // تجهيز بيانات الفاتورة PDF
                var invoiceData = new
                {
                    transactionNumber = transactionNumber,
                    customerName = updatedCustomer?.FullName,
                    customerPhone = updatedCustomer?.PhoneNumber,
                    transactionType = model.TransactionType,
                    amount = model.Amount,
                    paidAmount = model.TransactionType == "دفعة" ? model.Amount : model.PaidAmount,
                    remainingAmount = updatedCustomer?.CurrentDebt ?? 0,
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    time = DateTime.Now.ToString("HH:mm:ss"),
                    notes = model.Notes,
                    createdBy = GetCurrentUsername(),
                    limitExceeded = limitExceeded,
                    limitPercentage = limitPercentage,
                    maxDebtLimit = updatedCustomer?.MaxDebtLimit,
                    isInstallment = model.IsInstallment,
                    installmentCount = model.InstallmentCount,
                    installmentPeriod = model.InstallmentPeriod
                };

                return Json(new
                {
                    success = true,
                    message = message,
                    transactionId = transactionNumber,
                    currentDebt = updatedCustomer?.CurrentDebt ?? 0,
                    limitExceeded = limitExceeded,
                    limitPercentage = limitPercentage,
                    maxDebtLimit = updatedCustomer?.MaxDebtLimit,
                    customerId = model.CustomerId,
                    invoice = invoiceData
                });
            }
            catch (DbUpdateException ex)
            {
                await dbTransaction.RollbackAsync();
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ خطأ في قاعدة البيانات: " + innerException });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== دفع قسط (مع تحسينات) =====
        [HttpPost]
        public async Task<IActionResult> PayInstallment([FromBody] PayInstallmentViewModel model)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var installment = await _context.Installments
                    .Include(i => i.Transaction)
                        .ThenInclude(t => t.Customer)
                    .FirstOrDefaultAsync(i => i.Id == model.InstallmentId);

                if (installment == null)
                    return Json(new { success = false, message = "القسط غير موجود" });

                if (installment.IsPaid)
                    return Json(new { success = false, message = "❌ هذا القسط مدفوع بالفعل" });

                if (model.Amount < installment.Amount)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"❌ المبلغ المدفوع ({model.Amount} ج.م) أقل من قيمة القسط ({installment.Amount} ج.م)"
                    });
                }

                // تحديث القسط
                installment.IsPaid = true;
                installment.PaidAmount = model.Amount;
                installment.PaidDate = DateTime.Now;
                installment.PaidBy = userId;
                installment.PaymentMethod = model.PaymentMethod;
                installment.Notes = model.Notes;

                // تحديث المعاملة
                var transaction = installment.Transaction;
                if (transaction != null)
                {
                    transaction.InstallmentPaidCount++;
                    transaction.PaidAmount += model.Amount;
                    transaction.RemainingAmount -= model.Amount;

                    // تحديث دين العميل
                    if (transaction.Customer != null)
                    {
                        transaction.Customer.CurrentDebt = Math.Max(0, transaction.Customer.CurrentDebt - model.Amount);
                    }

                    // التحقق من اكتمال جميع الأقساط
                    if (transaction.InstallmentPaidCount == transaction.InstallmentCount)
                    {
                        transaction.Status = "مكتمل";
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    // حساب التقدم
                    var progress = transaction.InstallmentCount > 0
                        ? (transaction.InstallmentPaidCount * 100 / transaction.InstallmentCount.Value)
                        : 0;

                    // رسالة تأكيد
                    var message = $"✅ تم دفع القسط رقم {installment.InstallmentNumber} بمبلغ {model.Amount} ج.م";

                    var remainingInstallments = (transaction.InstallmentCount ?? 0) - transaction.InstallmentPaidCount;
                    if (remainingInstallments > 0)
                    {
                        message += $"، تبقى {remainingInstallments} أقساط (التقدم: {progress}%)";
                    }
                    else
                    {
                        message += " 🎉 تم سداد جميع الأقساط!";
                    }

                    // تجهيز بيانات الفاتورة
                    var invoiceData = new
                    {
                        transactionNumber = transaction.TransactionNumber,
                        customerName = transaction.Customer?.FullName,
                        customerPhone = transaction.Customer?.PhoneNumber,
                        transactionType = "دفع قسط",
                        amount = model.Amount,
                        paidAmount = model.Amount,
                        remainingAmount = transaction.Customer?.CurrentDebt ?? 0,
                        date = DateTime.Now.ToString("yyyy-MM-dd"),
                        time = DateTime.Now.ToString("HH:mm:ss"),
                        installmentNumber = installment.InstallmentNumber,
                        totalInstallments = transaction.InstallmentCount,
                        progress = progress,
                        notes = $"دفع القسط رقم {installment.InstallmentNumber}",
                        createdBy = GetCurrentUsername()
                    };

                    return Json(new
                    {
                        success = true,
                        message = message,
                        currentDebt = transaction.Customer?.CurrentDebt ?? 0,
                        progress = progress,
                        invoice = invoiceData
                    });
                }

                return Json(new { success = false, message = "حدث خطأ غير متوقع" });
            }
            catch (DbUpdateException ex)
            {
                await dbTransaction.RollbackAsync();
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "❌ خطأ في قاعدة البيانات: " + innerException });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== الحصول على تقارير الديون المحسنة =====
        [HttpGet]
        public async Task<IActionResult> DebtReports(DateTime? fromDate, DateTime? toDate, int? customerId)
        {
            try
            {
                if (!fromDate.HasValue)
                    fromDate = DateTime.Now.AddMonths(-1);
                if (!toDate.HasValue)
                    toDate = DateTime.Now;

                var query = _context.CustomerTransactions
                    .Include(t => t.Customer)
                    .Include(t => t.Sale)
                    .Where(t => t.TransactionDate >= fromDate.Value && t.TransactionDate <= toDate.Value);

                if (customerId.HasValue)
                {
                    query = query.Where(t => t.CustomerId == customerId.Value);
                }

                var transactions = await query
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new CustomerTransactionViewModel
                    {
                        Id = t.Id,
                        TransactionNumber = t.TransactionNumber,
                        CustomerName = t.Customer != null ? t.Customer.FullName : "",
                        TransactionType = t.TransactionType,
                        TransactionDate = t.TransactionDate,
                        Amount = t.Amount,
                        PaidAmount = t.PaidAmount,
                        RemainingAmount = t.RemainingAmount,
                        PaidPercentage = t.Amount > 0 ? (t.PaidAmount / t.Amount) * 100 : 0,
                        DueDate = t.DueDate,
                        Status = t.Status,
                        SaleNumber = t.Sale != null ? t.Sale.SaleNumber : null
                    })
                    .ToListAsync();

                // إحصائيات متقدمة
                var summary = new
                {
                    TotalTransactions = transactions.Count,
                    TotalDebts = transactions.Where(t => t.TransactionType == "دين").Sum(t => t.Amount),
                    TotalPaid = transactions.Sum(t => t.PaidAmount),
                    TotalRemaining = transactions.Sum(t => t.RemainingAmount),
                    AveragePaidPercentage = transactions.Where(t => t.Amount > 0).Average(t => t.PaidPercentage),
                    OverdueCount = transactions.Count(t => t.Status == "متأخر"),
                    ActiveCount = transactions.Count(t => t.Status == "نشط"),
                    CompletedCount = transactions.Count(t => t.Status == "مكتمل"),
                    DebtStats = new
                    {
                        Count = transactions.Count(t => t.TransactionType == "دين"),
                        Total = transactions.Where(t => t.TransactionType == "دين").Sum(t => t.Amount),
                        Paid = transactions.Where(t => t.TransactionType == "دين").Sum(t => t.PaidAmount)
                    },
                    PaymentStats = new
                    {
                        Count = transactions.Count(t => t.TransactionType == "دفعة"),
                        Total = transactions.Where(t => t.TransactionType == "دفعة").Sum(t => t.Amount)
                    }
                };

                return Json(new { success = true, transactions = transactions, summary = summary });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== جلب العملاء المعرضين للخطر (ديون عالية) =====
        [HttpGet]
        public async Task<IActionResult> GetRiskyCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.Transactions)
                    .Where(c => !c.IsDeleted && c.CurrentDebt > 0 && c.MaxDebtLimit.HasValue)
                    .ToListAsync();

                var riskyCustomers = customers
                    .Where(c => c.CurrentDebt > c.MaxDebtLimit.Value * 0.7m)
                    .Select(c => new CustomerViewModel
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        PhoneNumber = c.PhoneNumber,
                        CurrentDebt = c.CurrentDebt,
                        MaxDebtLimit = c.MaxDebtLimit,
                        DebtPercentage = (c.CurrentDebt / c.MaxDebtLimit.Value) * 100,
                        WarningMessage = c.CurrentDebt > c.MaxDebtLimit.Value * 0.9m ?
                            "⚠️ خطر: تجاوز 90% من الحد" : "⚠️ تنبيه: اقتراب من الحد"
                    })
                    .OrderByDescending(c => c.DebtPercentage)
                    .ToList();

                return Json(new { success = true, customers = riskyCustomers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== تسجيل الأنشطة =====
        private async Task LogActivity(int userId, string action, string details)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] المستخدم {userId} - {action}: {details}");
            }
            catch { }
        }
    }
}

// ===== ViewModels =====
public class CustomerSummaryViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string AlternativePhone { get; set; }
    public string IDNumber { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string Notes { get; set; }
    public string CustomerType { get; set; }
    public decimal? MaxDebtLimit { get; set; }
    public decimal CurrentDebt { get; set; }
    public decimal TotalDebts { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PaidPercentage { get; set; }
    public int ActiveTransactions { get; set; }
    public int OverdueTransactions { get; set; }
    public int CompletedTransactions { get; set; }
    public decimal AverageDebt { get; set; }
    public string CustomerCategory { get; set; }
    public string DebtStatus { get; set; }
    public List<CustomerTransactionViewModel> RecentTransactions { get; set; }
}

public class InstallmentViewModel
{
    public int Id { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? PaidDate { get; set; }
    public bool IsPaid { get; set; }
    public bool IsOverdue { get; set; }
    public int OverdueDays { get; set; }
    public string PaymentMethod { get; set; }
}

public class CustomerViewModel
{
    public int Id { get; set; }
    public string CustomerCode { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string AlternativePhone { get; set; }
    public string IDNumber { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string Notes { get; set; }
    public string CustomerType { get; set; }
    public decimal? MaxDebtLimit { get; set; }
    public decimal CurrentDebt { get; set; }
    public string CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int TransactionsCount { get; set; }
    public decimal TotalTransactions { get; set; }
    public decimal TotalPaid { get; set; }
    public string CustomerCategory { get; set; }
    public decimal DebtPercentage { get; set; }
    public string WarningMessage { get; set; }
}

public class CustomerTransactionViewModel
{
    public int Id { get; set; }
    public string TransactionNumber { get; set; }
    public string CustomerName { get; set; }
    public string TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PaidPercentage { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int OverdueDays { get; set; }
    public string Status { get; set; }
    public bool IsInstallment { get; set; }
    public int? InstallmentCount { get; set; }
    public int InstallmentPaidCount { get; set; }
    public decimal InstallmentProgress { get; set; }
    public string SaleNumber { get; set; }
    public string CreatedByName { get; set; }
    public List<InstallmentViewModel> Installments { get; set; }
}

public class CreateCustomerViewModel
{
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string AlternativePhone { get; set; }
    public string IDNumber { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string Notes { get; set; }
    public string CustomerType { get; set; }
    public decimal? MaxDebtLimit { get; set; }
}

public class CreateTransactionViewModel
{
    public int CustomerId { get; set; }
    public int? SaleId { get; set; }
    public string TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Notes { get; set; }
    public bool IsInstallment { get; set; }
    public int? InstallmentCount { get; set; }
    public int? InstallmentPeriod { get; set; }
}

public class PayInstallmentViewModel
{
    public int InstallmentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
    public string Notes { get; set; }
    public int InstallmentPaymentId { get; internal set; }
    public bool SendWhatsApp { get; internal set; }
}

public class DashboardStatsViewModel
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int CustomersWithDebt { get; set; }
    public decimal TotalDebts { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingDebts { get; set; }
    public int OverdueTransactions { get; set; }
    public int UpcomingInstallments { get; set; }
    public List<CustomerViewModel> TopDebtors { get; set; }
    public List<CustomerViewModel> BestPayers { get; set; }
}