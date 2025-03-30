using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace User.Management.Migrations
{
    /// <inheritdoc />
    public partial class AddressFieldToJobTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "AspNetUsers",
                newName: "JobTitle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JobTitle",
                table: "AspNetUsers",
                newName: "Address");
        }
    }
}
