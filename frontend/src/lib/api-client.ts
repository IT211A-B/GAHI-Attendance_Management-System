import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { useAuthStore } from "@/stores/auth-store";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5001";

const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor — attach JWT token (skip if expired)
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    if (typeof window !== "undefined") {
      const store = useAuthStore.getState();
      if (store.user) {
        if (store.isTokenExpired()) {
          store.logout();
          window.location.href = "/login";
          return Promise.reject(new Error("Token expired"));
        }
        config.headers.Authorization = `Bearer ${store.user.token}`;
      }
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor — handle 401 and token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = useAuthStore.getState().user?.refreshToken;
        if (!refreshToken) throw new Error("No refresh token");

        const res = await axios.post(`${API_BASE_URL}/api/Auth/refresh`, {
          refreshToken,
        });

        if (res.data?.success && res.data?.data) {
          useAuthStore.getState().setUser(res.data.data);
          originalRequest.headers.Authorization = `Bearer ${res.data.data.token}`;
          return apiClient(originalRequest);
        }
      } catch {
        useAuthStore.getState().logout();
        if (typeof window !== "undefined") {
          window.location.href = "/login";
        }
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
