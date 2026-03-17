namespace MobileShopSystem.Models
{
    public class DeviceSnapshot
    {
        public string Name { get; set; }
        public string Serial { get; set; }
        public int Storage { get; set; }
        public int RAM { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public string OwnerType { get; set; }
        public string OwnerName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerID { get; set; }
    }
}