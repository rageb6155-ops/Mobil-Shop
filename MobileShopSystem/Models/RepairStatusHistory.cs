using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("RepairStatusHistories")]
    public class RepairStatusHistory
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; }
        public int ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Notes { get; set; }

        [ForeignKey("DeviceId")]
        public virtual RepairDevice? Device { get; set; }

        [ForeignKey("ChangedBy")]
        public virtual User? Changer { get; set; }
    }
}