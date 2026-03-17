using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("RepairInstallments")]
    public class RepairInstallment
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }
        public decimal RemainingAmount { get; set; }
        public int NumberOfInstallments { get; set; }
        public decimal InstallmentAmount { get; set; }
        public DateTime StartDate { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("DeviceId")]
        public virtual RepairDevice? Device { get; set; }

        public virtual ICollection<RepairInstallmentPayment>? Payments { get; set; }
    }
}