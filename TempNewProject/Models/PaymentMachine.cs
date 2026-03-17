using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobileShopSystem.Models
{
    public class PaymentMachine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MachineName { get; set; }

        public decimal Amount { get; set; }

        // FK
        public int DailyClosingId { get; set; }

        [ForeignKey("DailyClosingId")]
        public DailyClosing DailyClosing { get; set; }
    }
}
