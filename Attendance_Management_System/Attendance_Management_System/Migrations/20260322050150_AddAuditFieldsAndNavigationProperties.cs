using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsAndNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Teachers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Teachers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseId1",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Subjects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionId1",
                table: "Students",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Students",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Students",
                type: "integer",
                nullable: true);

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
                name: "SubjectId1",
                table: "Sections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Sections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Sections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionId1",
                table: "Schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId1",
                table: "Schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Schedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionId1",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Courses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Courses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Classrooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Classrooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Attendances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Attendances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AttendanceReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "AttendanceReports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AcademicYears",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "AcademicYears",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_CourseId1",
                table: "Subjects",
                column: "CourseId1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_SectionId1",
                table: "Students",
                column: "SectionId1");

            migrationBuilder.CreateIndex(
                name: "IX_SectionTeachers_SectionId1",
                table: "SectionTeachers",
                column: "SectionId1");

            migrationBuilder.CreateIndex(
                name: "IX_SectionTeachers_TeacherId1",
                table: "SectionTeachers",
                column: "TeacherId1");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_SubjectId1",
                table: "Sections",
                column: "SubjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SectionId1",
                table: "Schedules",
                column: "SectionId1");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SubjectId1",
                table: "Schedules",
                column: "SubjectId1");

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
                name: "FK_Schedules_Sections_SectionId1",
                table: "Schedules",
                column: "SectionId1",
                principalTable: "Sections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Subjects_SubjectId1",
                table: "Schedules",
                column: "SubjectId1",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Subjects_SubjectId1",
                table: "Sections",
                column: "SubjectId1",
                principalTable: "Subjects",
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

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Sections_SectionId1",
                table: "Students",
                column: "SectionId1",
                principalTable: "Sections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Courses_CourseId1",
                table: "Subjects",
                column: "CourseId1",
                principalTable: "Courses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Sections_SectionId1",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Sections_SectionId1",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Subjects_SubjectId1",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Subjects_SubjectId1",
                table: "Sections");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionTeachers_Sections_SectionId1",
                table: "SectionTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionTeachers_Teachers_TeacherId1",
                table: "SectionTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Sections_SectionId1",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Courses_CourseId1",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_CourseId1",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Students_SectionId1",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_SectionTeachers_SectionId1",
                table: "SectionTeachers");

            migrationBuilder.DropIndex(
                name: "IX_SectionTeachers_TeacherId1",
                table: "SectionTeachers");

            migrationBuilder.DropIndex(
                name: "IX_Sections_SubjectId1",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SectionId1",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SubjectId1",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SectionId1",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "CourseId1",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "SectionTeachers");

            migrationBuilder.DropColumn(
                name: "TeacherId1",
                table: "SectionTeachers");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "SubjectId1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "SectionId1",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AttendanceReports");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AttendanceReports");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AcademicYears");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AcademicYears");
        }
    }
}
