using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItemIdToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskItemId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TaskItemId",
                table: "Assignments",
                column: "TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_TaskItems_TaskItemId",
                table: "Assignments",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_TaskItems_TaskItemId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TaskItemId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "Assignments");
        }
    }
}
