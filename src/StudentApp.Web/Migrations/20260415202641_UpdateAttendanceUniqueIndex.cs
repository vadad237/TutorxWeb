using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendanceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId_GroupId_Date",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_GroupId_Date_Time",
                table: "Attendances",
                columns: new[] { "StudentId", "GroupId", "Date", "Time" },
                unique: true,
                filter: "[Time] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId_GroupId_Date_Time",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_GroupId_Date",
                table: "Attendances",
                columns: new[] { "StudentId", "GroupId", "Date" },
                unique: true);
        }
    }
}
