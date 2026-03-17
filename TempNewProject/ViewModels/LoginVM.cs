using System.ComponentModel.DataAnnotations;

namespace MobileShopSystem.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "الرجاء إدخال اسم المستخدم")]
        public string Username { get; set; }

        [Required(ErrorMessage = "الرجاء إدخال كلمة السر")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
