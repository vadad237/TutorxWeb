using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawHistoryActivityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityId",
                table: "DrawHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawHistories_ActivityId",
                table: "DrawHistories",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrawHistories_Activities_ActivityId",
                table: "DrawHistories",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrawHistories_Activities_ActivityId",
                table: "DrawHistories");

            migrationBuilder.DropIndex(
                name: "IX_DrawHistories_ActivityId",
                table: "DrawHistories");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "DrawHistories");
        }
    }
}
