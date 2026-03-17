using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class SalaryChangeLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OldSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewSalary { get; set; }

        public DateTime ChangeDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required]
        public int ChangedBy { get; set; }

        // العلاقات
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("ChangedBy")]
        public virtual User? Changer { get; set; }
    }
}