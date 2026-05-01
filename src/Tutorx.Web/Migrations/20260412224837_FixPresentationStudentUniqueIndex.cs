using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixPresentationStudentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PresentationStudents_TaskItemId_StudentId",
                table: "PresentationStudents");

            migrationBuilder.CreateIndex(
                name: "IX_PresentationStudents_TaskItemId_StudentId_Role",
                table: "PresentationStudents",
                columns: new[] { "TaskItemId", "StudentId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PresentationStudents_TaskItemId_StudentId_Role",
                table: "PresentationStudents");

            migrationBuilder.CreateIndex(
                name: "IX_PresentationStudents_TaskItemId_StudentId",
                table: "PresentationStudents",
                columns: new[] { "TaskItemId", "StudentId" },
                unique: true);
        }
    }
}
