import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  ChangePasswordRequest,
} from "@/types/api";

export const authService = {
  login: async (data: LoginRequest) => {
    const res = await apiClient.post<ApiResponse<LoginResponse>>(
      "/Auth/login",
      data
    );
    return res.data;
  },

  changePassword: async (data: ChangePasswordRequest) => {
    const res = await apiClient.post<ApiResponse>("/Auth/change-password", data);
    return res.data;
  },
};
