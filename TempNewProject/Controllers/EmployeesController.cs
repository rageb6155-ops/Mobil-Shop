using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Data;
using MobileShopSystem.Models;
using MobileShopSystem.ViewModels;
using System.Text.Json;

namespace MobileShopSystem.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeesController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
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

        // ===== صفحة الموظفين الرئيسية =====
        public IActionResult Index()
        {
            ViewBag.Username = GetCurrentUsername();
            return View();
        }

        // ===== الحصول على جميع الموظفين =====
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees(string? search, bool showDeleted = false)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Creator)
                    .Include(e => e.Transactions)
                    .Where(e => e.IsDeleted == showDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim();
                    query = query.Where(e =>
                        e.FullName.Contains(search) ||
                        e.PhoneNumber.Contains(search) ||
                        e.EmployeeCode.Contains(search) ||
                        (e.IDNumber != null && e.IDNumber.Contains(search)));
                }

                var employees = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                var result = employees.Select(e => new EmployeeViewModel
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    PhoneNumber = e.PhoneNumber,
                    Email = e.Email,
                    Address = e.Address,
                    IDNumber = e.IDNumber,
                    HireDate = e.HireDate,
                    BaseSalary = e.BaseSalary,
                    CurrentSalary = e.CurrentSalary,
                    Department = e.Department,
                    Position = e.Position,
                    Notes = e.Notes,
                    Status = e.Status,
                    CreatedByName = e.Creator != null ? e.Creator.Username : "Unknown",
                    CreatedAt = e.CreatedAt,
                    TransactionsCount = e.Transactions.Count,
                    TotalLoans = e.Transactions.Where(t => t.TransactionType == "سلفة").Sum(t => t.Amount),
                    TotalAdditions = e.Transactions.Where(t => t.TransactionType == "مكافأة").Sum(t => t.Amount),
                    TotalDeductions = e.Transactions.Where(t => t.TransactionType == "خصم").Sum(t => t.Amount)
                }).ToList();

                return Json(new { success = true, employees = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== إضافة موظف جديد =====
        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] CreateEmployeeViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                // التحقق من صحة البيانات
                if (string.IsNullOrEmpty(model.FullName))
                    return Json(new { success = false, message = "الرجاء إدخال اسم الموظف" });

                if (string.IsNullOrEmpty(model.PhoneNumber))
                    return Json(new { success = false, message = "الرجاء إدخال رقم الهاتف" });

                // التحقق من تكرار رقم الهاتف
                var existingPhone = await _context.Employees
                    .AnyAsync(e => e.PhoneNumber == model.PhoneNumber && !e.IsDeleted);

                if (existingPhone)
                    return Json(new { success = false, message = "رقم الهاتف موجود بالفعل" });

                // إنشاء كود موظف فريد
                var lastEmployee = await _context.Employees
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefaultAsync();

                int nextId = (lastEmployee?.Id ?? 0) + 1;
                string employeeCode = "EMP-" + nextId.ToString("D6");

                var employee = new Employee
                {
                    EmployeeCode = employeeCode,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    Address = model.Address,
                    IDNumber = model.IDNumber,
                    BaseSalary = model.BaseSalary,
                    CurrentSalary = model.BaseSalary,
                    Department = model.Department,
                    Position = model.Position,
                    Notes = model.Notes,
                    CreatedBy = userId.Value
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم إضافة الموظف بنجاح", employeeId = employee.Id });
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

        // ===== تحديث بيانات موظف =====
        [HttpPost]
        public async Task<IActionResult> UpdateEmployee([FromBody] EmployeeViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var employee = await _context.Employees.FindAsync(model.Id);
                if (employee == null)
                    return Json(new { success = false, message = "الموظف غير موجود" });

                // التحقق من تكرار رقم الهاتف
                var existingPhone = await _context.Employees
                    .AnyAsync(e => e.PhoneNumber == model.PhoneNumber && e.Id != model.Id && !e.IsDeleted);

                if (existingPhone)
                    return Json(new { success = false, message = "رقم الهاتف موجود بالفعل" });

                employee.FullName = model.FullName;
                employee.PhoneNumber = model.PhoneNumber;
                employee.Email = model.Email;
                employee.Address = model.Address;
                employee.IDNumber = model.IDNumber;
                employee.Department = model.Department;
                employee.Position = model.Position;
                employee.Notes = model.Notes;
                employee.Status = model.Status;
                employee.UpdatedBy = userId;
                employee.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم تحديث بيانات الموظف بنجاح" });
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

        // ===== تغيير راتب الموظف =====
        [HttpPost]
        public async Task<IActionResult> ChangeSalary([FromBody] SalaryChangeViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var employee = await _context.Employees.FindAsync(model.EmployeeId);
                if (employee == null)
                    return Json(new { success = false, message = "الموظف غير موجود" });

                if (model.NewSalary <= 0)
                    return Json(new { success = false, message = "❌ الراتب يجب أن يكون أكبر من صفر" });

                // تسجيل التغيير
                var log = new SalaryChangeLog
                {
                    EmployeeId = employee.Id,
                    OldSalary = employee.CurrentSalary,
                    NewSalary = model.NewSalary,
                    Reason = model.Reason,
                    ChangedBy = userId.Value
                };

                // تحديث الراتب
                employee.CurrentSalary = model.NewSalary;
                employee.UpdatedBy = userId;
                employee.UpdatedAt = DateTime.Now;

                _context.SalaryChangeLogs.Add(log);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"✅ تم تغيير الراتب من {log.OldSalary} ج.م إلى {model.NewSalary} ج.م" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== إضافة معاملة (سلفة/مكافأة/خصم) =====
        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] CreateEmployeeTransactionViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var employee = await _context.Employees.FindAsync(model.EmployeeId);
                if (employee == null)
                    return Json(new { success = false, message = "الموظف غير موجود" });

                // التحقق من صحة البيانات
                if (model.Amount <= 0)
                    return Json(new { success = false, message = "❌ المبلغ يجب أن يكون أكبر من صفر" });

                if (string.IsNullOrWhiteSpace(model.Description))
                    return Json(new { success = false, message = "❌ يجب إدخال بيان للمعاملة" });

                // التحقق من صحة الشهر والسنة
                if (model.Month < 1 || model.Month > 12)
                    return Json(new { success = false, message = "❌ الشهر غير صحيح" });

                if (model.Year < 2000 || model.Year > 2100)
                    return Json(new { success = false, message = "❌ السنة غير صحيحة" });

                // إنشاء رقم معاملة فريد
                var lastTransaction = await _context.EmployeeTransactions
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync();

                int nextId = (lastTransaction?.Id ?? 0) + 1;
                string prefix = model.TransactionType == "سلفة" ? "SLF" : (model.TransactionType == "مكافأة" ? "MKF" : "KSM");
                string transactionNumber = $"{prefix}-{nextId:D8}";

                var transaction = new EmployeeTransaction
                {
                    TransactionNumber = transactionNumber,
                    EmployeeId = model.EmployeeId,
                    TransactionType = model.TransactionType,
                    Amount = model.Amount,
                    Description = model.Description,
                    Month = model.Month,
                    Year = model.Year,
                    CreatedBy = userId.Value,
                    Notes = model.Notes,
                    TransactionDate = DateTime.Now
                };

                _context.EmployeeTransactions.Add(transaction);
                await _context.SaveChangesAsync(); // حفظ للحصول على الـ Id

                // البحث عن راتب الشهر الحالي أو إنشاؤه
                var salary = await _context.EmployeeSalaries
                    .FirstOrDefaultAsync(s => s.EmployeeId == model.EmployeeId &&
                                              s.SalaryMonth == model.Month &&
                                              s.SalaryYear == model.Year);

                if (salary == null)
                {
                    salary = new EmployeeSalary
                    {
                        EmployeeId = model.EmployeeId,
                        SalaryMonth = model.Month,
                        SalaryYear = model.Year,
                        BaseSalary = employee.CurrentSalary,
                        CreatedBy = userId.Value,
                        CreatedAt = DateTime.Now
                    };
                    _context.EmployeeSalaries.Add(salary);
                    await _context.SaveChangesAsync(); // حفظ للحصول على الـ Id
                }

                // ربط المعاملة بالراتب
                transaction.SalaryId = salary.Id;
                transaction.IsDeductedFromSalary = true;

                // تحديث مجاميع الراتب حسب نوع المعاملة
                switch (model.TransactionType)
                {
                    case "مكافأة":
                        salary.TotalAdditions += model.Amount;
                        break;
                    case "خصم":
                        salary.TotalDeductions += model.Amount;
                        break;
                    case "سلفة":
                        salary.TotalLoans += model.Amount;
                        break;
                }

                // إعادة حساب صافي الراتب
                salary.NetSalary = salary.BaseSalary + salary.TotalAdditions - salary.TotalDeductions - salary.TotalLoans;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"✅ تم إضافة {model.TransactionType} بنجاح",
                    transactionId = transaction.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== الحصول على تفاصيل موظف =====
        [HttpGet]
        public async Task<IActionResult> GetEmployeeDetails(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Creator)
                    .Include(e => e.Transactions)
                        .ThenInclude(t => t.Creator)
                    .Include(e => e.Salaries)
                    .Include(e => e.SalaryChangeLogs)
                        .ThenInclude(l => l.Changer)
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

                if (employee == null)
                    return Json(new { success = false, message = "الموظف غير موجود" });

                // آخر 20 معاملة
                var recentTransactions = employee.Transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(20)
                    .Select(t => new EmployeeTransactionViewModel
                    {
                        Id = t.Id,
                        TransactionNumber = t.TransactionNumber,
                        TransactionType = t.TransactionType,
                        Amount = t.Amount,
                        Description = t.Description,
                        TransactionDate = t.TransactionDate,
                        Month = t.Month,
                        Year = t.Year,
                        CreatedByName = t.Creator != null ? t.Creator.Username : "Unknown"
                    }).ToList();

                // آخر 12 راتب
                var recentSalaries = employee.Salaries
                    .OrderByDescending(s => s.SalaryYear)
                    .ThenByDescending(s => s.SalaryMonth)
                    .Take(12)
                    .Select(s => new EmployeeSalaryViewModel
                    {
                        Id = s.Id,
                        Month = s.SalaryMonth,
                        Year = s.SalaryYear,
                        MonthName = GetMonthName(s.SalaryMonth),
                        BaseSalary = s.BaseSalary,
                        TotalAdditions = s.TotalAdditions,
                        TotalDeductions = s.TotalDeductions,
                        TotalLoans = s.TotalLoans,
                        NetSalary = s.NetSalary,
                        PaymentStatus = s.PaymentStatus,
                        PaymentDate = s.PaymentDate
                    }).ToList();

                // آخر تغييرات الراتب
                var salaryChanges = employee.SalaryChangeLogs
                    .OrderByDescending(l => l.ChangeDate)
                    .Take(10)
                    .Select(l => new
                    {
                        l.OldSalary,
                        l.NewSalary,
                        l.ChangeDate,
                        l.Reason,
                        ChangedBy = l.Changer != null ? l.Changer.Username : "Unknown"
                    }).ToList();

                var result = new
                {
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    employee.PhoneNumber,
                    employee.Email,
                    employee.Address,
                    employee.IDNumber,
                    employee.HireDate,
                    employee.BaseSalary,
                    employee.CurrentSalary,
                    employee.Department,
                    employee.Position,
                    employee.Notes,
                    employee.Status,
                    CreatedByName = employee.Creator != null ? employee.Creator.Username : "Unknown",
                    employee.CreatedAt,
                    RecentTransactions = recentTransactions,
                    RecentSalaries = recentSalaries,
                    SalaryChanges = salaryChanges,
                    Stats = new
                    {
                        TotalLoans = employee.Transactions.Where(t => t.TransactionType == "سلفة").Sum(t => t.Amount),
                        TotalAdditions = employee.Transactions.Where(t => t.TransactionType == "مكافأة").Sum(t => t.Amount),
                        TotalDeductions = employee.Transactions.Where(t => t.TransactionType == "خصم").Sum(t => t.Amount)
                    }
                };

                return Json(new { success = true, employee = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== الحصول على تقرير شهري =====
        [HttpGet]
        public async Task<IActionResult> GetMonthlyReport(int employeeId, int month, int year)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Transactions.Where(t => t.Month == month && t.Year == year))
                    .FirstOrDefaultAsync(e => e.Id == employeeId);

                if (employee == null)
                    return Json(new { success = false, message = "الموظف غير موجود" });

                var salary = await _context.EmployeeSalaries
                    .FirstOrDefaultAsync(s => s.EmployeeId == employeeId &&
                                              s.SalaryMonth == month &&
                                              s.SalaryYear == year);

                var report = new MonthlyReportViewModel
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    EmployeeCode = employee.EmployeeCode,
                    Department = employee.Department ?? "---",
                    Position = employee.Position ?? "---",
                    Month = month,
                    Year = year,
                    MonthName = GetMonthName(month),
                    BaseSalary = salary?.BaseSalary ?? employee.CurrentSalary,
                    TotalAdditions = employee.Transactions.Where(t => t.TransactionType == "مكافأة").Sum(t => t.Amount),
                    TotalDeductions = employee.Transactions.Where(t => t.TransactionType == "خصم").Sum(t => t.Amount),
                    TotalLoans = employee.Transactions.Where(t => t.TransactionType == "سلفة").Sum(t => t.Amount),
                    Additions = employee.Transactions
                        .Where(t => t.TransactionType == "مكافأة")
                        .Select(t => new TransactionReportItem
                        {
                            Date = t.TransactionDate,
                            Description = t.Description,
                            Amount = t.Amount
                        }).ToList(),
                    Deductions = employee.Transactions
                        .Where(t => t.TransactionType == "خصم")
                        .Select(t => new TransactionReportItem
                        {
                            Date = t.TransactionDate,
                            Description = t.Description,
                            Amount = t.Amount
                        }).ToList(),
                    Loans = employee.Transactions
                        .Where(t => t.TransactionType == "سلفة")
                        .Select(t => new TransactionReportItem
                        {
                            Date = t.TransactionDate,
                            Description = t.Description,
                            Amount = t.Amount
                        }).ToList()
                };

                report.NetSalary = report.BaseSalary + report.TotalAdditions - report.TotalDeductions - report.TotalLoans;

                return Json(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== تأكيد دفع الراتب =====
        [HttpPost]
        public async Task<IActionResult> ConfirmSalaryPayment(int employeeId, int month, int year)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var salary = await _context.EmployeeSalaries
                    .FirstOrDefaultAsync(s => s.EmployeeId == employeeId &&
                                              s.SalaryMonth == month &&
                                              s.SalaryYear == year);

                if (salary == null)
                    return Json(new { success = false, message = "لم يتم العثور على راتب لهذا الشهر" });

                salary.PaymentStatus = "مدفوع";
                salary.PaymentDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم تأكيد دفع الراتب" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== حذف موظف (ناعم) =====
        [HttpPost]
        public async Task<IActionResult> SoftDeleteEmployee(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                    return Json(new { success = false, message = "الموظف غير موجود" });

                // التحقق من وجود سلف غير مسددة
                var hasActiveLoans = await _context.EmployeeTransactions
                    .AnyAsync(t => t.EmployeeId == id && t.TransactionType == "سلفة" && !t.IsDeductedFromSalary);

                if (hasActiveLoans)
                    return Json(new { success = false, message = "❌ لا يمكن حذف موظف عليه سلف غير مسددة" });

                employee.IsDeleted = true;
                employee.DeletedBy = userId;
                employee.DeletedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم حذف الموظف بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== استعادة موظف محذوف =====
        [HttpPost]
        public async Task<IActionResult> RestoreEmployee(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Json(new { success = false, message = "يجب تسجيل الدخول أولاً" });

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted);

                if (employee == null)
                    return Json(new { success = false, message = "الموظف غير موجود" });

                employee.IsDeleted = false;
                employee.DeletedBy = null;
                employee.DeletedAt = null;
                employee.UpdatedBy = userId;
                employee.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم استعادة الموظف بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        // ===== دوال مساعدة =====
        private string GetMonthName(int month)
        {
            string[] months = {
                "يناير", "فبراير", "مارس", "إبريل", "مايو", "يونيو",
                "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
            };
            return months[month - 1];
        }
    }
}