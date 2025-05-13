using System;

namespace FormsApp.Models
{
    public class SalesforceUserProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; }  // FK to ApplicationUser
        public DateTime IntegrationDate { get; set; } = DateTime.UtcNow;
        public string? SalesforceAccountId { get; set; }
        public string? SalesforceContactId { get; set; }

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}