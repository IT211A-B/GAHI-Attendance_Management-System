import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  AcademicPeriodResponse,
  CreateAcademicPeriodRequest,
  UpdateAcademicPeriodRequest,
} from "@/types/api";

interface AcademicPeriodQueryParams {
  page?: number;
  pageSize?: number;
}

export const academicPeriodService = {
  getAll: async (params?: AcademicPeriodQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<AcademicPeriodResponse>>>(
      "/AcademicPeriods",
      { params }
    );
    return res.data;
  },

  getCurrent: async () => {
    const res = await apiClient.get<ApiResponse<AcademicPeriodResponse>>(
      "/AcademicPeriods/current"
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<AcademicPeriodResponse>>(
      `/AcademicPeriods/${id}`
    );
    return res.data;
  },

  create: async (data: CreateAcademicPeriodRequest) => {
    const res = await apiClient.post<ApiResponse<AcademicPeriodResponse>>(
      "/AcademicPeriods",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateAcademicPeriodRequest) => {
    const res = await apiClient.put<ApiResponse<AcademicPeriodResponse>>(
      `/AcademicPeriods/${id}`,
      data
    );
    return res.data;
  },

  setCurrent: async (id: string) => {
    const res = await apiClient.put<ApiResponse<AcademicPeriodResponse>>(
      `/AcademicPeriods/${id}/set-current`
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/AcademicPeriods/${id}`);
    return res.data;
  },
};
