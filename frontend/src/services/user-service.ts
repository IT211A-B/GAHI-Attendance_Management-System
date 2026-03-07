import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  UserResponse,
  CreateUserRequest,
  UpdateUserRequest,
  AssignRolesRequest,
} from "@/types/api";

interface UserQueryParams {
  page?: number;
  pageSize?: number;
}

export const userService = {
  getAll: async (params?: UserQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<UserResponse>>>(
      "/Users",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<UserResponse>>(
      `/Users/${id}`
    );
    return res.data;
  },

  create: async (data: CreateUserRequest) => {
    const res = await apiClient.post<ApiResponse<UserResponse>>(
      "/Users",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateUserRequest) => {
    const res = await apiClient.put<ApiResponse<UserResponse>>(
      `/Users/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/Users/${id}`);
    return res.data;
  },

  assignRoles: async (id: string, data: AssignRolesRequest) => {
    const res = await apiClient.put<ApiResponse<UserResponse>>(
      `/Users/${id}/roles`,
      data
    );
    return res.data;
  },
};
