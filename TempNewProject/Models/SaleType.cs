using System.ComponentModel.DataAnnotations;

namespace MobileShopSystem.Models
{
    public class SaleType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}