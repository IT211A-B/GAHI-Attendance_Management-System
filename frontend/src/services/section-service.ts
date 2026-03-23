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

interface BackendSection {
  id: string;
  name: string;
  createdAt: string;
}

interface BackendSectionList {
  items: BackendSection[];
  total: number;
}

const toSectionResponse = (s: BackendSection): SectionResponse => ({
  id: s.id,
  name: s.name,
  yearLevel: 1,
  academicProgramId: "",
  academicProgramName: "N/A",
  academicPeriodId: "",
  academicPeriodName: "N/A",
  studentCount: 0,
  createdAt: s.createdAt,
});

export const sectionService = {
  getAll: async (params?: SectionQueryParams) => {
    const res = await apiClient.get<ApiResponse<BackendSectionList>>(
      "/sections",
      { params }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to load sections",
      } as ApiResponse<PagedResult<SectionResponse>>;
    }

    const page = params?.page ?? 1;
    const pageSize = params?.pageSize ?? Math.max(res.data.data.total, 1);
    const totalCount = res.data.data.total;
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    return {
      success: true,
      message: "Sections loaded",
      data: {
        items: res.data.data.items.map(toSectionResponse),
        page,
        pageSize,
        totalCount,
        totalPages,
        hasPrevious: page > 1,
        hasNext: page < totalPages,
      },
    } as ApiResponse<PagedResult<SectionResponse>>;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<BackendSection>>(
      `/sections/${id}`
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to load section",
      } as ApiResponse<SectionResponse>;
    }
    return {
      success: true,
      message: "Section loaded",
      data: toSectionResponse(res.data.data),
    } as ApiResponse<SectionResponse>;
  },

  create: async (data: Pick<CreateSectionRequest, "name">) => {
    const res = await apiClient.post<ApiResponse<BackendSection>>(
      "/sections",
      { name: data.name }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to create section",
      } as ApiResponse<SectionResponse>;
    }
    return {
      success: true,
      message: "Section created",
      data: toSectionResponse(res.data.data),
    } as ApiResponse<SectionResponse>;
  },

  update: async (id: string, data: Pick<UpdateSectionRequest, "name">) => {
    const res = await apiClient.put<ApiResponse<BackendSection>>(
      `/sections/${id}`,
      { name: data.name }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to update section",
      } as ApiResponse<SectionResponse>;
    }
    return {
      success: true,
      message: "Section updated",
      data: toSectionResponse(res.data.data),
    } as ApiResponse<SectionResponse>;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/sections/${id}`);
    return {
      success: res.data.success,
      message: res.data.error?.message || "Section deleted",
    } as ApiResponse;
  },
};
