using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormsApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSampleTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete the sample topics
            migrationBuilder.Sql(@"
                -- First, update any templates using these topics to have null TopicId
                UPDATE FormTemplates 
                SET TopicId = NULL
                WHERE TopicId IN (
                    SELECT Id FROM Topics 
                    WHERE Name IN ('Education', 'Quiz', 'Survey', 'Feedback', 'Registration', 'Other')
                );

                -- Then delete the sample topics
                DELETE FROM Topics 
                WHERE Name IN ('Education', 'Quiz', 'Survey', 'Feedback', 'Registration', 'Other');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // We don't restore the sample topics in the Down method
            // If needed, they will be recreated by the DbInitializer
        }
    }
}
