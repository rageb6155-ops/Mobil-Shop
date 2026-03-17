using System;

namespace MobileShopSystem.ViewModels
{
    public class QuickCodeViewModel
    {
        public int Id { get; set; }
        public string CodeName { get; set; } = string.Empty;
        public string CodeValue { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}