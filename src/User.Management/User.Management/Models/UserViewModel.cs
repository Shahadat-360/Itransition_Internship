using User.Management.Enum;

namespace User.Management.Models
{
    public class UserViewModel
    {
        public string FirstName {  get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string JobTitle { get; set; }
        public string LastSeenText { get; set; }
        public bool IsActive { get; set; }
        public bool IsSelected { get; set; } = false;
    }
}
