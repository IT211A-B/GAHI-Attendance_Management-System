namespace Attendance_Management_System.Backend.Enums;

// Defines the roles available for users in the system
// Used for role-based access control throughout the application
public enum UserRole
{
    // Full system access - can manage all entities and users
    Admin,

    // Can manage attendance for assigned sections
    Teacher,

    // Can view own attendance and enroll in sections
    Student
}
