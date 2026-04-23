using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToDrawHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Role",
                table: "DrawHistories",
                type: "tinyint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "DrawHistories");
        }
    }
}
