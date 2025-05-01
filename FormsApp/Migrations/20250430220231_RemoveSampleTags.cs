using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormsApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSampleTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the sample tags
            migrationBuilder.Sql(@"
                DELETE FROM Tags 
                WHERE Name IN ('Education', 'Quiz', 'Survey', 'Feedback', 'Customer', 'Employment', 'Technology')
                AND NOT EXISTS (
                    SELECT 1 FROM TemplateTags 
                    WHERE TemplateTags.TagId = Tags.Id
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // We cannot reliably restore deleted tags
        }
    }
}
