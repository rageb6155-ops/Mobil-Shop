using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("RepairDeviceImages")]
    public class RepairDeviceImage
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string ImagePath { get; set; }
        public string? ImageType { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? Notes { get; set; }

        [ForeignKey("DeviceId")]
        public virtual RepairDevice? Device { get; set; }

        [ForeignKey("UploadedBy")]
        public virtual User? Uploader { get; set; }
    }
}