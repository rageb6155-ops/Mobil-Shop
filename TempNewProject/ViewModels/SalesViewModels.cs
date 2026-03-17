using System;
using System.Collections.Generic;

namespace MobileShopSystem.ViewModels
{
    // ===== طلبات API =====
    public class CompleteSaleViewModel
    {
        public decimal PaidAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class EditSaleViewModel
    {
        public int SaleId { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Notes { get; set; }
        public List<SaleItemViewModel> Items { get; set; } = new List<SaleItemViewModel>();
    }

    public class DeleteSaleViewModel
    {
        public int Id { get; set; }
    }

    public class RemoveItemViewModel
    {
        public int Id { get; set; }
    }

    public class RestoreSaleViewModel
    {
        public int Id { get; set; }
    }

    public class LoadItemsViewModel
    {
        public List<SaleItemViewModel> Items { get; set; } = new List<SaleItemViewModel>();
    }

    // ===== نماذج التقارير =====
    public class SaleReportViewModel
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime SaleDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsModified { get; set; }
        public decimal? MachinesTotal { get; set; }
        public decimal OriginalTotalAmount { get; set; }
        public decimal OriginalPaidAmount { get; set; }
        public decimal OriginalRemainingAmount { get; set; }
        public List<SaleItemReportViewModel> Items { get; set; } = new List<SaleItemReportViewModel>();
        public List<ModificationReportViewModel> Modifications { get; set; } = new List<ModificationReportViewModel>();
    }

    public class SaleItemReportViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal ItemPrice { get; set; }
        public string SaleTypeName { get; set; } = string.Empty;
    }

    public class ModificationReportViewModel
    {
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
        public string ModificationType { get; set; } = string.Empty;
        public decimal? OldAmount { get; set; }
        public decimal? NewAmount { get; set; }
        public List<SaleItemReportViewModel>? OldItems { get; set; }
        public List<SaleItemReportViewModel>? NewItems { get; set; }
    }

    // ===== نماذج التفاصيل =====
    public class SaleDetailsViewModel
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime SaleDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsModified { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? Notes { get; set; }
        public decimal OriginalTotalAmount { get; set; }
        public decimal OriginalPaidAmount { get; set; }
        public decimal OriginalRemainingAmount { get; set; }
        public List<SaleItemDetailsViewModel> Items { get; set; } = new List<SaleItemDetailsViewModel>();
        public List<SaleItemDetailsViewModel> OriginalItems { get; set; } = new List<SaleItemDetailsViewModel>();
        public List<ModificationReportViewModel> Modifications { get; set; } = new List<ModificationReportViewModel>();
    }

    public class SaleItemDetailsViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal ItemPrice { get; set; }
        public string SaleTypeName { get; set; } = string.Empty;
    }

    // ===== نماذج المبيعات اليومية =====
    public class SaleViewModel
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime SaleDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsModified { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public List<SaleItemViewModel> Items { get; set; } = new List<SaleItemViewModel>();
    }
}