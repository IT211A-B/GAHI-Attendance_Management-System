import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  SectionResponse,
  CreateSectionRequest,
  UpdateSectionRequest,
} from "@/types/api";

interface SectionQueryParams {
  page?: number;
  pageSize?: number;
  programId?: string;
  periodId?: string;
}

export const sectionService = {
  getAll: async (params?: SectionQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<SectionResponse>>>(
      "/Sections",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<SectionResponse>>(
      `/Sections/${id}`
    );
    return res.data;
  },

  create: async (data: CreateSectionRequest) => {
    const res = await apiClient.post<ApiResponse<SectionResponse>>(
      "/Sections",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateSectionRequest) => {
    const res = await apiClient.put<ApiResponse<SectionResponse>>(
      `/Sections/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/Sections/${id}`);
    return res.data;
  },
};
