using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceQrSessionAndCheckins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceQrSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    ScheduleId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    OwnerTeacherId = table.Column<int>(type: "integer", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TokenNonce = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceQrSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceQrSessions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceQrSessions_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceQrSessions_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceQrSessions_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceQrCheckins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttendanceQrSessionId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    CheckedInAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AttendanceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceQrCheckins", x => x.Id);
                    table.CheckConstraint("CK_AttendanceQrCheckin_Status", "\"Status\" IN ('present', 'late')");
                    table.ForeignKey(
                        name: "FK_AttendanceQrCheckins_AttendanceQrSessions_AttendanceQrSessi~",
                        column: x => x.AttendanceQrSessionId,
                        principalTable: "AttendanceQrSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceQrCheckins_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "Attendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceQrCheckins_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrCheckins_AttendanceId",
                table: "AttendanceQrCheckins",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrCheckins_AttendanceQrSessionId_CheckedInAtUtc",
                table: "AttendanceQrCheckins",
                columns: new[] { "AttendanceQrSessionId", "CheckedInAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrCheckins_AttendanceQrSessionId_StudentId",
                table: "AttendanceQrCheckins",
                columns: new[] { "AttendanceQrSessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrCheckins_StudentId",
                table: "AttendanceQrCheckins",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrSessions_CreatedByUserId",
                table: "AttendanceQrSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrSessions_OwnerTeacherId_IsActive_ExpiresAtUtc",
                table: "AttendanceQrSessions",
                columns: new[] { "OwnerTeacherId", "IsActive", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrSessions_ScheduleId",
                table: "AttendanceQrSessions",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrSessions_SectionId",
                table: "AttendanceQrSessions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrSessions_SessionId",
                table: "AttendanceQrSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceQrSessions_SubjectId",
                table: "AttendanceQrSessions",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceQrCheckins");

            migrationBuilder.DropTable(
                name: "AttendanceQrSessions");
        }
    }
}
