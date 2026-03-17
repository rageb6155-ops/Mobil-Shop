using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class QuickCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CodeName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CodeValue { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedBy { get; set; }
    }
}