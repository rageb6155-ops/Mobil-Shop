using System;
using System.Collections.Generic;

namespace MobileShopSystem.ViewModels
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? IDNumber { get; set; }
        public DateTime HireDate { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal CurrentSalary { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "نشط";
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TransactionsCount { get; set; }
        public decimal TotalLoans { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
    }

    public class CreateEmployeeViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? IDNumber { get; set; }
        public decimal BaseSalary { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateEmployeeTransactionViewModel
    {
        public int EmployeeId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string? Notes { get; set; }
    }

    public class EmployeeTransactionViewModel
    {
        public int Id { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public bool IsDeductedFromSalary { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class EmployeeSalaryViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalLoans { get; set; }
        public decimal NetSalary { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public string? Notes { get; set; }
        public List<EmployeeTransactionViewModel> Transactions { get; set; } = new List<EmployeeTransactionViewModel>();
    }

    public class MonthlyReportViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalLoans { get; set; }
        public decimal NetSalary { get; set; }
        public List<TransactionReportItem> Additions { get; set; } = new List<TransactionReportItem>();
        public List<TransactionReportItem> Deductions { get; set; } = new List<TransactionReportItem>();
        public List<TransactionReportItem> Loans { get; set; } = new List<TransactionReportItem>();
    }

    public class TransactionReportItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class SalaryChangeViewModel
    {
        public int EmployeeId { get; set; }
        public decimal NewSalary { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class EmployeeSummaryViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public decimal CurrentSalary { get; set; }
        public decimal TotalLoans { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public int PendingTransactions { get; set; }
    }
}