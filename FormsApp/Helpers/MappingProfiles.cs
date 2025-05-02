using AutoMapper;
using FormsApp.Models;
using FormsApp.ViewModels;
using System.Linq;

namespace FormsApp.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            // User mapping
            CreateMap<ApplicationUser, UserViewModel>()
                .ForMember(dest => dest.IsAdmin, opt => opt.Ignore()); // We'll handle admin role separately
                
            // FormTemplate mapping
            CreateMap<FormTemplate, FormTemplateViewModel>()
                .ForMember(dest => dest.Topic, opt => opt.MapFrom(src => src.TopicNavigation != null ? src.TopicNavigation.Name : "Other"))
                .ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator.UserName))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src => src.Comments.Count))
                .ForMember(dest => dest.TagIds, opt => opt.MapFrom(src => src.TemplateTags.Select(tt => tt.TagId).ToList()))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.TemplateTags.Select(tt => tt.Tag.Name).ToList()))
                .ForMember(dest => dest.AllowedUserEmails, opt => opt.MapFrom(src => src.AllowedUsers.Where(u => !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).ToList()))
                .ForMember(dest => dest.ImageFile, opt => opt.Ignore()) // Ignore upload form field
                .ForMember(dest => dest.CurrentUserLiked, opt => opt.Ignore()); // Requires user context
                
            // Question mapping
            CreateMap<Question, QuestionViewModel>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options));
                
            // Option mapping
            CreateMap<QuestionOption, OptionViewModel>();
            
            // Comment mapping
            CreateMap<Comment, CommentViewModel>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.UserName));
                
            // Tag mapping
            CreateMap<Tag, TagViewModel>();
        }
    }
} 