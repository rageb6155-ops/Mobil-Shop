using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class DailyClosingMachine
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "يجب إدخال اسم الماكينة")]
        public string MachineName { get; set; } = "Unknown"; // قيمة افتراضية لتجنب null

        [Required(ErrorMessage = "يجب إدخال رصيد الماكينة")]
        public decimal Balance { get; set; } = 0; // قيمة افتراضية لتجنب أخطاء الحفظ

        // مفتاح خارجي يربط الماكينة بتقفيل الحساب
        [Required]
        public int DailyClosingId { get; set; }

        // Navigation property للإشارة إلى DailyClosing
        [ForeignKey("DailyClosingId")]
        public DailyClosing DailyClosing { get; set; } = null!;
    }
}