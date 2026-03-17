using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("RepairRatings")]
    public class RepairRating
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int? CustomerSatisfaction { get; set; }
        public int? TechnicianRating { get; set; }
        public int? PriceRating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("DeviceId")]
        public virtual RepairDevice? Device { get; set; }
    }
}