using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.Data;

/// <summary>
/// Seeds the database with essential reference data on application startup.
/// Only inserts data if the target tables are empty (idempotent).
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // ── Roles ──
        if (!await context.Roles.IgnoreQueryFilters().AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { Name = "Admin", Description = "Full system access" },
                new() { Name = "Registrar", Description = "Manages student/staff records and masterlists" },
                new() { Name = "Guard", Description = "Operates gate terminals and views scan results" },
                new() { Name = "DepartmentHead", Description = "Views attendance for their department" },
                new() { Name = "Staff", Description = "Basic staff access" }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }

        // ── Default Admin User ──
        if (!await context.Users.IgnoreQueryFilters().AnyAsync())
        {
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@dbtc.edu.ph",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@DBTC2026"),
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            // Assign Admin role
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            context.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });

            await context.SaveChangesAsync();
        }

        // ── Departments ──
        if (!await context.Departments.IgnoreQueryFilters().AnyAsync())
        {
            var departments = new List<Department>
            {
                new() { Name = "College Department", Code = "COLLEGE", Description = "Bachelor's degree programs" },
                new() { Name = "TVET Department", Code = "TVET", Description = "Technical-Vocational Education and Training" },
                new() { Name = "Senior High School", Code = "SHS", Description = "Senior High School (Grades 11-12)" },
                new() { Name = "Administration", Code = "ADMIN", Description = "Non-teaching administrative staff" }
            };

            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();
        }

        // ── Academic Period (current semester) ──
        if (!await context.AcademicPeriods.IgnoreQueryFilters().AnyAsync())
        {
            var currentPeriod = new AcademicPeriod
            {
                Name = "SY 2025-2026 2nd Semester",
                StartDate = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
                IsCurrent = true
            };

            context.AcademicPeriods.Add(currentPeriod);
            await context.SaveChangesAsync();
        }

        // ── Business Rules (institutional defaults) ──
        if (!await context.BusinessRules.IgnoreQueryFilters().AnyAsync())
        {
            // Get department IDs for department-specific rules
            var tvetDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "TVET");
            var collegeDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "COLLEGE");
            var shsDept = await context.Departments.FirstOrDefaultAsync(d => d.Code == "SHS");

            var rules = new List<BusinessRule>
            {
                // Institution-wide defaults
                new()
                {
                    RuleKey = "MORNING_ENTRY_START",
                    RuleValue = "06:00",
                    Description = "Earliest allowed morning entry time (HH:mm)"
                },
                new()
                {
                    RuleKey = "MORNING_CUTOFF_TIME",
                    RuleValue = "08:00",
                    Description = "Default morning cutoff; arrivals after this are marked Late"
                },
                new()
                {
                    RuleKey = "AFTERNOON_CUTOFF_TIME",
                    RuleValue = "13:00",
                    Description = "Default afternoon cutoff; arrivals after this are marked Late"
                },
                // TVET-specific
                new()
                {
                    RuleKey = "GRACE_PERIOD_MINUTES",
                    RuleValue = "15",
                    Description = "Grace period (minutes) after cutoff before marking Late",
                    DepartmentId = tvetDept?.Id
                },
                // College-specific
                new()
                {
                    RuleKey = "GRACE_PERIOD_MINUTES",
                    RuleValue = "10",
                    Description = "Grace period (minutes) after cutoff before marking Late",
                    DepartmentId = collegeDept?.Id
                },
                // SHS-specific
                new()
                {
                    RuleKey = "GRACE_PERIOD_MINUTES",
                    RuleValue = "5",
                    Description = "Grace period (minutes) after cutoff before marking Late",
                    DepartmentId = shsDept?.Id
                }
            };

            context.BusinessRules.AddRange(rules);
            await context.SaveChangesAsync();
        }

        // ── Default Gate Terminal ──
        if (!await context.GateTerminals.IgnoreQueryFilters().AnyAsync())
        {
            var terminal = new GateTerminal
            {
                Name = "Main Gate - Terminal 1",
                Location = "DBTC Cebu Main Gate",
                TerminalType = TerminalType.QRScanner,
                IsActive = true
            };

            context.GateTerminals.Add(terminal);
            await context.SaveChangesAsync();
        }
    }
}
