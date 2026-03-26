using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEnrollmentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAt",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedBy",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Enrollments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ProcessedBy",
                table: "Enrollments",
                column: "ProcessedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_AspNetUsers_ProcessedBy",
                table: "Enrollments",
                column: "ProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_AspNetUsers_ProcessedBy",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ProcessedBy",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ProcessedBy",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Enrollments");
        }
    }
}
