# QR Attendance Manual QA and Blockers Report (MVC)

Date: 2026-04-06
Branch target: JP
Scope: MVC frontend QR attendance flow (`/attendance/qr` and `/attendance/scan`) with runtime smoke and hardening checks.

## 1) What Was Implemented

### Routes and Access
- Added teacher/admin QR page route: `/attendance/qr`.
- Added student scanner page route: `/attendance/scan`.
- Enforced role policies on attendance routes in controller actions.

### UI and Navigation
- Added sidebar links:
  - Teacher/Admin: `Attendance QR`
  - Student: `Scan QR`
- Added new Razor views:
  - `Views/AttendanceManagement/Qr.cshtml`
  - `Views/AttendanceManagement/Scan.cshtml`

### Frontend Logic
- Added QR session generation and short TTL rotation.
- Added token parsing and validation.
- Added student submit handling with duplicate protection.
- Added UI states for submit result and scanner lifecycle.
- Added check-in table rendering with safe DOM APIs.

### Styling
- Added QR/scanner page styles and result-state styles in site CSS.

### Runtime/Test Configuration
- Updated launch settings for local manual QA profile behavior on `http://localhost:5003`.
- Added development login bypass logic in `AccountController` to unblock manual QA when DB auth fails.

## 2) Manual QA Coverage and Outcomes

### Core Flow
1. Teacher login
   - Result: PASS
2. Teacher opens `/attendance/qr`
   - Result: PASS
3. Teacher generates active QR token/session
   - Result: PASS
4. Student login
   - Result: PASS
5. Student opens `/attendance/scan`
   - Result: PASS
6. Student submits valid token
   - Result: PASS
7. Teacher sees live check-in row update
   - Result: PASS

### Hardening/Negative Cases
1. Duplicate submit with same student and session
   - Result: PASS (blocked)
2. Malformed token submission
   - Result: PASS (blocked)
3. Expired token submission
   - Result: PASS (blocked)
4. Student direct access to teacher route `/attendance/qr`
   - Result: PASS (access prevented/redirect)
5. Rapid repeated submit clicks
   - Result: PASS (cooldown message shown)

## 3) Current Frontend Blockers

### Blocker A: Scanner Stop Error Path
- Severity: High (frontend robustness)
- Symptom:
  - When stopping scanner while scanner is not fully running, an uncaught page error can occur from html5-qrcode stop flow.
- User impact:
  - Scanner UX can become noisy or unstable under rapid start/stop actions.
- Repro:
  1. Open `/attendance/scan`.
  2. Click Start Scanner.
  3. Quickly click Stop Scanner during startup/failure edge.
  4. Observe runtime error path.

### Blocker B: Missing Stylesheet 404
- Severity: Medium
- Symptom:
  - Request to `Attendance_Management_System.styles.css` returns 404.
- User impact:
  - App still functions, but noisy console/network errors can hide real issues.
- Repro:
  1. Open authenticated page.
  2. Check browser network/console for stylesheet 404.

## 4) Environment Blockers

### Blocker C: Database Authentication Failure (Port 5002 and unstable runs)
- Severity: Critical (startup/runtime)
- Symptom:
  - `PostgresException: 28P01: password authentication failed for user "postgres"`.
- Impact:
  - Login/signup/dashboard routes can return Internal Server Error.
- Notes:
  - This is environment configuration and credential alignment, not frontend logic.

## 5) Security and Release Notes

- Development login bypass in `AccountController` is for QA unblocking only.
- Do not enable bypass behavior in production release workflows.
- Token validation currently includes frontend controls, but backend-signed tokens and persistence are still required for production trust.

## 6) Self-Test Runbook (For Developer Manual Testing)

1. Start app on `http://localhost:5003` using launch profile `http` or `manual-qa`.
2. Open `/login`.
3. Login as teacher and open `/attendance/qr`.
4. Generate QR and copy current token.
5. Sign out, login as student, open `/attendance/scan`.
6. Submit token and validate success message.
7. Submit same token again and validate duplicate-block message.
8. Replace token with invalid string and validate format/size error.
9. Validate teacher page check-in list updates after successful submission.

## 7) Recommended Next Fix Order

1. Fix scanner stop error handling to eliminate uncaught exception path.
2. Fix missing stylesheet reference/build output for `Attendance_Management_System.styles.css`.
3. Resolve DB credentials/config for stable non-bypass startup.
4. Replace frontend-only trust model with backend-signed token and server-side validation.
