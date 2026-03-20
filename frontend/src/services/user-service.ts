import apiClient from "@/lib/api-client";
import type {
  ApiResponse,
  PagedResult,
  UserResponse,
  CreateUserRequest,
  UpdateUserRequest,
  AssignRolesRequest,
} from "@/types/api";

interface UserQueryParams {
  page?: number;
  pageSize?: number;
}

interface BackendUser {
  id: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

interface BackendUserList {
  items: BackendUser[];
  total: number;
}

const toUserResponse = (u: BackendUser): UserResponse => {
  const nameParts = (u.name || "").trim().split(/\s+/);
  const firstName = nameParts[0] || "";
  const lastName = nameParts.slice(1).join(" ");
  return {
    id: u.id,
    username: u.email,
    email: u.email,
    firstName,
    lastName,
    isActive: u.isActive,
    roles: [u.role],
    createdAt: u.createdAt,
  };
};

export const userService = {
  getAll: async (params?: UserQueryParams) => {
    const res = await apiClient.get<ApiResponse<BackendUserList>>(
      "/users",
      { params }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to load users",
        errors: res.data.error?.code ? [res.data.error.code] : undefined,
      } as ApiResponse<PagedResult<UserResponse>>;
    }

    const page = params?.page ?? 1;
    const pageSize = params?.pageSize ?? Math.max(res.data.data.total, 1);
    const totalCount = res.data.data.total;
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const mapped: PagedResult<UserResponse> = {
      items: res.data.data.items.map(toUserResponse),
      page,
      pageSize,
      totalCount,
      totalPages,
      hasPrevious: page > 1,
      hasNext: page < totalPages,
    };

    return {
      success: true,
      message: "Users loaded",
      data: mapped,
    } as ApiResponse<PagedResult<UserResponse>>;
  },

  getById: async (id: string) => {
    const res = await apiClient.get<ApiResponse<BackendUser>>(
      `/users/${id}`
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to load user",
      } as ApiResponse<UserResponse>;
    }
    return {
      success: true,
      message: "User loaded",
      data: toUserResponse(res.data.data),
    } as ApiResponse<UserResponse>;
  },

  create: async (data: CreateUserRequest) => {
    const fullName = `${data.firstName} ${data.lastName}`.trim();
    const role = data.roleIds?.[0]?.toLowerCase() === "admin" ? "admin" : "teacher";
    const res = await apiClient.post<ApiResponse<BackendUser>>(
      "/users",
      {
        name: fullName,
        email: data.email,
        password: data.password,
        role,
      }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to create user",
      } as ApiResponse<UserResponse>;
    }
    return {
      success: true,
      message: "User created",
      data: toUserResponse(res.data.data),
    } as ApiResponse<UserResponse>;
  },

  update: async (id: string, data: UpdateUserRequest) => {
    const fullName = [data.firstName, data.lastName].filter(Boolean).join(" ").trim();
    const res = await apiClient.put<ApiResponse<BackendUser>>(
      `/users/${id}`,
      {
        name: fullName || undefined,
        email: data.email,
        isActive: data.isActive,
      }
    );
    if (!res.data.success || !res.data.data) {
      return {
        success: false,
        message: res.data.error?.message || "Failed to update user",
      } as ApiResponse<UserResponse>;
    }
    return {
      success: true,
      message: "User updated",
      data: toUserResponse(res.data.data),
    } as ApiResponse<UserResponse>;
  },

  delete: async (id: string) => {
    const res = await apiClient.delete<ApiResponse>(`/users/${id}`);
    return {
      success: res.data.success,
      message: res.data.error?.message || "User deleted",
    } as ApiResponse;
  },

  assignRoles: async (_id: string, _data: AssignRolesRequest) => {
    return {
      success: false,
      message: "Role assignment endpoint is not available in the current backend",
      errors: ["NOT_SUPPORTED"],
    } as ApiResponse<UserResponse>;
  },
};
