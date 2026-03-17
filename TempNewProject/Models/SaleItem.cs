using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class SaleItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }

        [ForeignKey("SaleId")]
        public virtual Sale? Sale { get; set; }

        [Required]
        public int SaleTypeId { get; set; }

        [ForeignKey("SaleTypeId")]
        public virtual SaleType? SaleType { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ItemPrice { get; set; }

        public int Quantity { get; set; } = 1;

        public int? ProductId { get; set; }

        public int? DeviceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}