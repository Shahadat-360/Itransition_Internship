using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class Topic
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for templates with this topic
        public virtual ICollection<FormTemplate> FormTemplates { get; set; } = new List<FormTemplate>();
    }
} 