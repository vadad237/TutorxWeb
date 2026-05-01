using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class EvaluationConnectToTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Activities_ActivityId",
                table: "Evaluations");

            migrationBuilder.RenameColumn(
                name: "ActivityId",
                table: "Evaluations",
                newName: "TaskItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Evaluations_StudentId_ActivityId",
                table: "Evaluations",
                newName: "IX_Evaluations_StudentId_TaskItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Evaluations_ActivityId",
                table: "Evaluations",
                newName: "IX_Evaluations_TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_TaskItems_TaskItemId",
                table: "Evaluations",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_TaskItems_TaskItemId",
                table: "Evaluations");

            migrationBuilder.RenameColumn(
                name: "TaskItemId",
                table: "Evaluations",
                newName: "ActivityId");

            migrationBuilder.RenameIndex(
                name: "IX_Evaluations_TaskItemId",
                table: "Evaluations",
                newName: "IX_Evaluations_ActivityId");

            migrationBuilder.RenameIndex(
                name: "IX_Evaluations_StudentId_TaskItemId",
                table: "Evaluations",
                newName: "IX_Evaluations_StudentId_ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Activities_ActivityId",
                table: "Evaluations",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
