using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationStudentRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrawHistories_TaskItems_TaskItemId",
                table: "DrawHistories");

            migrationBuilder.AddColumn<byte>(
                name: "Role",
                table: "PresentationStudents",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddForeignKey(
                name: "FK_DrawHistories_TaskItems_TaskItemId",
                table: "DrawHistories",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrawHistories_TaskItems_TaskItemId",
                table: "DrawHistories");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "PresentationStudents");

            migrationBuilder.AddForeignKey(
                name: "FK_DrawHistories_TaskItems_TaskItemId",
                table: "DrawHistories",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
