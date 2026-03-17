using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MobileShopSystem.ViewModels
{
    // ========== ViewModels الأساسية ==========
    public class RepairDeviceViewModel
    {
            public bool IsDeleted { get; set; } // أضف هذا السطر
        public int Id { get; set; }
        public string DeviceCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string DeviceType { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceSerial { get; set; }
        public string DeviceColor { get; set; }
        public string DevicePassword { get; set; }
        public string DeviceAccessories { get; set; }
        public string ReportedIssue { get; set; }
        public string TechnicianNotes { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? FinalCost { get; set; }
        public decimal AdvancePayment { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string ReceivedDay { get; set; }
        public string ReceivedTime { get; set; }
        public DateTime? PromisedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public string Status { get; set; }
        public bool RequiresSpareParts { get; set; }
        public string SparePartsDetails { get; set; }
        public decimal SparePartsCost { get; set; }
        public bool IsWarranty { get; set; }
        public string WarrantyDetails { get; set; }
        public string Notes { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }

        // الخاصية الجديدة
        public string TechnicianName { get; set; } = "غير معين";

        // إحصائيات محسوبة
        public int DaysInRepair { get; set; }
        public string StatusColor { get; set; }
        public string StatusIcon { get; set; }
        public bool IsOverdue { get; set; }
        public int OverdueDays { get; set; }

        // القوائم المرتبطة
        public List<RepairStatusHistoryViewModel> StatusHistory { get; set; }
        public List<RepairDeviceImageViewModel> Images { get; set; }
        public List<SparePartUsedViewModel> SparePartsUsed { get; set; }
        public List<RepairInstallmentViewModel> Installments { get; set; }
        public WarrantyViewModel Warranty { get; set; }
        public RepairRatingViewModel Rating { get; set; }
        public List<WhatsAppMessageLogViewModel> WhatsAppMessages { get; set; }
    }

    public class RepairStatusHistoryViewModel
    {
        public int Id { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Notes { get; set; }
        public string TimeAgo { get; set; }
    }

    public class RepairDeviceImageViewModel
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }
        public string ImageType { get; set; }
        public string UploadedByName { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Notes { get; set; }
    }

    public class SparePartViewModel
    {
        public int Id { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public string[] CompatibleModels { get; set; }
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
        public decimal Cost { get; set; }
        public decimal SellingPrice { get; set; }
        public string Supplier { get; set; }
        public string Location { get; set; }
        public bool IsLowStock => Quantity <= MinQuantity;
    }

    public class SparePartUsedViewModel
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public string PartCode { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
        public DateTime UsedAt { get; set; }
        public string Notes { get; set; }
    }

    public class RepairInstallmentViewModel
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }
        public decimal RemainingAmount { get; set; }
        public int NumberOfInstallments { get; set; }
        public decimal InstallmentAmount { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; }
        public List<RepairInstallmentPaymentViewModel> Payments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal Progress => (PaidInstallments * 100m) / NumberOfInstallments;
    }

    public class RepairInstallmentPaymentViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public bool IsOverdue => !IsPaid && DueDate < DateTime.Now;
    }

    public class WarrantyViewModel
    {
        public int Id { get; set; }
        public string WarrantyNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WarrantyType { get; set; }
        public string Coverage { get; set; }
        public decimal? Cost { get; set; }
        public bool IsActive { get; set; }
        public int RemainingDays => IsActive ? (EndDate - DateTime.Now).Days : 0;
        public bool IsExpiringSoon => RemainingDays > 0 && RemainingDays <= 30;
    }

    public class RepairRatingViewModel
    {
        public int Id { get; set; }
        public int? CustomerSatisfaction { get; set; }
        public int? TechnicianRating { get; set; }
        public int? PriceRating { get; set; }
        public double AverageRating => new[] {
            CustomerSatisfaction ?? 0,
            TechnicianRating ?? 0,
            PriceRating ?? 0
        }.Average();
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WhatsAppMessageLogViewModel
    {
        public int Id { get; set; }
        public string CustomerPhone { get; set; }
        public string MessageType { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsSuccess { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
        public string TimeAgo { get; set; }
    }

    // ========== Create/Update ViewModels ==========
    public class CreateRepairDeviceViewModel
    {
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string DeviceType { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceSerial { get; set; }
        public string DeviceColor { get; set; }
        public string DevicePassword { get; set; }
        public string DeviceAccessories { get; set; }
        public string ReportedIssue { get; set; }
        public string TechnicianNotes { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal AdvancePayment { get; set; }
        public DateTime? PromisedDate { get; set; }
        public bool IsWarranty { get; set; }
        public string WarrantyDetails { get; set; }
        public string Notes { get; set; }
        public bool SendWhatsApp { get; set; } = true;
    }

    public class UpdateRepairDeviceViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string DeviceType { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceSerial { get; set; }
        public string DeviceColor { get; set; }
        public string DevicePassword { get; set; }
        public string DeviceAccessories { get; set; }
        public string ReportedIssue { get; set; }
        public string TechnicianNotes { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? FinalCost { get; set; }
        public decimal AdvancePayment { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? PromisedDate { get; set; }
        public bool RequiresSpareParts { get; set; }
        public string SparePartsDetails { get; set; }
        public decimal SparePartsCost { get; set; }
        public bool IsWarranty { get; set; }
        public string WarrantyDetails { get; set; }
        public string Notes { get; set; }
    }

    public class ChangeRepairStatusViewModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public decimal? FinalCost { get; set; }
        public decimal? AdvancePayment { get; set; }
        public string Notes { get; set; }
        public bool SendWhatsApp { get; set; } = true;
    }

    public class AddSparePartViewModel
    {
        public int RepairId { get; set; }
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
    }

    public class UploadImageViewModel
    {
        public int DeviceId { get; set; }
        public IFormFile Image { get; set; }
        public string ImageType { get; set; }
        public string Notes { get; set; }
    }

    public class CreateInstallmentViewModel
    {
        public int DeviceId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }
        public int NumberOfInstallments { get; set; }
        public DateTime StartDate { get; set; }
    }

    // ===== PayInstallmentViewModel (مرة واحدة فقط - في السطر 232) =====

    public class CreateWarrantyViewModel
    {
        public int DeviceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WarrantyType { get; set; }
        public string Coverage { get; set; }
        public decimal? Cost { get; set; }
        public string Notes { get; set; }
    }

    public class AddRatingViewModel
    {
        public int DeviceId { get; set; }
        public int CustomerSatisfaction { get; set; }
        public int TechnicianRating { get; set; }
        public int PriceRating { get; set; }
        public string Comment { get; set; }
    }

    public class SendWhatsAppViewModel
    {
        public int DeviceId { get; set; }
        public string MessageType { get; set; }
        public string CustomMessage { get; set; }
    }

    // ========== Dashboard و Reports ==========
    public class RepairDashboardStatsViewModel
    {
        public int TotalDevices { get; set; }
        public int ReceivedToday { get; set; }
        public int InProgress { get; set; }
        public int WaitingParts { get; set; }
        public int Completed { get; set; }
        public int Delivered { get; set; }
        public int OverdueDevices { get; set; }

        public decimal TotalEstimatedCost { get; set; }
        public decimal TotalFinalCost { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalRemaining { get; set; }

        public List<RepairDeviceViewModel> RecentDevices { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; }
        public List<SparePartViewModel> LowStockParts { get; set; }
    }

    public class TechnicianPerformanceReportViewModel
    {
        public string TechnicianName { get; set; }
        public int TotalDevices { get; set; }
        public int CompletedDevices { get; set; }
        public double AverageDays { get; set; }
        public decimal TotalRevenue { get; set; }
        public double CustomerRating { get; set; }
        public int DevicesInProgress { get; set; }
    }

    public class RevenueReportViewModel
    {
        public string Period { get; set; }
        public DateTime Date { get; set; }
        public int DevicesCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit { get; set; }
        public List<RevenueDetailViewModel> Details { get; set; }
    }

    public class RevenueDetailViewModel
    {
        public string DeviceCode { get; set; }
        public string CustomerName { get; set; }
        public string DeviceModel { get; set; }
        public decimal FinalCost { get; set; }
        public decimal SparePartsCost { get; set; }
        public decimal Profit { get; set; }
        public DateTime CompletedDate { get; set; }
    }

    public class CustomerReportViewModel
    {
        public string CustomerPhone { get; set; }
        public string CustomerName { get; set; }
        public int TotalDevices { get; set; }
        public decimal TotalSpent { get; set; }
        public int CompletedDevices { get; set; }
        public int PendingDevices { get; set; }
        public DateTime LastRepairDate { get; set; }
        public List<RepairDeviceViewModel> Devices { get; set; }
        public string TechnicianName { get; set; } = "غير معين";
    }

    // ===== ViewModels إضافية للتاريخ =====
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
}