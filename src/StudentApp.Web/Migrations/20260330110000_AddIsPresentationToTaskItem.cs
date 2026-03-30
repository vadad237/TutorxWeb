using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPresentationToTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPresentation",
                table: "TaskItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPresentation",
                table: "TaskItems");
        }
    }
}
