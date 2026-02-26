import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  AuditLogResponse,
  AuditLogFilterRequest,
} from "@/types/api";

export const auditLogService = {
  getAll: async (params?: AuditLogFilterRequest) => {
    const res = await apiClient.get<ApiResponse<PagedResult<AuditLogResponse>>>(
      "/AuditLogs",
      { params }
    );
    return res.data;
  },
};
