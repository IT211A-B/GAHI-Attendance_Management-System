"use client";

import Sidebar from "./sidebar";
import Header from "./header";
import { usePathname } from "next/navigation";
import { PUBLIC_ROUTES } from "@/lib/constants";

export default function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const showLayout = !PUBLIC_ROUTES.includes(pathname);

  if (!showLayout) {
    return <>{children}</>;
  }

  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <Header />
        <main className="flex-1 p-4 md:p-6">{children}</main>
      </div>
    </div>
  );
}
