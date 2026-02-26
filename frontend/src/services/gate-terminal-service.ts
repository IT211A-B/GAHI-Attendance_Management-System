import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  GateTerminalResponse,
  CreateGateTerminalRequest,
  UpdateGateTerminalRequest,
} from "@/types/api";

interface GateTerminalQueryParams {
  page?: number;
  pageSize?: number;
}

export const gateTerminalService = {
  getAll: async (params?: GateTerminalQueryParams) => {
    const res = await apiClient.get<ApiResponse<PagedResult<GateTerminalResponse>>>(
      "/GateTerminals",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<GateTerminalResponse>>(
      `/GateTerminals/${id}`
    );
    return res.data;
  },

  create: async (data: CreateGateTerminalRequest) => {
    const res = await apiClient.post<ApiResponse<GateTerminalResponse>>(
      "/GateTerminals",
      data
    );
    return res.data;
  },

  update: async (id: string, data: UpdateGateTerminalRequest) => {
    const res = await apiClient.put<ApiResponse<GateTerminalResponse>>(
      `/GateTerminals/${id}`,
      data
    );
    return res.data;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/GateTerminals/${id}`);
    return res.data;
  },
};
