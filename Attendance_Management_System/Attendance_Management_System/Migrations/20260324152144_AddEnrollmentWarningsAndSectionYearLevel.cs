using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentWarningsAndSectionYearLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_AspNetUsers_ProcessedBy",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SectionId",
                table: "Schedules");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Teachers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SectionId1",
                table: "SectionTeachers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherId1",
                table: "SectionTeachers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearLevel",
                table: "Sections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasWarning",
                table: "Enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SectionId1",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarningMessage",
                table: "Enrollments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionTeachers_SectionId1",
                table: "SectionTeachers",
                column: "SectionId1");

            migrationBuilder.CreateIndex(
                name: "IX_SectionTeachers_TeacherId1",
                table: "SectionTeachers",
                column: "TeacherId1");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SectionId_DayOfWeek",
                table: "Schedules",
                columns: new[] { "SectionId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SectionId1",
                table: "Enrollments",
                column: "SectionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_AspNetUsers_ProcessedBy",
                table: "Enrollments",
                column: "ProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Sections_SectionId1",
                table: "Enrollments",
                column: "SectionId1",
                principalTable: "Sections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionTeachers_Sections_SectionId1",
                table: "SectionTeachers",
                column: "SectionId1",
                principalTable: "Sections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionTeachers_Teachers_TeacherId1",
                table: "SectionTeachers",
                column: "TeacherId1",
                principalTable: "Teachers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_AspNetUsers_ProcessedBy",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Sections_SectionId1",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionTeachers_Sections_SectionId1",
                table: "SectionTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionTeachers_Teachers_TeacherId1",
                table: "SectionTeachers");

            migrationBuilder.DropIndex(
                name: "IX_SectionTeachers_SectionId1",
                table: "SectionTeachers");

            migrationBuilder.DropIndex(
                name: "IX_SectionTeachers_TeacherId1",
                table: "SectionTeachers");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SectionId_DayOfWeek",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SectionId1",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "SectionTeachers");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "SectionTeachers");

            migrationBuilder.DropColumn(
                name: "YearLevel",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "HasWarning",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "WarningMessage",
                table: "Enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SectionId",
                table: "Schedules",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_AspNetUsers_ProcessedBy",
                table: "Enrollments",
                column: "ProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
