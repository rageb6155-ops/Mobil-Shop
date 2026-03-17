using System;
using System.Collections.Generic;

namespace MobileShopSystem.ViewModels
{
    public class CustomerViewModel
    {
        public string TechnicianName { get; set; } = "غير معين"; // أضف هذا السطر

        public int Id { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AlternativePhone { get; set; }
        public string? IDNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public string CustomerType { get; set; } = "عادي";
        public decimal? MaxDebtLimit { get; set; }
        public decimal CurrentDebt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int TransactionsCount { get; set; }
        public decimal TotalTransactions { get; set; }
        public decimal TotalPaid { get; set; }
        public string CustomerCategory { get; set; } = "عادي"; // مدين - منتظم - ممتاز
        public decimal DebtPercentage { get; set; } // نسبة الدين من الحد الأقصى
        public string WarningMessage { get; set; } = string.Empty;
    }

    public class CreateCustomerViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AlternativePhone { get; set; }
        public string? IDNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public string CustomerType { get; set; } = "عادي";
        public decimal? MaxDebtLimit { get; set; }
    }

    public class CustomerTransactionViewModel
    {
        public int Id { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? SaleId { get; set; }
        public string? SaleNumber { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal PaidPercentage { get; set; } // نسبة المدفوع
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; } // هل هو متأخر؟
        public int OverdueDays { get; set; } // عدد أيام التأخير
        public string? Notes { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public bool IsInstallment { get; set; }
        public int? InstallmentCount { get; set; }
        public int InstallmentPaidCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<InstallmentViewModel> Installments { get; set; } = new List<InstallmentViewModel>();
        public int InstallmentProgress { get; internal set; }
    }

    public class CreateTransactionViewModel
    {
        public int CustomerId { get; set; }
        public int? SaleId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; } = 0;
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
        public bool IsInstallment { get; set; } = false;
        public int? InstallmentCount { get; set; }
        public int? InstallmentPeriod { get; set; } // بالأيام
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
        public string? PaidByName { get; set; }
        public string? PaymentMethod { get; set; }
        public bool IsOverdue { get; set; }
        public int OverdueDays { get; set; }
    }

    public class PayInstallmentViewModel
    {
        public int InstallmentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "نقدي";
        public string? Notes { get; set; }
    }

    public class CustomerSummaryViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal CurrentDebt { get; set; }
        public decimal TotalDebts { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PaidPercentage { get; set; }
        public int ActiveTransactions { get; set; }
        public int OverdueTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public decimal AverageDebt { get; set; }
        public string CustomerCategory { get; set; } = string.Empty;
        public string DebtStatus { get; set; } = string.Empty;
        public List<CustomerTransactionViewModel> RecentTransactions { get; set; } = new List<CustomerTransactionViewModel>();
    }

    public class DebtReportViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingDebt { get; set; }
        public decimal PaidPercentage { get; set; }
        public int ActiveTransactionsCount { get; set; }
        public int OverdueTransactionsCount { get; set; }
        public DateTime LastTransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
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
        public int UpcomingInstallments { get; set; } // أقساط قادمة خلال 7 أيام
        public List<CustomerViewModel> TopDebtors { get; set; } = new List<CustomerViewModel>(); // أكبر المدينين
        public List<CustomerViewModel> BestPayers { get; set; } = new List<CustomerViewModel>(); // أفضل المسددين
    }
}