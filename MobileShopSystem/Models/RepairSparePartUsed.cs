using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("RepairSparePartsUsed")]
    public class RepairSparePartUsed
    {
        [Key]
        public int Id { get; set; }
        public int RepairId { get; set; }
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime UsedAt { get; set; }
        public string? Notes { get; set; }

        [ForeignKey("RepairId")]
        public virtual RepairDevice? Repair { get; set; }

        [ForeignKey("PartId")]
        public virtual SparePart? Part { get; set; }
    }
}