import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  AttendanceLogResponse,
  AttendanceFilterRequest,
  ScanRequest,
  ScanResponse,
} from "@/types/api";

export const attendanceService = {
  getAll: async (params?: AttendanceFilterRequest) => {
    const res = await apiClient.get<ApiResponse<PagedResult<AttendanceLogResponse>>>(
      "/Attendance",
      { params }
    );
    return res.data;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<AttendanceLogResponse>>(
      `/Attendance/${id}`
    );
    return res.data;
  },

  scan: async (data: ScanRequest) => {
    const res = await apiClient.post<ApiResponse<ScanResponse>>(
      "/Attendance/scan",
      data
    );
    return res.data;
  },
};
