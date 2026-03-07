import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  AcademicProgramResponse,
  CreateAcademicProgramRequest,
  UpdateAcademicProgramRequest,
} from "@/types/api";

interface AcademicProgramQueryParams {
  page?: number;
  pageSize?: number;
  departmentId?: string;
}

export const academicProgramService = {
  getAll: async (params?: AcademicProgramQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<AcademicProgramResponse>>>(
      "/AcademicPrograms",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<AcademicProgramResponse>>(
      `/AcademicPrograms/${id}`
    );
    return res.data;
  },

  create: async (data: CreateAcademicProgramRequest) => {
    const res = await apiClient.post<ApiResponse<AcademicProgramResponse>>(
      "/AcademicPrograms",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateAcademicProgramRequest) => {
    const res = await apiClient.put<ApiResponse<AcademicProgramResponse>>(
      `/AcademicPrograms/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/AcademicPrograms/${id}`);
    return res.data;
  },
};
