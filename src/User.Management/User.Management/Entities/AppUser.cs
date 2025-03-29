using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using User.Management.Enum;

namespace User.Management.Entities
{
    public class AppUser:IdentityUser
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Email { get; set; }
        public string Address { get; set; }
        public Status IsActive { get; set; }
        public DateTime LastLoginTime { get; set; }
    }
}
