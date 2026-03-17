using Microsoft.EntityFrameworkCore;
using MobileShopSystem.Models;

namespace MobileShopSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { }

        // الجداول الموجودة
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceHistory> DeviceHistories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<DailyClosing> DailyClosings { get; set; }
        public DbSet<DailyClosingMachine> DailyClosingMachines { get; set; }

        // الجداول الجديدة للمبيعات والأكواد السريعة
        public DbSet<SaleType> SaleTypes { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<QuickCode> QuickCodes { get; set; }
        public DbSet<SaleModificationLog> SaleModificationLogs { get; set; }

        // الجداول الجديدة لنظام العملاء والديون
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerTransaction> CustomerTransactions { get; set; }
        public DbSet<Installment> Installments { get; set; }

        // الجداول الجديدة لنظام الموظفين
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeSalary> EmployeeSalaries { get; set; }
        public DbSet<EmployeeTransaction> EmployeeTransactions { get; set; }
        public DbSet<SalaryChangeLog> SalaryChangeLogs { get; set; }

        // ========== الجداول الجديدة لنظام الصيانة ==========
        public DbSet<RepairDevice> RepairDevices { get; set; }
        public DbSet<RepairStatusHistory> RepairStatusHistories { get; set; }
        public DbSet<RepairDeviceImage> RepairDeviceImages { get; set; }
        public DbSet<SparePart> SpareParts { get; set; }
        public DbSet<RepairSparePartUsed> RepairSparePartsUsed { get; set; }
        public DbSet<RepairInstallment> RepairInstallments { get; set; }
        public DbSet<RepairInstallmentPayment> RepairInstallmentPayments { get; set; }
        public DbSet<Warranty> Warranties { get; set; }
        public DbSet<RepairRating> RepairRatings { get; set; }
        public DbSet<WhatsAppMessageLog> WhatsAppMessageLogs { get; set; }

        // ========== الجداول الجديدة لنظام طلبات المحل ==========
        public DbSet<ShopRequest> ShopRequests { get; set; }
        public DbSet<ShopRequestWhatsAppLog> ShopRequestWhatsAppLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== العلاقات الموجودة ==========

            // علاقة DailyClosing مع الماكينات
            modelBuilder.Entity<DailyClosingMachine>()
                .HasOne(m => m.DailyClosing)
                .WithMany(d => d.Machines)
                .HasForeignKey(m => m.DailyClosingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ضبط الدقة المالية
            modelBuilder.Entity<DailyClosing>()
                .Property(d => d.CashLeft).HasPrecision(18, 2);
            modelBuilder.Entity<DailyClosing>()
                .Property(d => d.CoinsAmount).HasPrecision(18, 2);
            modelBuilder.Entity<DailyClosingMachine>()
                .Property(m => m.Balance).HasPrecision(18, 2);

            // القيم الافتراضية للحالات
            modelBuilder.Entity<DailyClosing>()
                .Property(d => d.IsDeleted).HasDefaultValue(false);
            modelBuilder.Entity<DailyClosing>()
                .Property(d => d.IsEdited).HasDefaultValue(false);

            // إعدادات إضافية
            modelBuilder.Entity<DailyClosing>()
                .Property(d => d.PreviousDataJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            modelBuilder.Entity<DailyClosing>()
                .Property(d => d.CreatedBy).IsRequired();
            modelBuilder.Entity<DailyClosing>()
                .Property(d => d.CreatedAt).IsRequired();

            // ========== العلاقات الجديدة للمبيعات ==========

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.SaleType)
                .WithMany()
                .HasForeignKey(si => si.SaleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleModificationLog>()
                .HasOne(l => l.Sale)
                .WithMany()
                .HasForeignKey(l => l.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuickCode>()
                .HasOne(q => q.User)
                .WithMany()
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== إعدادات الحقول للمبيعات ==========

            modelBuilder.Entity<Sale>()
                .Property(s => s.SaleNumber).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.SaleNumber).IsUnique();
            modelBuilder.Entity<Sale>()
                .Property(s => s.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Sale>()
                .Property(s => s.PaidAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Sale>()
                .Property(s => s.RemainingAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Sale>()
                .Property(s => s.IsDeleted).HasDefaultValue(false);
            modelBuilder.Entity<Sale>()
                .Property(s => s.IsModified).HasDefaultValue(false);
            modelBuilder.Entity<Sale>()
                .HasIndex(s => new { s.UserId, s.SaleDate });

            modelBuilder.Entity<SaleItem>()
                .Property(si => si.ItemName).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<SaleItem>()
                .Property(si => si.ItemPrice).HasPrecision(18, 2);
            modelBuilder.Entity<SaleItem>()
                .Property(si => si.Quantity).HasDefaultValue(1);
            modelBuilder.Entity<SaleItem>()
                .HasIndex(si => si.SaleId);

            modelBuilder.Entity<SaleType>()
                .Property(st => st.Name).IsRequired().HasMaxLength(50);

            // Seed SaleType data
            modelBuilder.Entity<SaleType>().HasData(
                new SaleType { Id = 1, Name = "رصيد", CreatedAt = DateTime.Now },
                new SaleType { Id = 2, Name = "منتج", CreatedAt = DateTime.Now }
            );

            modelBuilder.Entity<QuickCode>()
                .Property(q => q.CodeName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<QuickCode>()
                .Property(q => q.CodeValue).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<QuickCode>()
                .HasIndex(q => q.CodeValue).IsUnique();

            modelBuilder.Entity<SaleModificationLog>()
                .Property(l => l.ModificationType).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<SaleModificationLog>()
                .Property(l => l.OldData).HasColumnType("nvarchar(max)");
            modelBuilder.Entity<SaleModificationLog>()
                .Property(l => l.NewData).HasColumnType("nvarchar(max)");

            // ========== العلاقات الجديدة لنظام العملاء والديون ==========

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Creator).WithMany().HasForeignKey(c => c.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Updater).WithMany().HasForeignKey(c => c.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Deleter).WithMany().HasForeignKey(c => c.DeletedBy).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomerTransaction>()
                .HasOne(t => t.Customer).WithMany(c => c.Transactions).HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CustomerTransaction>()
                .HasOne(t => t.Sale).WithMany().HasForeignKey(t => t.SaleId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CustomerTransaction>()
                .HasOne(t => t.Creator).WithMany().HasForeignKey(t => t.CreatedBy).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Installment>()
                .HasOne(i => i.Transaction).WithMany(t => t.Installments).HasForeignKey(i => i.TransactionId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Installment>()
                .HasOne(i => i.Payer).WithMany().HasForeignKey(i => i.PaidBy).OnDelete(DeleteBehavior.Restrict);

            // ========== إعدادات الحقول للعملاء ==========

            modelBuilder.Entity<Customer>()
                .Property(c => c.CustomerCode).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode).IsUnique();
            modelBuilder.Entity<Customer>()
                .Property(c => c.FullName).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<Customer>()
                .Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            modelBuilder.Entity<Customer>()
                .Property(c => c.AlternativePhone).HasMaxLength(20).IsRequired(false);
            modelBuilder.Entity<Customer>()
                .Property(c => c.IDNumber).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<Customer>()
                .Property(c => c.Address).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<Customer>()
                .Property(c => c.Email).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<Customer>()
                .Property(c => c.CustomerType).HasMaxLength(50).HasDefaultValue("عادي");
            modelBuilder.Entity<Customer>()
                .Property(c => c.MaxDebtLimit).HasPrecision(18, 2).IsRequired(false);
            modelBuilder.Entity<Customer>()
                .Property(c => c.CurrentDebt).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<Customer>()
                .Property(c => c.IsActive).HasDefaultValue(true);
            modelBuilder.Entity<Customer>()
                .Property(c => c.IsDeleted).HasDefaultValue(false);
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.PhoneNumber).HasDatabaseName("IX_Customers_PhoneNumber");
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode).HasDatabaseName("IX_Customers_CustomerCode");

            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.TransactionNumber).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<CustomerTransaction>()
                .HasIndex(t => t.TransactionNumber).IsUnique();
            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.TransactionType).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.PaidAmount).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.RemainingAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.Status).HasMaxLength(50).HasDefaultValue("نشط");
            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.IsInstallment).HasDefaultValue(false);
            modelBuilder.Entity<CustomerTransaction>()
                .Property(t => t.InstallmentPaidCount).HasDefaultValue(0);
            modelBuilder.Entity<CustomerTransaction>()
                .HasIndex(t => t.CustomerId).HasDatabaseName("IX_CustomerTransactions_CustomerId");
            modelBuilder.Entity<CustomerTransaction>()
                .HasIndex(t => t.TransactionDate).HasDatabaseName("IX_CustomerTransactions_TransactionDate");
            modelBuilder.Entity<CustomerTransaction>()
                .HasIndex(t => t.Status).HasDatabaseName("IX_CustomerTransactions_Status");

            modelBuilder.Entity<Installment>()
                .Property(i => i.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Installment>()
                .Property(i => i.PaidAmount).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<Installment>()
                .Property(i => i.PaymentMethod).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<Installment>()
                .Property(i => i.IsPaid).HasDefaultValue(false);
            modelBuilder.Entity<Installment>()
                .HasIndex(i => i.TransactionId).HasDatabaseName("IX_Installments_TransactionId");
            modelBuilder.Entity<Installment>()
                .HasIndex(i => i.DueDate).HasDatabaseName("IX_Installments_DueDate");
            modelBuilder.Entity<Installment>()
                .HasIndex(i => i.IsPaid).HasDatabaseName("IX_Installments_IsPaid");

            // ========== العلاقات الجديدة لنظام الموظفين ==========

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Creator).WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Updater).WithMany().HasForeignKey(e => e.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Deleter).WithMany().HasForeignKey(e => e.DeletedBy).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeSalary>()
                .HasOne(s => s.Employee).WithMany(e => e.Salaries).HasForeignKey(s => s.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmployeeSalary>()
                .HasOne(s => s.Creator).WithMany().HasForeignKey(s => s.CreatedBy).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTransaction>()
                .HasOne(t => t.Employee).WithMany(e => e.Transactions).HasForeignKey(t => t.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmployeeTransaction>()
                .HasOne(t => t.Salary).WithMany(s => s.Transactions).HasForeignKey(t => t.SalaryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EmployeeTransaction>()
                .HasOne(t => t.Creator).WithMany().HasForeignKey(t => t.CreatedBy).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryChangeLog>()
                .HasOne(l => l.Employee).WithMany(e => e.SalaryChangeLogs).HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SalaryChangeLog>()
                .HasOne(l => l.Changer).WithMany().HasForeignKey(l => l.ChangedBy).OnDelete(DeleteBehavior.Restrict);

            // ========== إعدادات الحقول للموظفين ==========

            modelBuilder.Entity<Employee>()
                .Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode).IsUnique();
            modelBuilder.Entity<Employee>()
                .Property(e => e.FullName).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<Employee>()
                .Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            modelBuilder.Entity<Employee>()
                .Property(e => e.Email).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<Employee>()
                .Property(e => e.Address).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<Employee>()
                .Property(e => e.IDNumber).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<Employee>()
                .Property(e => e.Department).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<Employee>()
                .Property(e => e.Position).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<Employee>()
                .Property(e => e.Status).HasMaxLength(50).HasDefaultValue("نشط");
            modelBuilder.Entity<Employee>()
                .Property(e => e.BaseSalary).HasPrecision(18, 2);
            modelBuilder.Entity<Employee>()
                .Property(e => e.CurrentSalary).HasPrecision(18, 2);
            modelBuilder.Entity<Employee>()
                .Property(e => e.IsDeleted).HasDefaultValue(false);
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.PhoneNumber).HasDatabaseName("IX_Employees_PhoneNumber");
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode).HasDatabaseName("IX_Employees_EmployeeCode");
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Status).HasDatabaseName("IX_Employees_Status");

            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.SalaryMonth).IsRequired();
            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.SalaryYear).IsRequired();
            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.BaseSalary).HasPrecision(18, 2);
            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.TotalAdditions).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.TotalDeductions).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.TotalLoans).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.NetSalary).HasPrecision(18, 2);
            modelBuilder.Entity<EmployeeSalary>()
                .Property(s => s.PaymentStatus).HasMaxLength(50).HasDefaultValue("غير مدفوع");
            modelBuilder.Entity<EmployeeSalary>()
                .HasIndex(s => new { s.EmployeeId, s.SalaryYear, s.SalaryMonth })
                .IsUnique().HasDatabaseName("IX_EmployeeSalaries_EmployeeMonth");

            modelBuilder.Entity<EmployeeTransaction>()
                .Property(t => t.TransactionNumber).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<EmployeeTransaction>()
                .HasIndex(t => t.TransactionNumber).IsUnique();
            modelBuilder.Entity<EmployeeTransaction>()
                .Property(t => t.TransactionType).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<EmployeeTransaction>()
                .Property(t => t.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<EmployeeTransaction>()
                .Property(t => t.Description).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<EmployeeTransaction>()
                .Property(t => t.Month).IsRequired();
            modelBuilder.Entity<EmployeeTransaction>()
                .Property(t => t.Year).IsRequired();
            modelBuilder.Entity<EmployeeTransaction>()
                .Property(t => t.IsDeductedFromSalary).HasDefaultValue(false);
            modelBuilder.Entity<EmployeeTransaction>()
                .HasIndex(t => t.EmployeeId).HasDatabaseName("IX_EmployeeTransactions_EmployeeId");
            modelBuilder.Entity<EmployeeTransaction>()
                .HasIndex(t => new { t.Year, t.Month }).HasDatabaseName("IX_EmployeeTransactions_MonthYear");
            modelBuilder.Entity<EmployeeTransaction>()
                .HasIndex(t => t.TransactionType).HasDatabaseName("IX_EmployeeTransactions_Type");

            modelBuilder.Entity<SalaryChangeLog>()
                .Property(l => l.OldSalary).HasPrecision(18, 2);
            modelBuilder.Entity<SalaryChangeLog>()
                .Property(l => l.NewSalary).HasPrecision(18, 2);
            modelBuilder.Entity<SalaryChangeLog>()
                .Property(l => l.Reason).HasMaxLength(500).IsRequired(false);

            // ========== العلاقات الجديدة لنظام الصيانة ==========

            modelBuilder.Entity<RepairDevice>()
                .HasOne(d => d.Creator).WithMany().HasForeignKey(d => d.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RepairDevice>()
                .HasOne(d => d.Updater).WithMany().HasForeignKey(d => d.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RepairDevice>()
                .HasOne(d => d.Deleter).WithMany().HasForeignKey(d => d.DeletedBy).OnDelete(DeleteBehavior.Restrict);

            // ===== العلاقة الجديدة للمهندس الفني (أضفنا هذا) =====
            modelBuilder.Entity<RepairDevice>()
                .HasOne(d => d.Technician)
                .WithMany()
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RepairStatusHistory>()
                .HasOne(h => h.Device).WithMany(d => d.StatusHistory).HasForeignKey(h => h.DeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RepairStatusHistory>()
                .HasOne(h => h.Changer).WithMany().HasForeignKey(h => h.ChangedBy).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RepairDeviceImage>()
                .HasOne(i => i.Device).WithMany(d => d.Images).HasForeignKey(i => i.DeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RepairDeviceImage>()
                .HasOne(i => i.Uploader).WithMany().HasForeignKey(i => i.UploadedBy).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SparePart>()
                .Property(p => p.PartCode).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<SparePart>()
                .HasIndex(p => p.PartCode).IsUnique();
            modelBuilder.Entity<SparePart>()
                .Property(p => p.PartName).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<SparePart>()
                .Property(p => p.Cost).HasPrecision(18, 2);
            modelBuilder.Entity<SparePart>()
                .Property(p => p.SellingPrice).HasPrecision(18, 2);
            modelBuilder.Entity<SparePart>()
                .Property(p => p.Quantity).HasDefaultValue(0);
            modelBuilder.Entity<SparePart>()
                .Property(p => p.MinQuantity).HasDefaultValue(5);
            modelBuilder.Entity<SparePart>()
                .Property(p => p.IsDeleted).HasDefaultValue(false);

            modelBuilder.Entity<RepairSparePartUsed>()
                .HasOne(u => u.Repair).WithMany(d => d.SparePartsUsed).HasForeignKey(u => u.RepairId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RepairSparePartUsed>()
                .HasOne(u => u.Part).WithMany(p => p.RepairsUsed).HasForeignKey(u => u.PartId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RepairSparePartUsed>()
                .Property(u => u.Price).HasPrecision(18, 2);

            modelBuilder.Entity<RepairInstallment>()
                .HasOne(i => i.Device).WithMany(d => d.Installments).HasForeignKey(i => i.DeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RepairInstallment>()
                .Property(i => i.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<RepairInstallment>()
                .Property(i => i.DownPayment).HasPrecision(18, 2);
            modelBuilder.Entity<RepairInstallment>()
                .Property(i => i.RemainingAmount).HasPrecision(18, 2);
            modelBuilder.Entity<RepairInstallment>()
                .Property(i => i.InstallmentAmount).HasPrecision(18, 2);

            modelBuilder.Entity<RepairInstallmentPayment>()
                .HasOne(p => p.Installment).WithMany(i => i.Payments).HasForeignKey(p => p.InstallmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RepairInstallmentPayment>()
                .Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<RepairInstallmentPayment>()
                .Property(p => p.IsPaid).HasDefaultValue(false);
            modelBuilder.Entity<RepairInstallmentPayment>()
                .Property(p => p.PaymentMethod).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<RepairInstallmentPayment>()
                .Property(p => p.Notes).HasMaxLength(500).IsRequired(false);

            modelBuilder.Entity<Warranty>()
                .HasOne(w => w.Device).WithMany(d => d.Warranties).HasForeignKey(w => w.DeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Warranty>()
                .Property(w => w.WarrantyNumber).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Warranty>()
                .HasIndex(w => w.WarrantyNumber).IsUnique();
            modelBuilder.Entity<Warranty>()
                .Property(w => w.WarrantyType).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Warranty>()
                .Property(w => w.Cost).HasPrecision(18, 2).IsRequired(false);
            modelBuilder.Entity<Warranty>()
                .Property(w => w.Coverage).IsRequired(false);
            modelBuilder.Entity<Warranty>()
                .Property(w => w.Notes).HasMaxLength(1000).IsRequired(false);
            modelBuilder.Entity<Warranty>()
                .Property(w => w.IsActive).HasDefaultValue(true);

            modelBuilder.Entity<RepairRating>()
                .HasOne(r => r.Device).WithMany(d => d.Ratings).HasForeignKey(r => r.DeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RepairRating>()
                .Property(r => r.CustomerSatisfaction).IsRequired(false);
            modelBuilder.Entity<RepairRating>()
                .Property(r => r.TechnicianRating).IsRequired(false);
            modelBuilder.Entity<RepairRating>()
                .Property(r => r.PriceRating).IsRequired(false);
            modelBuilder.Entity<RepairRating>()
                .Property(r => r.Comment).HasMaxLength(1000).IsRequired(false);

            modelBuilder.Entity<WhatsAppMessageLog>()
                .HasOne(w => w.Device).WithMany(d => d.WhatsAppMessages).HasForeignKey(w => w.DeviceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WhatsAppMessageLog>()
                .Property(w => w.MessageType).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<WhatsAppMessageLog>()
                .Property(w => w.IsSuccess).HasDefaultValue(true);
            modelBuilder.Entity<WhatsAppMessageLog>()
                .Property(w => w.MessageId).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<WhatsAppMessageLog>()
                .Property(w => w.Error).IsRequired(false);
            modelBuilder.Entity<WhatsAppMessageLog>()
                .HasIndex(w => w.SentAt).HasDatabaseName("IX_WhatsAppMessageLogs_SentAt");

            // ========== إعدادات الحقول لنظام الصيانة ==========

            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DeviceCode).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<RepairDevice>()
                .HasIndex(d => d.DeviceCode).IsUnique();
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.CustomerName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.CustomerPhone).IsRequired().HasMaxLength(20);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DeviceType).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DeviceBrand).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DeviceModel).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DeviceSerial).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DeviceColor).HasMaxLength(30).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DevicePassword).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.DeviceAccessories).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.ReportedIssue).IsRequired().HasMaxLength(1000);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.TechnicianNotes).HasMaxLength(1000).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.EstimatedCost).HasPrecision(18, 2).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.FinalCost).HasPrecision(18, 2).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.AdvancePayment).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.RemainingAmount).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.Status).HasMaxLength(20).HasDefaultValue("مستلم");
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.ReceivedDay).HasMaxLength(20).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.ReceivedTime).HasMaxLength(20).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.RequiresSpareParts).HasDefaultValue(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.SparePartsDetails).HasMaxLength(1000).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.SparePartsCost).HasPrecision(18, 2).HasDefaultValue(0);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.IsWarranty).HasDefaultValue(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.WarrantyDetails).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.Notes).HasMaxLength(1000).IsRequired(false);
            modelBuilder.Entity<RepairDevice>()
                .Property(d => d.IsDeleted).HasDefaultValue(false);
            modelBuilder.Entity<RepairDevice>()
                .HasIndex(d => d.Status).HasDatabaseName("IX_RepairDevices_Status");
            modelBuilder.Entity<RepairDevice>()
                .HasIndex(d => d.CustomerPhone).HasDatabaseName("IX_RepairDevices_CustomerPhone");
            modelBuilder.Entity<RepairDevice>()
                .HasIndex(d => d.DeviceSerial).HasDatabaseName("IX_RepairDevices_DeviceSerial");
            modelBuilder.Entity<RepairDevice>()
                .HasIndex(d => d.DeviceCode).HasDatabaseName("IX_RepairDevices_DeviceCode");
            modelBuilder.Entity<RepairDevice>()
                .HasIndex(d => d.ReceivedDate).HasDatabaseName("IX_RepairDevices_ReceivedDate");

            // ========== العلاقات الجديدة لنظام طلبات المحل ==========

            modelBuilder.Entity<ShopRequest>()
                .HasOne(r => r.Creator)
                .WithMany()
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShopRequest>()
                .HasOne(r => r.Updater)
                .WithMany()
                .HasForeignKey(r => r.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShopRequest>()
                .HasOne(r => r.Deleter)
                .WithMany()
                .HasForeignKey(r => r.DeletedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .HasOne(l => l.Request)
                .WithMany()
                .HasForeignKey(l => l.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== إعدادات الحقول لنظام طلبات المحل ==========

            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.ItemName).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.ItemType).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.Quantity).IsRequired();
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.UnitPrice).HasPrecision(18, 2).IsRequired(false);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.TotalPrice).HasPrecision(18, 2).IsRequired(false);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.Supplier).HasMaxLength(200).IsRequired(false);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.PhoneNumber).HasMaxLength(20).IsRequired(false);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.Notes).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.Priority).HasDefaultValue(1);
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.Status).HasMaxLength(50).HasDefaultValue("قيد الانتظار");
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.RequestDate).IsRequired();
            modelBuilder.Entity<ShopRequest>()
                .Property(r => r.IsDeleted).HasDefaultValue(false);
            modelBuilder.Entity<ShopRequest>()
                .HasIndex(r => r.RequestDate).HasDatabaseName("IX_ShopRequests_RequestDate");
            modelBuilder.Entity<ShopRequest>()
                .HasIndex(r => r.Status).HasDatabaseName("IX_ShopRequests_Status");
            modelBuilder.Entity<ShopRequest>()
                .HasIndex(r => r.ItemType).HasDatabaseName("IX_ShopRequests_ItemType");
            modelBuilder.Entity<ShopRequest>()
                .HasIndex(r => r.Priority).HasDatabaseName("IX_ShopRequests_Priority");

            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .Property(l => l.PhoneNumber).IsRequired().HasMaxLength(20);
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .Property(l => l.MessageType).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .Property(l => l.Message).IsRequired();
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .Property(l => l.SentAt).IsRequired();
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .Property(l => l.IsSuccess).HasDefaultValue(true);
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .Property(l => l.MessageId).HasMaxLength(100).IsRequired(false);
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .Property(l => l.Error).IsRequired(false);
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .HasIndex(l => l.SentAt).HasDatabaseName("IX_ShopRequestWhatsAppLogs_SentAt");
            modelBuilder.Entity<ShopRequestWhatsAppLog>()
                .HasIndex(l => l.RequestId).HasDatabaseName("IX_ShopRequestWhatsAppLogs_RequestId");
        }
    }
}