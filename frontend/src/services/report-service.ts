import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  DailyReportResponse,
  WeeklyReportResponse,
  DepartmentAttendanceSummary,
} from "@/types/api";

export const reportService = {
  getDaily: async (date?: string) => {
    const res = await apiClient.get<ApiResponse<DailyReportResponse>>(
      "/Reports/daily",
      { params: { date } }
    );
    return res.data;
  },

  getWeekly: async (startDate?: string) => {
    const res = await apiClient.get<ApiResponse<WeeklyReportResponse>>(
      "/Reports/weekly",
      { params: { startDate } }
    );
    return res.data;
  },

  getDepartmentSummary: async (date?: string) => {
    const res = await apiClient.get<ApiResponse<DepartmentAttendanceSummary[]>>(
      "/Reports/department-summary",
      { params: { date } }
    );
    return res.data;
  },
};
