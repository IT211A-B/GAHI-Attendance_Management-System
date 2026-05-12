using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class FixShadowFkRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_Enrollments_SectionId1",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "SectionTeachers");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "SectionTeachers");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "Enrollments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "SectionId1",
                table: "Enrollments",
                type: "integer",
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
                name: "IX_Enrollments_SectionId1",
                table: "Enrollments",
                column: "SectionId1");

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
    }
}
