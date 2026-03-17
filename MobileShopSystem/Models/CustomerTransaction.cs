using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class CustomerTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public int? SaleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty; // 'دين', 'دفعة', 'قسط', 'تسوية'

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public DateTime? DueDate { get; set; }

        public string? Notes { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsInstallment { get; set; } = false;
        public int? InstallmentCount { get; set; }
        public int InstallmentPaidCount { get; set; } = 0;

        [MaxLength(50)]
        public string Status { get; set; } = "نشط"; // نشط، مكتمل، متأخر، ملغي

        // العلاقات
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("SaleId")]
        public virtual Sale? Sale { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        public virtual ICollection<Installment> Installments { get; set; } = new List<Installment>();
    }
}