using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorx.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityAttributes_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityAttributeOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActivityAttributeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAttributeOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityAttributeOptions_ActivityAttributes_ActivityAttributeId",
                        column: x => x.ActivityAttributeId,
                        principalTable: "ActivityAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ActivityAttributeId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAttributeValues_ActivityAttributeOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "ActivityAttributeOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentAttributeValues_ActivityAttributes_ActivityAttributeId",
                        column: x => x.ActivityAttributeId,
                        principalTable: "ActivityAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAttributeValues_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAttributeOptions_ActivityAttributeId",
                table: "ActivityAttributeOptions",
                column: "ActivityAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAttributes_ActivityId",
                table: "ActivityAttributes",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttributeValues_ActivityAttributeId",
                table: "StudentAttributeValues",
                column: "ActivityAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttributeValues_OptionId",
                table: "StudentAttributeValues",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttributeValues_StudentId_ActivityAttributeId",
                table: "StudentAttributeValues",
                columns: new[] { "StudentId", "ActivityAttributeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAttributeValues");

            migrationBuilder.DropTable(
                name: "ActivityAttributeOptions");

            migrationBuilder.DropTable(
                name: "ActivityAttributes");
        }
    }
}
