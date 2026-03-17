using System;

namespace MobileShopSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Phone { get; set; }

        public bool IsAdmin { get; set; }
        public bool IsApproved { get; set; }
        public bool IsBlocked { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastLogout { get; set; }

        public bool IsOnline { get; set; }
        public string? LogoutMessage { get; set; }
    }
}