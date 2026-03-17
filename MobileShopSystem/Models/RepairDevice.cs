using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("RepairDevices")]
    public class RepairDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string DeviceCode { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(20)]
        public string CustomerPhone { get; set; }

        [StringLength(50)]
        public string? DeviceType { get; set; }

        [StringLength(50)]
        public string? DeviceBrand { get; set; }

        [StringLength(100)]
        public string? DeviceModel { get; set; }

        [StringLength(100)]
        public string? DeviceSerial { get; set; }

        [StringLength(30)]
        public string? DeviceColor { get; set; }

        [StringLength(50)]
        public string? DevicePassword { get; set; }

        [StringLength(500)]
        public string? DeviceAccessories { get; set; }

        [Required]
        [StringLength(1000)]
        public string ReportedIssue { get; set; }

        [StringLength(1000)]
        public string? TechnicianNotes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FinalCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvancePayment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public DateTime ReceivedDate { get; set; }

        [StringLength(20)]
        public string? ReceivedDay { get; set; }

        [StringLength(20)]
        public string? ReceivedTime { get; set; }

        public DateTime? PromisedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "مستلم";

        public bool RequiresSpareParts { get; set; }

        [StringLength(1000)]
        public string? SparePartsDetails { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SparePartsCost { get; set; }

        public bool IsWarranty { get; set; }

        [StringLength(500)]
        public string? WarrantyDetails { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        // ===== الخصائص الجديدة =====
        public int? TechnicianId { get; set; } // المهندس الفني المسؤول

        // ===== Navigation Properties =====
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? Updater { get; set; }

        [ForeignKey("DeletedBy")]
        public virtual User? Deleter { get; set; }

        // ===== Navigation Property للمهندس =====
        [ForeignKey("TechnicianId")]
        public virtual User? Technician { get; set; }

        // ===== Collections =====
        public virtual ICollection<RepairStatusHistory>? StatusHistory { get; set; }
        public virtual ICollection<RepairDeviceImage>? Images { get; set; }
        public virtual ICollection<RepairSparePartUsed>? SparePartsUsed { get; set; }
        public virtual ICollection<RepairInstallment>? Installments { get; set; }
        public virtual ICollection<Warranty>? Warranties { get; set; }
        public virtual ICollection<RepairRating>? Ratings { get; set; }
        public virtual ICollection<WhatsAppMessageLog>? WhatsAppMessages { get; set; }
    }
}