import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  DepartmentResponse,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from "@/types/api";

interface DepartmentQueryParams {
  page?: number;
  pageSize?: number;
}

interface BackendClassroom {
  id: string;
  name: string;
  roomNumber: string;
  createdAt: string;
}

interface BackendClassroomList {
  items: BackendClassroom[];
  total: number;
}

const toDepartmentResponse = (c: BackendClassroom): DepartmentResponse => ({
  id: c.id,
  name: c.name,
  code: c.roomNumber,
  description: "",
  programCount: 0,
  staffCount: 0,
  createdAt: c.createdAt,
});

export const departmentService = {
  getAll: async (params?: DepartmentQueryParams) => {
    const res = await apiClient.get<ApiResponse<BackendClassroomList>>(
      "/classrooms",
      { params }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to load classrooms",
      } as ApiResponse<PagedResult<DepartmentResponse>>;
    }

    const page = params?.page ?? 1;
    const pageSize = params?.pageSize ?? Math.max(res.data.data.total, 1);
    const totalCount = res.data.data.total;
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    return {
      success: true,
      message: "Classrooms loaded",
      data: {
        items: res.data.data.items.map(toDepartmentResponse),
        page,
        pageSize,
        totalCount,
        totalPages,
        hasPrevious: page > 1,
        hasNext: page < totalPages,
      },
    } as ApiResponse<PagedResult<DepartmentResponse>>;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<BackendClassroom>>(
      `/classrooms/${id}`
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to load classroom",
      } as ApiResponse<DepartmentResponse>;
    }
    return {
      success: true,
      message: "Classroom loaded",
      data: toDepartmentResponse(res.data.data),
    } as ApiResponse<DepartmentResponse>;
  },

  create: async (data: CreateDepartmentRequest) => {
    const res = await apiClient.post<ApiResponse<BackendClassroom>>(
      "/classrooms",
      {
        name: data.name,
        roomNumber: data.code,
      }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to create classroom",
      } as ApiResponse<DepartmentResponse>;
    }
    return {
      success: true,
      message: "Classroom created",
      data: toDepartmentResponse(res.data.data),
    } as ApiResponse<DepartmentResponse>;
  },

  update: async (id: string, data: UpdateDepartmentRequest) => {
    const res = await apiClient.put<ApiResponse<BackendClassroom>>(
      `/classrooms/${id}`,
      {
        name: data.name,
        roomNumber: data.code,
      }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to update classroom",
      } as ApiResponse<DepartmentResponse>;
    }
    return {
      success: true,
      message: "Classroom updated",
      data: toDepartmentResponse(res.data.data),
    } as ApiResponse<DepartmentResponse>;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/classrooms/${id}`);
    return {
      success: res.data.success,
      message: res.data.error?.message || "Classroom deleted",
    } as ApiResponse;
  },
};
