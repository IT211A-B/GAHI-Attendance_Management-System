using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseEducationLevelAndEnrollmentYearRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EducationLevel",
                table: "Courses",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Courses"
                SET "EducationLevel" = CASE
                    WHEN "Code" = 'ES' OR "Name" ILIKE '%elementary%' THEN 1
                    WHEN "Code" = 'JHS' OR "Name" ILIKE '%junior high%' THEN 2
                    WHEN "Code" ILIKE 'SHS%' OR "Name" ILIKE '%senior high%' THEN 3
                    WHEN "Code" IN ('BSIT', 'BSME', 'BTVTED', 'ABREPC') OR COALESCE("Description", '') ILIKE '%college%' THEN 4
                    WHEN "Code" IN ('DMT', 'DET', 'DFMT', 'CFM', 'CMACH', 'CMS', 'CPS') OR COALESCE("Description", '') ILIKE '%tvet%' THEN 5
                    ELSE 4
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "EducationLevel",
                table: "Courses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_EducationLevel",
                table: "Courses",
                sql: "\"EducationLevel\" IN (1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_EducationLevel",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "Courses");
        }
    }
}
