"use client";

import { Toaster } from "react-hot-toast";
import { AppShell, AuthGuard } from "@/components/layout";

export default function Providers({ children }: { children: React.ReactNode }) {
  return (
    <>
      <Toaster
        position="top-right"
        gutter={8}
        containerStyle={{ top: 20 }}
        toastOptions={{
          duration: 4000,
          style: {
            padding: "12px 16px",
            borderRadius: "12px",
            fontSize: "14px",
            fontWeight: 500,
            maxWidth: "420px",
            boxShadow: "0 4px 12px rgba(0, 0, 0, 0.1), 0 1px 3px rgba(0, 0, 0, 0.06)",
            background: "#fff",
            color: "#1f2937",
            border: "1px solid #e5e7eb",
          },
          success: {
            style: {
              background: "#f0fdf4",
              border: "1px solid #bbf7d0",
              color: "#166534",
            },
          },
          error: {
            style: {
              background: "#fef2f2",
              border: "1px solid #fecaca",
              color: "#991b1b",
            },
          },
        }}
      />
      <AuthGuard>
        <AppShell>{children}</AppShell>
      </AuthGuard>
    </>
  );
}
