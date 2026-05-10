using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Teachers\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Subjects\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Students\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Sections\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Schedules\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Notifications\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Enrollments\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Courses\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Classrooms\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"Attendances\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceReports\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceQrSessions\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceQrCheckins\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceAudits\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE \"AcademicYears\" ADD COLUMN IF NOT EXISTS \"UpdatedAt\" timestamp with time zone;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Teachers\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Subjects\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Students\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Sections\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Schedules\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Notifications\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Enrollments\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Courses\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Classrooms\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"Attendances\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceReports\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceQrSessions\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceQrCheckins\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"AttendanceAudits\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"AcademicYears\" DROP COLUMN IF EXISTS \"UpdatedAt\";");
        }
    }
}
