using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCardNumberUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_CardNumber",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CardNumber",
                table: "Students",
                column: "CardNumber",
                filter: "[CardNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_CardNumber",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CardNumber",
                table: "Students",
                column: "CardNumber",
                unique: true,
                filter: "[CardNumber] IS NOT NULL");
        }
    }
}
