# DonBosco AMS Frontend QA Smoke Report

Date: 2026-04-02
Scope: Frontend-only C# MVC/Razor pages and user flows
Environment: localhost:5002

## Test Summary

- Total cases: 10
- Passed: 10
- Failed: 0
- Blocked: 0

## Test Cases

1. Login page renders
- Steps: Open /login
- Expected: Sign-in form is visible
- Actual: Form rendered with email/password fields and submit button
- Result: PASS

2. Invalid session access redirect
- Steps: Open protected route /users while logged out
- Expected: Redirect to /login with ReturnUrl
- Actual: Redirected to /login?ReturnUrl=%2Fusers
- Result: PASS

3. Admin login happy path
- Steps: Login with admin@school.edu
- Expected: Authenticated redirect to protected content
- Actual: Redirected into authenticated app and admin menu displayed
- Result: PASS

4. Sections page data render
- Steps: Open /sections after admin login
- Expected: Section table renders rows
- Actual: Rows rendered (Grade 7-A, Grade 7-B and related columns)
- Result: PASS

5. Reports filter submit
- Steps: Open /reports, select Section + Schedule, click Generate Report
- Expected: KPI metrics render
- Actual: Metrics rendered (Total Students, Present, Late, Absent)
- Result: PASS

6. Attendance filter submit
- Steps: Open /attendance, select Section + Schedule, click Load Attendance
- Expected: Summary cards and attendance table render
- Actual: Summary cards and attendance rows rendered
- Result: PASS

7. Placeholder route parity page
- Steps: Open /audit-logs
- Expected: Frontend placeholder page explains status
- Actual: Placeholder rendered with clear backend API pending message
- Result: PASS

8. Logout flow
- Steps: Trigger logout from authenticated page
- Expected: Return to /login and clear auth session
- Actual: Redirected to /login and session cleared
- Result: PASS

9. Protected route blocked after logout
- Steps: Open /users after logout
- Expected: Redirect to /login
- Actual: Redirected to /login?ReturnUrl=%2Fusers
- Result: PASS

10. Teacher role restriction
- Steps: Login as teacher1@school.edu, then open /users
- Expected: Non-admin blocked from admin page
- Actual: Redirected to /Dashboard and teacher menu shown
- Result: PASS

## Notes

- UI animations can make automation clicks intermittently flaky on some buttons; direct form submit and route checks confirm backend outcomes are correct.
- This report validates realistic user journeys and role behavior for the migrated frontend.
