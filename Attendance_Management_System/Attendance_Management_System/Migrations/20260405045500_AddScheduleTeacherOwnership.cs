using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleTeacherOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "Schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Schedules" AS s
                SET "TeacherId" = (
                    SELECT st."TeacherId"
                    FROM "SectionTeachers" AS st
                    WHERE st."SectionId" = s."SectionId"
                    ORDER BY st."AssignedAt", st."TeacherId"
                    LIMIT 1
                )
                WHERE s."TeacherId" IS NULL;
            """);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TeacherId_DayOfWeek",
                table: "Schedules",
                columns: new[] { "TeacherId", "DayOfWeek" });

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Teachers_TeacherId",
                table: "Schedules",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Teachers_TeacherId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TeacherId_DayOfWeek",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Schedules");
        }
    }
}
