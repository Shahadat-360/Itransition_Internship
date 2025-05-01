using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FormsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TopicId",
                table: "FormTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8381), "Educational surveys and forms", "Education" },
                    { 2, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8542), "Business-related surveys and forms", "Business" },
                    { 3, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8543), "Health and wellness surveys", "Health" },
                    { 4, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8545), "Technology and IT related forms", "Technology" },
                    { 5, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8546), "Academic and scientific research", "Research" },
                    { 6, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8547), "Customer and user feedback forms", "Feedback" },
                    { 7, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8548), "Event registration and feedback", "Event" },
                    { 8, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8550), "Customer service and support", "Customer Service" },
                    { 9, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8551), "HR and employee forms", "Human Resources" },
                    { 10, new DateTime(2025, 4, 30, 18, 5, 48, 665, DateTimeKind.Utc).AddTicks(8552), "Miscellaneous forms", "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_TopicId",
                table: "FormTemplates",
                column: "TopicId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormTemplates_Topics_TopicId",
                table: "FormTemplates",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormTemplates_Topics_TopicId",
                table: "FormTemplates");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_FormTemplates_TopicId",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "FormTemplates");
        }
    }
}
