import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  ChangePasswordRequest,
} from "@/types/api";

interface BackendAuthResponse {
  token: string;
  tokenType: string;
  expiresAt: string;
  user: {
    id: string;
    name: string;
    email: string;
    role: string;
    isActive: boolean;
    createdAt: string;
  };
}

export const authService = {
  login: async (data: LoginRequest) => {
    const res = await apiClient.post<ApiResponse<BackendAuthResponse>>(
      "/auth/login",
      {
        email: data.username,
        password: data.password,
      }
    );

    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Login failed",
        errors: res.data.error?.code ? [res.data.error.code] : undefined,
      } as ApiResponse<LoginResponse>;
    }

    const mapped: LoginResponse = {
      token: res.data.data.token,
      refreshToken: "",
      expiresAt: res.data.data.expiresAt,
      username: res.data.data.user.email,
      email: res.data.data.user.email,
      fullName: res.data.data.user.name,
      roles: [res.data.data.user.role],
    };

    return {
      success: true,
      message: "Login successful",
      data: mapped,
    } as ApiResponse<LoginResponse>;
  },

  changePassword: async (_data: ChangePasswordRequest) => {
    return {
      success: false,
      message: "Change password is not available in the current backend",
      errors: ["NOT_SUPPORTED"],
    } as ApiResponse;
  },
};
