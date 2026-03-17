using System.ComponentModel.DataAnnotations;

namespace MobileShopSystem.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        public string Username { get; set; }

        [Required(ErrorMessage = "كلمة السر مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة السر يجب أن تكون 6 أحرف على الأقل")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "إعادة كتابة كلمة السر مطلوبة")]
        [Compare("Password", ErrorMessage = "كلمة السر غير متطابقة")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "رقم الهاتف يجب أن يكون 11 رقم")]
        public string Phone { get; set; }
    }
}
