using System.ComponentModel.DataAnnotations;

namespace FormsApp.ViewModels
{
    public class SalesforceProfileViewModel
    {
        // Account Information
        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Display(Name = "Industry")]
        public string Industry { get; set; }

        [Display(Name = "Company Description")]
        public string CompanyDescription { get; set; }

        [Display(Name = "Business Phone")]
        [Phone]
        public string BusinessPhone { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Country")]
        public string Country { get; set; }

        // Contact Information
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }
    }
}