namespace MobileShopSystem.Models
{
    public class DeviceIndexViewModel
    {
        public List<Device> Devices { get; set; } = new List<Device>();
        public Device DeviceForm { get; set; } = new Device();
    }
}