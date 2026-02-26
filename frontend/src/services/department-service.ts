import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  DepartmentResponse,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from "@/types/api";

interface DepartmentQueryParams {
  page?: number;
  pageSize?: number;
}

export const departmentService = {
  getAll: async (params?: DepartmentQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<DepartmentResponse>>>(
      "/Departments",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<DepartmentResponse>>(
      `/Departments/${id}`
    );
    return res.data;
  },

  create: async (data: CreateDepartmentRequest) => {
    const res = await apiClient.post<ApiResponse<DepartmentResponse>>(
      "/Departments",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateDepartmentRequest) => {
    const res = await apiClient.put<ApiResponse<DepartmentResponse>>(
      `/Departments/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/Departments/${id}`);
    return res.data;
  },
};
