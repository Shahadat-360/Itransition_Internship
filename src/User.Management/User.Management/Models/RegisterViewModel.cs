using System.ComponentModel.DataAnnotations;
using User.Management.Enum;

namespace User.Management.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage ="First Name is Required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage ="Last Name is Required")]
        public string LastName { get; set; }
        
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(1, ErrorMessage = "Password must be at least 1 characters long.")]
        public string Password { get; set; }
        public string Address { get; set; }
        public Status IsActive { get; set; } = Status.Active;
        public DateTime LastLoginTime { get; set; }= DateTime.Now;
    }
}
