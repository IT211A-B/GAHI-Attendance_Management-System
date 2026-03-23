import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { AuthUser, UserRole } from "@/types";
import type { LoginResponse } from "@/types/api";

interface AuthState {
  user: AuthUser | null;
  setUser: (loginResponse: LoginResponse) => void;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: UserRole[]) => boolean;
  isTokenExpired: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,

      setUser: (res: LoginResponse) => {
        set({
          user: {
            username: res.username,
            email: res.email,
            fullName: res.fullName,
            roles: res.roles as UserRole[],
            token: res.token,
            refreshToken: res.refreshToken,
            expiresAt: res.expiresAt,
          },
        });
      },

      logout: () => {
        set({ user: null });
      },

      hasRole: (role: UserRole) => {
        const roleNeedle = role.toLowerCase();
        return get().user?.roles.some((r) => r.toLowerCase() === roleNeedle) ?? false;
      },

      hasAnyRole: (roles: UserRole[]) => {
        const userRoles = get().user?.roles;
        if (!userRoles) return false;
        const normalized = userRoles.map((r) => r.toLowerCase());
        return roles.some((r) => normalized.includes(r.toLowerCase()));
      },

      isTokenExpired: () => {
        const expiresAt = get().user?.expiresAt;
        if (!expiresAt) return true;
        return new Date(expiresAt) <= new Date();
      },
    }),
    {
      name: "auth-storage",
    }
  )
);
