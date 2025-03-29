using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using User.Management.Enum;

namespace User.Management.Entities
{
    public class AppUser : IdentityUser
    {
        [Key]
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string JobTitle { get; set; }
        public Status IsActive { get; set; }
        public DateTime? LastLoginTime { get; set; }
    }
}
