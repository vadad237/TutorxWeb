using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentApp.Web.Migrations
{
    public partial class AssignmentsToActivityLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use IF EXISTS guards because a previous partial run may have already removed some of these.
            migrationBuilder.Sql(@"
                IF OBJECT_ID('FK_Assignments_TaskItems_TaskItemId', 'F') IS NOT NULL
                    ALTER TABLE [Assignments] DROP CONSTRAINT [FK_Assignments_TaskItems_TaskItemId];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Assignments_TaskItemId' AND object_id = OBJECT_ID('Assignments'))
                    DROP INDEX [IX_Assignments_TaskItemId] ON [Assignments];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Assignments_StudentId' AND object_id = OBJECT_ID('Assignments'))
                    DROP INDEX [IX_Assignments_StudentId] ON [Assignments];
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('Assignments', 'TaskItemId') IS NOT NULL
                BEGIN
                    DECLARE @var0 sysname;
                    SELECT @var0 = [d].[name]
                    FROM [sys].[default_constraints] [d]
                    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Assignments]') AND [c].[name] = N'TaskItemId');
                    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Assignments] DROP CONSTRAINT [' + @var0 + '];');
                    ALTER TABLE [Assignments] DROP COLUMN [TaskItemId];
                END
            ");

            // Remove duplicate (StudentId, ActivityId) rows introduced when assignments were task-level.
            // Keep only the lowest Id per (StudentId, ActivityId) pair.
            migrationBuilder.Sql(@"
                DELETE a
                FROM Assignments a
                WHERE a.Id NOT IN (
                    SELECT MIN(Id) FROM Assignments GROUP BY StudentId, ActivityId
                )
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Assignments_StudentId_ActivityId' AND object_id = OBJECT_ID('Assignments'))
                    CREATE UNIQUE INDEX [IX_Assignments_StudentId_ActivityId] ON [Assignments] ([StudentId], [ActivityId]);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_StudentId_ActivityId",
                table: "Assignments");

            migrationBuilder.AddColumn<int>(
                name: "TaskItemId",
                table: "Assignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TaskItemId",
                table: "Assignments",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StudentId",
                table: "Assignments",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_TaskItems_TaskItemId",
                table: "Assignments",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
