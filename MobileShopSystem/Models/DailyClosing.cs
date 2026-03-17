using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class DailyClosing
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "يجب إدخال تاريخ الإغلاق")]
        public DateTime ClosingDate { get; set; } = DateTime.Now; // قيمة افتراضية لتجنب null

        [Required(ErrorMessage = "يجب إدخال الرصيد النقدي")]
        public decimal CashLeft { get; set; } = 0; // قيمة افتراضية لتجنب أخطاء الحفظ

        [Required(ErrorMessage = "يجب إدخال قيمة الفضة")]
        public decimal CoinsAmount { get; set; } = 0; // قيمة افتراضية

        [Required]
        public string CreatedBy { get; set; } = "Unknown"; // اسم المستخدم الافتراضي

        public DateTime CreatedAt { get; set; } = DateTime.Now; // تاريخ الإنشاء الافتراضي
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false; // لتظليل السجلات المحذوفة
        public bool IsEdited { get; set; } = false;  // لتحديد السجلات المعدلة

        // JSON لتخزين نسخة البيانات قبل التعديل
        public string? PreviousDataJson { get; set; }

        // خاصية غير مخزنة في قاعدة البيانات لفك تشفير النسخة السابقة
        [NotMapped]
        public DailyClosingSnapshot? PreviousData
        {
            get => !string.IsNullOrEmpty(PreviousDataJson)
                   ? JsonSerializer.Deserialize<DailyClosingSnapshot>(PreviousDataJson)
                   : null;
            set => PreviousDataJson = value != null ? JsonSerializer.Serialize(value) : null;
        }

        // Navigation property للماكينات المرتبطة
        public ICollection<DailyClosingMachine> Machines { get; set; } = new List<DailyClosingMachine>();
    }

    // Snapshot لتخزين نسخة قبل التعديل
    public class DailyClosingSnapshot
    {
        public decimal CashLeft { get; set; }
        public decimal CoinsAmount { get; set; }
        public List<DailyClosingMachine> Machines { get; set; } = new List<DailyClosingMachine>();
    }
}