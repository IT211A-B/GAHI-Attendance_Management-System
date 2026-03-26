import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  StaffResponse,
  CreateStaffRequest,
  UpdateStaffRequest,
} from "@/types/api";

interface StaffQueryParams {
  page?: number;
  pageSize?: number;
  departmentId?: string;
  search?: string;
}

export const staffService = {
  getAll: async (params?: StaffQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<StaffResponse>>>(
      "/Staff",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<StaffResponse>>(
      `/Staff/${id}`
    );
    return res.data;
  },

  create: async (data: CreateStaffRequest) => {
    const res = await apiClient.post<ApiResponse<StaffResponse>>(
      "/Staff",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateStaffRequest) => {
    const res = await apiClient.put<ApiResponse<StaffResponse>>(
      `/Staff/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/Staff/${id}`);
    return res.data;
  },

  regenerateQr: async (id: string) => {
    const res = await apiClient.post<ApiResponse<StaffResponse>>(
      `/Staff/${id}/regenerate-qr`
    );
    return res.data;
  },
};
