using System;
using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        [Required]
        public string ReportedBy { get; set; }

        [Required]
        public string Summary { get; set; }

        // This field is optional and has a default value
        [Display(Name = "Template")]
        public string Template { get; set; } = "NoTemplate";

        [Required]
        public string Link { get; set; }

        [Required]
        public string Priority { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 