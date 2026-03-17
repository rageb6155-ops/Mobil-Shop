using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("Warranties")]
    public class Warranty
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string WarrantyNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WarrantyType { get; set; }
        public string? Coverage { get; set; }
        public decimal? Cost { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("DeviceId")]
        public virtual RepairDevice? Device { get; set; }
    }
}