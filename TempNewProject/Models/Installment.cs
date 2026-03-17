using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class Installment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [Required]
        public int InstallmentNumber { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        public DateTime? PaidDate { get; set; }

        public bool IsPaid { get; set; } = false;

        public int? PaidBy { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        public string? Notes { get; set; }

        // العلاقات
        [ForeignKey("TransactionId")]
        public virtual CustomerTransaction? Transaction { get; set; }

        [ForeignKey("PaidBy")]
        public virtual User? Payer { get; set; }
    }
}