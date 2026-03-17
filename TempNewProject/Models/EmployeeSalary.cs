using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class EmployeeSalary
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int SalaryMonth { get; set; }

        [Required]
        public int SalaryYear { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAdditions { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDeductions { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalLoans { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "غير مدفوع";

        public DateTime? PaymentDate { get; set; }

        public string? Notes { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // العلاقات
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        public virtual ICollection<EmployeeTransaction> Transactions { get; set; } = new List<EmployeeTransaction>();
    }
}