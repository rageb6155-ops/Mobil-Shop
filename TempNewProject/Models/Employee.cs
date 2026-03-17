using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? IDNumber { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentSalary { get; set; } = 0;

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }

        public string? Notes { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "نشط";

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
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

        public virtual ICollection<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();
        public virtual ICollection<EmployeeTransaction> Transactions { get; set; } = new List<EmployeeTransaction>();
        public virtual ICollection<SalaryChangeLog> SalaryChangeLogs { get; set; } = new List<SalaryChangeLog>();
    }
}