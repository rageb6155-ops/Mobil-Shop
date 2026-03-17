using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SaleNumber { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        [Required]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public int? DeletedBy { get; set; }

        public DateTime? DeletedAt { get; set; }

        public bool IsModified { get; set; } = false;

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public string? Notes { get; set; }

        // ===== الحقول الجديدة للبيانات الأصلية =====
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalTotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalRemainingAmount { get; set; }

        // العلاقات
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        // أضف هذه الخصائص في نهاية ملف Sale.cs قبل العلاقات
        public int? CustomerId { get; set; }
        public bool IsInstallment { get; set; } = false;
        public int? InstallmentCount { get; set; }
        public int? InstallmentPeriod { get; set; } // مثلاً 30 (يوم)، 7 (أسبوع)، 1 (شهر)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RemainingDebt { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }
    }
}