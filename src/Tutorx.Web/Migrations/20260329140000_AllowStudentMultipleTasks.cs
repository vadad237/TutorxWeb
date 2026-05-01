using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class AllowStudentMultipleTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the unique constraint that prevented a student from being
            // assigned to more than one task within the same activity.
            migrationBuilder.DropIndex(
                name: "IX_Assignments_StudentId_ActivityId",
                table: "Assignments");

            // Re-create as a non-unique index so queries on (StudentId, ActivityId) remain fast.
            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StudentId_ActivityId",
                table: "Assignments",
                columns: new[] { "StudentId", "ActivityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_StudentId_ActivityId",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StudentId_ActivityId",
                table: "Assignments",
                columns: new[] { "StudentId", "ActivityId" },
                unique: true);
        }
    }
}
