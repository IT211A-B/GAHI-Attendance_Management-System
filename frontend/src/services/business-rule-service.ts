import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  BusinessRuleResponse,
  CreateBusinessRuleRequest,
  UpdateBusinessRuleRequest,
} from "@/types/api";

interface BusinessRuleQueryParams {
  page?: number;
  pageSize?: number;
  departmentId?: string;
}

export const businessRuleService = {
  getAll: async (params?: BusinessRuleQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<BusinessRuleResponse>>>(
      "/BusinessRules",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<BusinessRuleResponse>>(
      `/BusinessRules/${id}`
    );
    return res.data;
  },

  create: async (data: CreateBusinessRuleRequest) => {
    const res = await apiClient.post<ApiResponse<BusinessRuleResponse>>(
      "/BusinessRules",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateBusinessRuleRequest) => {
    const res = await apiClient.put<ApiResponse<BusinessRuleResponse>>(
      `/BusinessRules/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/BusinessRules/${id}`);
    return res.data;
  },
};
