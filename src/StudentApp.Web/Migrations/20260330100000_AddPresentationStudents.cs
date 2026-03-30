using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresentationStudents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskItemId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentationStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresentationStudents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentationStudents_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PresentationStudents_StudentId",
                table: "PresentationStudents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PresentationStudents_TaskItemId_StudentId",
                table: "PresentationStudents",
                columns: new[] { "TaskItemId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PresentationStudents");
        }
    }
}
