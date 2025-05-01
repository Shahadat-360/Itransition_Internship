using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FormsApp.Data.DataMigrations
{
    public static class TopicIdResetUtility
    {
        public static async Task ResetTopicIdsAsync(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Only reset TopicIds that reference non-existent topics
                await dbContext.Database.ExecuteSqlRawAsync(@"
                    UPDATE FormTemplates 
                    SET TopicId = NULL 
                    WHERE TopicId IS NOT NULL AND TopicId NOT IN (SELECT Id FROM Topics)");
                
                Console.WriteLine("Successfully reset orphaned TopicIds to NULL");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting TopicIds: {ex.Message}");
            }
        }
    }
} 