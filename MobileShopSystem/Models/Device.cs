using System;
using System.ComponentModel.DataAnnotations;

namespace MobileShopSystem.Models
{
    public class Device
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الجهاز مطلوب")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "السيريال مطلوب")]
        public string Serial { get; set; } = string.Empty;

        [Required(ErrorMessage = "السعة مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "السعة يجب أن تكون أكبر من صفر")]
        public int? Storage { get; set; }

        [Required(ErrorMessage = "الرام مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "الرام يجب أن تكون أكبر من صفر")]
        public int? RAM { get; set; }

        [Required(ErrorMessage = "سعر الشراء مطلوب")]
        [Range(0, double.MaxValue, ErrorMessage = "سعر الشراء يجب أن يكون 0 أو أكثر")]
        public decimal? PurchasePrice { get; set; }

        [Required(ErrorMessage = "سعر البيع مطلوب")]
        [Range(0, double.MaxValue, ErrorMessage = "سعر البيع يجب أن يكون 0 أو أكثر")]
        public decimal? SalePrice { get; set; }

        [Required(ErrorMessage = "نوع المالك مطلوب")]
        public string OwnerType { get; set; } = "محل";

        public string? OwnerName { get; set; }
        public string? OwnerPhone { get; set; }
        public string? OwnerID { get; set; }

        [Required]
        public string Status { get; set; } = "سليم";

        [Required]
        public string CreatedBy { get; set; } = "System";

        [Required]
        public DateTime CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ===== بيانات الأصلية عند الإضافة =====
        public string? OriginalName { get; set; }
        public string? OriginalSerial { get; set; }
        public int? OriginalStorage { get; set; }
        public int? OriginalRAM { get; set; }
        public decimal? OriginalPurchasePrice { get; set; }
        public decimal? OriginalSalePrice { get; set; }
        public string? OriginalOwnerType { get; set; }
        public string? OriginalOwnerName { get; set; }
        public string? OriginalOwnerPhone { get; set; }
        public string? OriginalOwnerID { get; set; }
    }
}