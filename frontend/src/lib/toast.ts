import toast from "react-hot-toast";
import axios from "axios";

/**
 * Enhanced toast notifications with consistent styling and UX.
 * Wraps react-hot-toast with application-specific patterns.
 */
export const notify = {
  /** Success action completed (create, update, delete) */
  success(message: string) {
    toast.success(message, {
      duration: 3000,
      iconTheme: { primary: "#16a34a", secondary: "#fff" },
    });
  },

  /** Non-critical warning */
  warning(message: string) {
    toast(message, {
      duration: 4000,
      icon: "⚠️",
      style: { borderLeft: "4px solid #f59e0b" },
    });
  },

  /** Error — extracts message from Axios errors automatically */
  error(messageOrError: string | unknown) {
    const message = extractErrorMessage(messageOrError);
    toast.error(message, {
      duration: 5000,
      iconTheme: { primary: "#dc2626", secondary: "#fff" },
    });
  },

  /** Informational (non-error) */
  info(message: string) {
    toast(message, {
      duration: 3000,
      icon: "ℹ️",
    });
  },

  /** Promise-based toast for async operations */
  promise<T>(
    promise: Promise<T>,
    messages: { loading: string; success: string; error?: string }
  ) {
    return toast.promise(promise, {
      loading: messages.loading,
      success: messages.success,
      error: messages.error ?? "Something went wrong",
    });
  },
};

/**
 * Extract a user-friendly error message from various error shapes.
 */
export function extractErrorMessage(err: unknown): string {
  if (typeof err === "string") return err;

  if (axios.isAxiosError(err)) {
    const data = err.response?.data;
    // ApiResponse shape: { message, errors }
    if (data?.message) return data.message;
    if (data?.errors && Array.isArray(data.errors) && data.errors.length > 0) {
      return data.errors[0];
    }
    // Fallback HTTP status messages
    if (err.response?.status === 401) return "Session expired. Please sign in again.";
    if (err.response?.status === 403) return "You don't have permission for this action.";
    if (err.response?.status === 404) return "The requested resource was not found.";
    if (err.response?.status === 409) return "A conflict occurred. The record may have been modified.";
    if (err.response?.status === 422) return "Validation failed. Please check your input.";
    if (err.response?.status && err.response.status >= 500) return "Server error. Please try again later.";
    if (err.code === "ERR_NETWORK") return "Network error. Please check your connection.";
  }

  if (err instanceof Error) return err.message;

  return "An unexpected error occurred.";
}
