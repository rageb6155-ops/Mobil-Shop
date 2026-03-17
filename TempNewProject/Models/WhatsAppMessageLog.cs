using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    [Table("WhatsAppMessageLogs")]
    public class WhatsAppMessageLog
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string CustomerPhone { get; set; }
        public string MessageType { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsSuccess { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }

        [ForeignKey("DeviceId")]
        public virtual RepairDevice? Device { get; set; }
    }
}