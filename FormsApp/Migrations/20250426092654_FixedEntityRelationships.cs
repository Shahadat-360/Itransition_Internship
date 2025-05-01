using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormsApp.Migrations
{
    /// <inheritdoc />
    public partial class FixedEntityRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccessUsers_AspNetUsers_UserId",
                table: "TemplateAccessUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccessUsers_FormTemplates_TemplateId",
                table: "TemplateAccessUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TemplateAccessUsers",
                table: "TemplateAccessUsers");

            migrationBuilder.RenameTable(
                name: "TemplateAccessUsers",
                newName: "TemplateAccessUser");

            migrationBuilder.RenameIndex(
                name: "IX_TemplateAccessUsers_UserId",
                table: "TemplateAccessUser",
                newName: "IX_TemplateAccessUser_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TemplateAccessUsers_TemplateId",
                table: "TemplateAccessUser",
                newName: "IX_TemplateAccessUser_TemplateId");

            migrationBuilder.AddColumn<int>(
                name: "FormTemplateId",
                table: "Tags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Required",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "FormTemplates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormResponses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "FormResponses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FormResponses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Answers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TemplateAccessUser",
                table: "TemplateAccessUser",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_FormTemplateId",
                table: "Tags",
                column: "FormTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_FormTemplates_FormTemplateId",
                table: "Tags",
                column: "FormTemplateId",
                principalTable: "FormTemplates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccessUser_AspNetUsers_UserId",
                table: "TemplateAccessUser",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccessUser_FormTemplates_TemplateId",
                table: "TemplateAccessUser",
                column: "TemplateId",
                principalTable: "FormTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_FormTemplates_FormTemplateId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccessUser_AspNetUsers_UserId",
                table: "TemplateAccessUser");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccessUser_FormTemplates_TemplateId",
                table: "TemplateAccessUser");

            migrationBuilder.DropIndex(
                name: "IX_Tags_FormTemplateId",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TemplateAccessUser",
                table: "TemplateAccessUser");

            migrationBuilder.DropColumn(
                name: "FormTemplateId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Required",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormResponses");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "FormResponses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FormResponses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "Text",
                table: "Answers");

            migrationBuilder.RenameTable(
                name: "TemplateAccessUser",
                newName: "TemplateAccessUsers");

            migrationBuilder.RenameIndex(
                name: "IX_TemplateAccessUser_UserId",
                table: "TemplateAccessUsers",
                newName: "IX_TemplateAccessUsers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TemplateAccessUser_TemplateId",
                table: "TemplateAccessUsers",
                newName: "IX_TemplateAccessUsers_TemplateId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TemplateAccessUsers",
                table: "TemplateAccessUsers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccessUsers_AspNetUsers_UserId",
                table: "TemplateAccessUsers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccessUsers_FormTemplates_TemplateId",
                table: "TemplateAccessUsers",
                column: "TemplateId",
                principalTable: "FormTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
