using FormsApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FormsApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<FormTemplate> FormTemplates { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<FormResponse> FormResponses { get; set; } = null!;
        public DbSet<Answer> Answers { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<TemplateTag> TemplateTags { get; set; } = null!;
        public DbSet<TemplateLike> TemplateLikes { get; set; } = null!;
        public DbSet<TemplateAccessUser> TemplateAccessUsers { get; set; } = null!;
        public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
        public DbSet<Topic> Topics { get; set; } = null!;
        public DbSet<ApiToken> ApiTokens { get; set; } = null!;

        // Salesforce integration
        public DbSet<SalesforceUserProfile> SalesforceUserProfiles { get; set; } = null!;

        // Alias for TemplateAccessUsers for backward compatibility
        public DbSet<TemplateAccessUser> AllowedUsers => TemplateAccessUsers;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Suppress the pending model changes warning
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(RelationalEventId.ModelSnapshotNotFound, 
                                RelationalEventId.PendingModelChangesWarning));
            
            base.OnConfiguring(optionsBuilder);
        }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Add unique index for email
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique();
            
            // Configure relationships and constraints
            builder.Entity<FormTemplate>()
                .HasOne(t => t.Creator)
                .WithMany(u => u.CreatedTemplates)
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<FormResponse>()
                .HasOne(r => r.Respondent)
                .WithMany(u => u.FilledForms)
                .HasForeignKey(r => r.RespondentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<FormResponse>()
                .HasOne(r => r.Template)
                .WithMany(t => t.Responses)
                .HasForeignKey(r => r.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Question>()
                .HasOne(q => q.Template)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<Answer>()
                .HasOne(a => a.Response)
                .WithMany(r => r.Answers)
                .HasForeignKey(a => a.ResponseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Comment>()
                .HasOne(c => c.Template)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<TemplateTag>()
                .HasOne(tt => tt.Template)
                .WithMany(t => t.TemplateTags)
                .HasForeignKey(tt => tt.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<TemplateTag>()
                .HasOne(tt => tt.Tag)
                .WithMany(t => t.TemplateTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<TemplateLike>()
                .HasOne(tl => tl.Template)
                .WithMany(t => t.Likes)
                .HasForeignKey(tl => tl.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<TemplateLike>()
                .HasOne(tl => tl.User)
                .WithMany()
                .HasForeignKey(tl => tl.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<TemplateAccessUser>()
                .HasOne(ta => ta.Template)
                .WithMany(t => t.AllowedUsers)
                .HasForeignKey(ta => ta.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<TemplateAccessUser>()
                .HasOne(ta => ta.User)
                .WithMany()
                .HasForeignKey(ta => ta.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<QuestionOption>()
                .HasOne(qo => qo.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(qo => qo.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Configure FormTemplate to use Topic - making it optional during migration
            builder.Entity<FormTemplate>()
                .HasOne(t => t.TopicNavigation)
                .WithMany(topic => topic.FormTemplates)
                .HasForeignKey(t => t.TopicId)
                .IsRequired(false) // Allow NULL for TopicId during migration
                .OnDelete(DeleteBehavior.SetNull);

            // Configure SalesforceUserProfile
            builder.Entity<SalesforceUserProfile>()
                .HasOne(s => s.User)
                .WithOne(u => u.SalesforceProfile)
                .HasForeignKey<SalesforceUserProfile>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ApiToken
            builder.Entity<ApiToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.ApiTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for faster token lookups
            builder.Entity<ApiToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            // Seed default topics removed - users will create their own topics
        }
    }
} 