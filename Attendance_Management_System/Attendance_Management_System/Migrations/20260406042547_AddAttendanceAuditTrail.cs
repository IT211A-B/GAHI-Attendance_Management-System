using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttendanceId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    BeforeTimeIn = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    BeforeRemarks = table.Column<string>(type: "text", nullable: true),
                    BeforeStatus = table.Column<string>(type: "text", nullable: true),
                    AfterTimeIn = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    AfterRemarks = table.Column<string>(type: "text", nullable: true),
                    AfterStatus = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    ActionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAudits", x => x.Id);
                    table.CheckConstraint("CK_AttendanceAudit_Action", "\"Action\" IN ('created', 'updated')");
                    table.ForeignKey(
                        name: "FK_AttendanceAudits_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceAudits_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "Attendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAudits_ActorUserId",
                table: "AttendanceAudits",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAudits_AttendanceId_ActionAt",
                table: "AttendanceAudits",
                columns: new[] { "AttendanceId", "ActionAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceAudits");
        }
    }
}
