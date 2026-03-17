using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string CustomerCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? AlternativePhone { get; set; }

        [MaxLength(50)]
        public string? IDNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public string? Notes { get; set; }

        [MaxLength(50)]
        public string CustomerType { get; set; } = "عادي";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDebtLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentDebt { get; set; } = 0;

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public int? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        // العلاقات
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? Updater { get; set; }

        [ForeignKey("DeletedBy")]
        public virtual User? Deleter { get; set; }

        public virtual ICollection<CustomerTransaction> Transactions { get; set; } = new List<CustomerTransaction>();
    }
}