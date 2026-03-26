"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { PUBLIC_ROUTES } from "@/lib/constants";
import { Loader2 } from "lucide-react";

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  const isTokenExpired = useAuthStore((s) => s.isTokenExpired);
  const [isChecking, setIsChecking] = useState(true);

  useEffect(() => {
    const isPublic = PUBLIC_ROUTES.includes(pathname);

    // Token expired — force logout
    if (user && isTokenExpired()) {
      logout();
      router.replace("/login");
      return;
    }

    if (!user && !isPublic) {
      router.replace("/login");
      return;
    }

    if (user && isPublic) {
      router.replace("/dashboard");
      return;
    }

    setIsChecking(false);
  }, [user, pathname, router, logout, isTokenExpired]);

  // Show a brief loading spinner while checking auth
  if (isChecking) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    );
  }

  // On public routes, render if no user
  if (PUBLIC_ROUTES.includes(pathname)) {
    return <>{children}</>;
  }

  // On protected routes, render only if logged in
  if (!user) return null;

  return <>{children}</>;
}
