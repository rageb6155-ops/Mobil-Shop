using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("RepairInstallmentPayments")]
    public class RepairInstallmentPayment
    {
        [Key]
        public int Id { get; set; }
        public int InstallmentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public bool IsPaid { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }

        [ForeignKey("InstallmentId")]
        public virtual RepairInstallment? Installment { get; set; }
    }
}