"use client";

import { useCallback, useState } from "react";
import { authService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import { notify, extractErrorMessage } from "@/lib/toast";

interface UseLoginControllerOptions {
  onSuccess?: () => void;
}

export function useLoginController(options: UseLoginControllerOptions = {}) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const setUser = useAuthStore((s) => s.setUser);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError("");
      setIsLoading(true);

      try {
        const res = await authService.login({ username: email, password });
        if (res.success && res.data) {
          setUser(res.data);
          notify.success("Welcome back!");
          options.onSuccess?.();
        } else {
          setError(res.message || "Login failed");
        }
      } catch (err: unknown) {
        setError(extractErrorMessage(err));
      } finally {
        setIsLoading(false);
      }
    },
    [email, password, setUser, options]
  );

  return {
    email,
    setEmail,
    password,
    setPassword,
    isLoading,
    error,
    handleSubmit,
  };
}
