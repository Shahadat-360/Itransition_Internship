using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FormsApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
