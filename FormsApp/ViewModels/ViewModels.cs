using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FormsApp.Models;

namespace FormsApp.ViewModels
{
    // Utility view models kept in this general file
    public class CommentViewModel
    {
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public string AuthorName { get; set; } = string.Empty;
        
        public string AuthorId { get; set; } = string.Empty;
        
        public int TemplateId { get; set; }
    }
    
    public class HomeViewModel
    {
        public List<FormTemplateViewModel> PublicTemplates { get; set; } = new List<FormTemplateViewModel>();
        public List<FormTemplateViewModel> PopularTemplates { get; set; } = new List<FormTemplateViewModel>();
        public List<TagViewModel> TopTags { get; set; } = new List<TagViewModel>();
    }
    
    public class SearchResultViewModel
    {
        public List<FormTemplateViewModel> Templates { get; set; } = new List<FormTemplateViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
    }
    
    public class DashboardViewModel
    {
        public List<FormTemplateViewModel> UserTemplates { get; set; } = new List<FormTemplateViewModel>();
        public List<FormResponseViewModel> UserResponses { get; set; } = new List<FormResponseViewModel>();
    }
} 