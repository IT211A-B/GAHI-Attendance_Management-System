export type UserRole =
  | "Admin"
  | "Registrar"
  | "DepartmentHead"
  | "Guard"
  | "admin"
  | "teacher";

export interface AuthUser {
  username: string;
  email: string;
  fullName: string;
  roles: UserRole[];
  token: string;
  refreshToken: string;
  expiresAt: string;
}
