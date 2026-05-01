using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleStudentsPerTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the FK and unique index for the shadow TaskItemId1 column
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_TaskItems_TaskItemId1",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TaskItemId1",
                table: "Assignments");

            // Drop the unique constraint on TaskItemId (a task can now have many students)
            migrationBuilder.DropIndex(
                name: "IX_Assignments_TaskItemId",
                table: "Assignments");

            // Drop the shadow column
            migrationBuilder.DropColumn(
                name: "TaskItemId1",
                table: "Assignments");

            // Re-create TaskItemId index as non-unique
            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TaskItemId",
                table: "Assignments",
                column: "TaskItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_TaskItemId",
                table: "Assignments");

            migrationBuilder.AddColumn<int>(
                name: "TaskItemId1",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TaskItemId",
                table: "Assignments",
                column: "TaskItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TaskItemId1",
                table: "Assignments",
                column: "TaskItemId1",
                unique: true,
                filter: "[TaskItemId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_TaskItems_TaskItemId1",
                table: "Assignments",
                column: "TaskItemId1",
                principalTable: "TaskItems",
                principalColumn: "Id");
        }
    }
}
