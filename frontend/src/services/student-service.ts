import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  StudentResponse,
  CreateStudentRequest,
  UpdateStudentRequest,
} from "@/types/api";

interface StudentQueryParams {
  page?: number;
  pageSize?: number;
  sectionId?: string;
  search?: string;
}

export const studentService = {
  getAll: async (params?: StudentQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<StudentResponse>>>(
      "/Students",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<StudentResponse>>(
      `/Students/${id}`
    );
    return res.data;
  },

  create: async (data: CreateStudentRequest) => {
    const res = await apiClient.post<ApiResponse<StudentResponse>>(
      "/Students",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateStudentRequest) => {
    const res = await apiClient.put<ApiResponse<StudentResponse>>(
      `/Students/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/Students/${id}`);
    return res.data;
  },

  regenerateQr: async (id: string) => {
    const res = await apiClient.post<ApiResponse<StudentResponse>>(
      `/Students/${id}/regenerate-qr`
    );
    return res.data;
  },
};
