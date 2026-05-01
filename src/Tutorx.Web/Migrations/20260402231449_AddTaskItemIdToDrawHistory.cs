using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItemIdToDrawHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskItemId",
                table: "DrawHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawHistories_TaskItemId",
                table: "DrawHistories",
                column: "TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrawHistories_TaskItems_TaskItemId",
                table: "DrawHistories",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrawHistories_TaskItems_TaskItemId",
                table: "DrawHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrawHistories_TaskItemId",
                table: "DrawHistories");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "DrawHistories");
        }
    }
}
