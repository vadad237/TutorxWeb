using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class MovePresDateToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_StudentId_ActivityId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "PresentationDate",
                table: "Activities");

            migrationBuilder.AddColumn<DateTime>(
                name: "PresentationDate",
                table: "TaskItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StudentId",
                table: "Assignments",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_StudentId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "PresentationDate",
                table: "TaskItems");

            migrationBuilder.AddColumn<DateTime>(
                name: "PresentationDate",
                table: "Activities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StudentId_ActivityId",
                table: "Assignments",
                columns: new[] { "StudentId", "ActivityId" });
        }
    }
}
