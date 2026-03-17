using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = null!;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        public bool IsAdmin { get; set; } = false;

        public bool IsApproved { get; set; } = false;

        public bool IsBlocked { get; set; } = false;

        // ⭐⭐⭐ خاصية جديدة: تعليق المستخدم
        public bool IsSuspended { get; set; } = false;

        // ⭐⭐⭐ خاصية جديدة: رسالة التعليق
        [StringLength(500)]
        public string? SuspensionMessage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLogin { get; set; }

        public DateTime? LastLogout { get; set; }

        public bool IsOnline { get; set; } = false;

        public string? LogoutMessage { get; set; }

        // ===== خصائص الحذف الناعم (Soft Delete) =====
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }

        // العلاقة مع المستخدم الذي قام بالحذف
        [ForeignKey("DeletedBy")]
        public virtual User? Deleter { get; set; }

        // ⭐ دالة مساعدة: التحقق مما إذا كان المستخدم نشط (قادر على تسجيل الدخول)
        public bool CanLogin()
        {
            return !IsDeleted && IsApproved && !IsBlocked && !IsSuspended;
        }

        // ⭐ دالة مساعدة: الحصول على سبب عدم القدرة على تسجيل الدخول
        public string GetLoginRestrictionReason()
        {
            if (IsDeleted) return "الحساب محذوف";
            if (!IsApproved) return "الحساب قيد الموافقة";
            if (IsBlocked) return "الحساب محظور";
            if (IsSuspended) return "الحساب معلق";
            return string.Empty;
        }

        // ⭐ دالة مساعدة: الحصول على حالة المستخدم كنص
        public string GetStatusText()
        {
            if (IsDeleted) return "محذوف";
            if (!IsApproved) return "قيد الموافقة";
            if (IsBlocked) return "محظور";
            if (IsSuspended) return "معلق";
            if (IsOnline) return "متصل";
            return "غير متصل";
        }

        // ⭐ دالة مساعدة: الحصول على لون الحالة (للاستخدام في CSS)
        public string GetStatusColor()
        {
            if (IsDeleted || IsBlocked || IsSuspended) return "#dc3545"; // أحمر
            if (!IsApproved) return "#ffc107"; // أصفر
            if (IsOnline) return "#28a745"; // أخضر
            return "#6c757d"; // رمادي
        }
    }
}