using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class SaleModificationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }

        [ForeignKey("SaleId")]
        public virtual Sale? Sale { get; set; }

        [Required]
        public int ModifiedBy { get; set; }

        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string ModificationType { get; set; } = string.Empty; // 'تعديل' أو 'حذف'

        public string? OldData { get; set; } // JSON (للتوافق مع الإصدار السابق)

        public string? NewData { get; set; } // JSON (للتوافق مع الإصدار السابق)

        // ===== الحقول الجديدة =====
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldTotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NewTotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NewPaidAmount { get; set; }

        public string? OldItemsJson { get; set; } // JSON للعناصر القديمة

        public string? NewItemsJson { get; set; } // JSON للعناصر الجديدة
    }
}