# DonBosco-Attendance_Management-System

An Attendance Management System for Don Bosco.

## Pure MVC Mode

The ASP.NET application is now structurally pure MVC.

- Business API controllers were removed from the backend project.
- Web features run through MVC controllers and Razor views.
- Authentication remains cookie-based with ASP.NET Identity.
- Health monitoring remains available at `/health`.
- Legacy standalone root `frontend/` (Next.js client) was removed.
- Active UI is the in-solution MVC frontend under `Attendance_Management_System/Attendance_Management_System/Frontend`.

Reference implementation details in [Attendance_Management_System/Attendance_Management_System/PURE_MVC_CUTOVER.md](Attendance_Management_System/Attendance_Management_System/PURE_MVC_CUTOVER.md).
