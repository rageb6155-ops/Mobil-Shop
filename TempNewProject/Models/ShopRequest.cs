using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class ShopRequest
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "اسم القطعة")]
        [StringLength(200)]
        public string? ItemName { get; set; }

        [Display(Name = "النوع")]
        [StringLength(50)]
        public string? ItemType { get; set; }

        [Display(Name = "الكمية")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "سعر الوحدة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [Display(Name = "السعر الإجمالي")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalPrice { get; set; }

        [Display(Name = "المورد")]
        [StringLength(200)]
        public string? Supplier { get; set; }

        [Display(Name = "رقم المورد")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "ملاحظات")]
        [StringLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "الأولوية")]
        public int Priority { get; set; } = 1;

        [Display(Name = "الحالة")]
        [StringLength(50)]
        public string Status { get; set; } = "قيد الانتظار";

        [Display(Name = "تاريخ الطلب")]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Display(Name = "تاريخ التسليم المطلوب")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        // الحقول الخاصة بالحذف الناعم
        public bool IsDeleted { get; set; } = false;
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        // العلاقات
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? Updater { get; set; }

        [ForeignKey("DeletedBy")]
        public virtual User? Deleter { get; set; }

        // قائمة الأنواع المتاحة (للاستخدام في Dropdown - اختياري)
        public static readonly string[] ItemTypes = new[]
        {
            "شاحن",
            "اسكرينة",
            "بطارية",
            "كفر",
            "سماعة",
            "قطعة غيار",
            "أخرى"
        };

        // قائمة الأولويات
        public static readonly Dictionary<int, string> Priorities = new()
        {
            { 1, "عادي" },
            { 2, "مهم" },
            { 3, "عاجل" }
        };

        // قائمة الحالات
        public static readonly string[] Statuses = new[]
        {
            "قيد الانتظار",
            "تم الطلب",
            "تم الاستلام",
            "ملغي"
        };
    }

    public class ShopRequestWhatsAppLog
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string PhoneNumber { get; set; }
        public string MessageType { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsSuccess { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }

        [ForeignKey("RequestId")]
        public virtual ShopRequest Request { get; set; }
    }
}