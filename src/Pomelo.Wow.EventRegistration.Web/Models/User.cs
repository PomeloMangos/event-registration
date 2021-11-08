using System.ComponentModel.DataAnnotations;

namespace Pomelo.Wow.EventRegistration.Web.Models
{
    public enum UserRole
    { 
        Admin,
        User
    }

    public class User
    {
        public int Id { get; set; }

        [MaxLength(32)]
        public string Username { get; set; }

        [MaxLength(32)]
        public string DisplayName { get; set; }

        [MaxLength(128)]
        public string PasswordHash { get; set; }

        public UserRole Role { get; set; }
    }
}
