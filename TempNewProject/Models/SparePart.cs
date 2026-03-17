using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("SpareParts")]
    public class SparePart
    {
        [Key]
        public int Id { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public string? CompatibleModels { get; set; }
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
        public decimal Cost { get; set; }
        public decimal SellingPrice { get; set; }
        public string? Supplier { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<RepairSparePartUsed>? RepairsUsed { get; set; }
    }
}