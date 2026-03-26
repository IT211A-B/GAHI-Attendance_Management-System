// ============================================================
// Common API wrapper types
// ============================================================

export interface ApiResponse<T = void> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
  error?: {
    code?: string;
    message?: string;
    details?: unknown;
  };
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

// ============================================================
// Enums (sent as strings by the API)
// ============================================================

export type AttendanceStatus = "OnTime" | "Late" | "Absent";
export type EnrollmentStatus =
  | "Active"
  | "Inactive"
  | "Graduated"
  | "DroppedOut"
  | "Suspended"
  | "OnLeave";
export type PersonType = "Student" | "Staff";
export type ScanType = "Entry" | "Exit";
export type StaffType = "Teaching" | "NonTeaching" | "Security" | "Administrative";
export type TerminalType = "QRScanner" | "Manual";
export type VerificationStatus = "Verified" | "Failed";

// ============================================================
// Auth
// ============================================================

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  username: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

// ============================================================
// Attendance
// ============================================================

export interface ScanRequest {
  gateTerminalId: string;
  rawScanData: string;
  remarks?: string;
}

export interface ScanResponse {
  attendanceLogId: string;
  personName: string;
  personType: PersonType;
  idNumber: string;
  scanType: ScanType;
  status: AttendanceStatus;
  verificationStatus: VerificationStatus;
  scannedAt: string;
  terminalName: string;
}

export interface AttendanceLogResponse {
  id: string;
  personType: PersonType;
  studentId?: string;
  studentName?: string;
  studentIdNumber?: string;
  staffId?: string;
  staffName?: string;
  employeeIdNumber?: string;
  gateTerminalId: string;
  gateTerminalName: string;
  scannedAt: string;
  scanType: ScanType;
  status: AttendanceStatus;
  verificationStatus: VerificationStatus;
  rawScanData?: string;
  remarks?: string;
  createdAt: string;
}

export interface AttendanceFilterRequest {
  date?: string;
  startDate?: string;
  endDate?: string;
  sectionId?: string;
  departmentId?: string;
  status?: AttendanceStatus;
  personType?: PersonType;
  page?: number;
  pageSize?: number;
}

// ============================================================
// Students
// ============================================================

export interface CreateStudentRequest {
  studentIdNumber: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  email?: string;
  contactNumber?: string;
  sectionId: string;
}

export interface UpdateStudentRequest {
  firstName?: string;
  middleName?: string;
  lastName?: string;
  email?: string;
  contactNumber?: string;
  enrollmentStatus?: EnrollmentStatus;
  sectionId?: string;
}

export interface StudentResponse {
  id: string;
  studentIdNumber: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  contactNumber?: string;
  qrCodeData?: string;
  enrollmentStatus: EnrollmentStatus;
  sectionId: string;
  sectionName: string;
  academicProgramName: string;
  departmentName: string;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Staff
// ============================================================

export interface CreateStaffRequest {
  employeeIdNumber: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  email?: string;
  contactNumber?: string;
  staffType: StaffType;
  departmentId: string;
}

export interface UpdateStaffRequest {
  firstName?: string;
  middleName?: string;
  lastName?: string;
  email?: string;
  contactNumber?: string;
  staffType?: StaffType;
  departmentId?: string;
}

export interface StaffResponse {
  id: string;
  employeeIdNumber: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  contactNumber?: string;
  qrCodeData?: string;
  staffType: StaffType;
  departmentId: string;
  departmentName: string;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Departments
// ============================================================

export interface CreateDepartmentRequest {
  name: string;
  code: string;
  description?: string;
}

export interface UpdateDepartmentRequest {
  name?: string;
  code?: string;
  description?: string;
}

export interface DepartmentResponse {
  id: string;
  name: string;
  code: string;
  description?: string;
  programCount: number;
  staffCount: number;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Sections
// ============================================================

export interface CreateSectionRequest {
  name: string;
  yearLevel: number;
  academicProgramId: string;
  academicPeriodId: string;
}

export interface UpdateSectionRequest {
  name?: string;
  yearLevel?: number;
  academicProgramId?: string;
  academicPeriodId?: string;
}

export interface SectionResponse {
  id: string;
  name: string;
  yearLevel: number;
  academicProgramId: string;
  academicProgramName: string;
  academicPeriodId: string;
  academicPeriodName: string;
  studentCount: number;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Academic Periods
// ============================================================

export interface CreateAcademicPeriodRequest {
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
}

export interface UpdateAcademicPeriodRequest {
  name?: string;
  startDate?: string;
  endDate?: string;
  isCurrent?: boolean;
}

export interface AcademicPeriodResponse {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  sectionCount: number;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Academic Programs
// ============================================================

export interface CreateAcademicProgramRequest {
  name: string;
  code: string;
  description?: string;
  departmentId: string;
}

export interface UpdateAcademicProgramRequest {
  name?: string;
  code?: string;
  description?: string;
  departmentId?: string;
}

export interface AcademicProgramResponse {
  id: string;
  name: string;
  code: string;
  description?: string;
  departmentId: string;
  departmentName: string;
  sectionCount: number;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Business Rules
// ============================================================

export interface CreateBusinessRuleRequest {
  ruleKey: string;
  ruleValue: string;
  description?: string;
  departmentId?: string;
}

export interface UpdateBusinessRuleRequest {
  ruleValue?: string;
  description?: string;
}

export interface BusinessRuleResponse {
  id: string;
  ruleKey: string;
  ruleValue: string;
  description?: string;
  departmentId?: string;
  departmentName?: string;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Gate Terminals
// ============================================================

export interface CreateGateTerminalRequest {
  name: string;
  location: string;
  terminalType: TerminalType;
}

export interface UpdateGateTerminalRequest {
  name?: string;
  location?: string;
  terminalType?: TerminalType;
  isActive?: boolean;
}

export interface GateTerminalResponse {
  id: string;
  name: string;
  location: string;
  terminalType: TerminalType;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Reports
// ============================================================

export interface DepartmentAttendanceSummary {
  departmentId: string;
  departmentName: string;
  totalPersonnel: number;
  presentCount: number;
  lateCount: number;
  absentCount: number;
  attendanceRate: number;
}

export interface DailyReportResponse {
  date: string;
  totalScans: number;
  onTimeCount: number;
  lateCount: number;
  absentCount: number;
  uniqueStudents: number;
  uniqueStaff: number;
  byDepartment: DepartmentAttendanceSummary[];
}

export interface WeeklyReportResponse {
  startDate: string;
  endDate: string;
  dailyBreakdown: DailyReportResponse[];
  totalScans: number;
  averageOnTimeRate: number;
  averageLateRate: number;
}

// ============================================================
// Users
// ============================================================

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  roleIds: string[];
}

export interface UpdateUserRequest {
  email?: string;
  firstName?: string;
  lastName?: string;
  isActive?: boolean;
}

export interface AssignRolesRequest {
  roleIds: string[];
}

export interface UserResponse {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  lastLoginAt?: string;
  roles: string[];
  createdAt: string;
  updatedAt?: string;
}

// ============================================================
// Audit Logs
// ============================================================

export interface AuditLogFilterRequest {
  action?: string;
  entityName?: string;
  userId?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}

export interface AuditLogResponse {
  id: string;
  action: string;
  entityName: string;
  entityId?: string;
  oldValues?: string;
  newValues?: string;
  performedByUserId?: string;
  performedByUsername?: string;
  performedAt: string;
}
