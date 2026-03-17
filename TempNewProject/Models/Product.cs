using System;

namespace MobileShopSystem.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ShortCode { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;
        public decimal FactoryPrice { get; set; }
        public decimal SalePrice { get; set; }     // موجود في DB
        public string PurchasePlace { get; set; } = null!;
        public string StoragePlace { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // العلاقة مع المستخدم الذي أضاف المنتج
        public int AddedByUserId { get; set; }
        public User AddedByUser { get; set; } = null!;
    }
}