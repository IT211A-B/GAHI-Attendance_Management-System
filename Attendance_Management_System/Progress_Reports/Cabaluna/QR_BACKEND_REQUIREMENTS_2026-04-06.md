# Backend Requirements for MVC QR Attendance Integration

Date: 2026-04-06
Owner: Frontend Team handoff to Backend Team

## Objective
Replace the current frontend-only QR simulation with secure backend-validated attendance recording per class period.

## Required Endpoints

### 1) Create QR Session (Teacher/Admin)
- Method: POST
- Route suggestion: `/api/attendance-qr/sessions`
- Auth: Teacher or Admin
- Request body:
  - sectionId (int)
  - scheduleId (int)
  - subjectId (int, optional if derivable from schedule)
  - periodLabel (string, optional if derivable)
- Response:
  - sessionId (string)
  - token (string, signed)
  - expiresAtUtc (ISO datetime)
  - refreshAfterSeconds (int)

### 2) Refresh/Rotate QR Session Token
- Method: POST
- Route suggestion: `/api/attendance-qr/sessions/{sessionId}/refresh`
- Auth: Teacher or Admin owning section/schedule
- Response:
  - token (string, signed)
  - expiresAtUtc (ISO datetime)

### 3) Student Check-in Submission
- Method: POST
- Route suggestion: `/api/attendance-qr/checkins`
- Auth: Student (preferred) or controlled guest flow if policy allows
- Request body:
  - token (string)
  - studentId (int or studentNumber string)
  - studentName (string, optional if server resolves from user)
  - scannedAtClientUtc (ISO datetime, optional)
- Response:
  - success (bool)
  - status (present|late|rejected)
  - message (string)
  - recordedAtUtc (ISO datetime)

### 4) Teacher Live Check-in Feed
- Method: GET
- Route suggestion: `/api/attendance-qr/sessions/{sessionId}/checkins`
- Auth: Teacher or Admin owning section/schedule
- Response:
  - session metadata
  - student check-ins list (student id, name, timestamp, computed status)

## Required Validation and Security Rules

### Token Security
- Must be signed server-side (HMAC or asymmetric key).
- Must include expiry and be rejected after expiry.
- Must include session identity and schedule/section scope.
- Must not be trusted if payload is tampered.

### Attendance Integrity
- Enforce one check-in per student per active session.
- Enforce student enrollment in target section/schedule.
- Enforce schedule-time window policy (allow small grace period if required).
- Reject stale session tokens after session close or rotation policy.

### Access Control
- Teacher can create/refresh sessions only for owned or allowed sections.
- Student can submit only for own identity (server-resolved user mapping preferred).
- Admin can view and audit any session.

### Auditability
- Persist who created session, who checked in, and timestamps.
- Persist rejection reasons for suspicious attempts.
- Expose audit trail fields for teacher/admin review.

## API Error Contract (Recommended)
Use a consistent envelope:
- success (bool)
- code (machine-readable string)
- message (human-readable)
- errors (optional field-level object)

Suggested codes:
- TOKEN_INVALID
- TOKEN_EXPIRED
- SESSION_INACTIVE
- STUDENT_NOT_ENROLLED
- ALREADY_CHECKED_IN
- OUTSIDE_ALLOWED_WINDOW
- FORBIDDEN

## Frontend Integration Contract Notes
- Frontend currently expects:
  - short-lived token rotation
  - immediate duplicate-check feedback
  - per-session live check-in list
- Frontend can switch from localStorage simulation to API calls with minimal view changes once endpoints above exist.

## Performance and Reliability Notes
- Check-in endpoint should be idempotent against duplicates.
- Use database uniqueness constraints to enforce one check-in per student/session.
- Live check-ins can start with polling every 3-5 seconds; websockets/signalr optional later.

## Minimum Backend Delivery for Integration Sprint
1. Create session endpoint
2. Check-in endpoint with strict validation
3. Check-in list endpoint for teacher UI
4. Signed token implementation with expiration
5. Enrollment and schedule ownership checks
