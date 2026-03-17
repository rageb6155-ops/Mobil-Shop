using System;

namespace MobileShopSystem.Models
{
    public class DeviceHistory
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string OldData { get; set; } = string.Empty;
        public string NewData { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
    }
}