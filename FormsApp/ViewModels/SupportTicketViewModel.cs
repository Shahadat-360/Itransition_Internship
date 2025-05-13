using System.ComponentModel.DataAnnotations;

namespace FormsApp.ViewModels
{
    public class SupportTicketViewModel
    {
        [Required(ErrorMessage = "Please provide a summary of your issue")]
        [Display(Name = "Summary")]
        public string Summary { get; set; }

        [Required(ErrorMessage = "Please select a priority")]
        [Display(Name = "Priority")]
        public string Priority { get; set; }

        // Hidden fields - explicitly marked as not required
        public string ReportedBy { get; set; } = string.Empty;
        
        // Template field with default value
        [Display(Name = "Template")]
        public string Template { get; set; } = "NoTemplate";
        
        public string Link { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }
} 