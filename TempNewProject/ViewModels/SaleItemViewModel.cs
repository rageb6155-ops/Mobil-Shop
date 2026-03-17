namespace MobileShopSystem.ViewModels
{
    public class SaleItemViewModel
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal ItemPrice { get; set; }
        public string SaleTypeName { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public int? DeviceId { get; set; }
    }
}