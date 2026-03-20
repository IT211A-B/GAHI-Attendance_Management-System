"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";
import { useSidebarStore } from "@/stores/sidebar-store";
import type { UserRole } from "@/types";
import { APP_NAME } from "@/lib/constants";
import {
  LayoutDashboard,
  Users,
  Building2,
  BookOpen,
  Shield,
  ChevronLeft,
  ChevronRight,
  X,
} from "lucide-react";
import { useEffect } from "react";

interface NavItem {
  label: string;
  href: string;
  icon: React.ElementType;
  roles?: UserRole[];
}

const navigation: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  {
    label: "Classrooms",
    href: "/departments",
    icon: Building2,
  },
  {
    label: "Sections",
    href: "/sections",
    icon: BookOpen,
  },
  {
    label: "Users",
    href: "/users",
    icon: Users,
    roles: ["Admin"],
  },
];

export default function Sidebar() {
  const pathname = usePathname();
  const { hasAnyRole } = useAuthStore();
  const { isOpen, setOpen, isCollapsed, toggleCollapsed } = useSidebarStore();

  // Close mobile sidebar on route change
  useEffect(() => {
    setOpen(false);
  }, [pathname, setOpen]);

  const filteredNav = navigation.filter(
    (item) => !item.roles || hasAnyRole(item.roles)
  );

  const sidebarContent = (
    <>
      {/* Logo */}
      <div className="h-16 flex items-center px-4 border-b border-gray-200">
        <Shield className="h-8 w-8 text-blue-600 flex-shrink-0" />
        {(!isCollapsed || isOpen) && (
          <span className="ml-3 font-bold text-gray-900 text-lg truncate">
            {APP_NAME}
          </span>
        )}
        {/* Mobile close button */}
        <button
          onClick={() => setOpen(false)}
          className="ml-auto p-1 rounded-lg text-gray-400 hover:bg-gray-100 hover:text-gray-600 md:hidden"
          aria-label="Close sidebar"
        >
          <X className="h-5 w-5" />
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4 px-2 space-y-1">
        {filteredNav.map((item) => {
          const isActive =
            pathname === item.href || pathname.startsWith(item.href + "/");
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors",
                isActive
                  ? "bg-blue-50 text-blue-700"
                  : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
              )}
              title={isCollapsed && !isOpen ? item.label : undefined}
            >
              <item.icon
                className={cn(
                  "h-5 w-5 flex-shrink-0",
                  isActive && "text-blue-600"
                )}
              />
              {(!isCollapsed || isOpen) && <span>{item.label}</span>}
            </Link>
          );
        })}
      </nav>

      {/* Collapse toggle — desktop only */}
      <div className="p-2 border-t border-gray-200 hidden md:block">
        <button
          onClick={toggleCollapsed}
          className="w-full flex items-center justify-center p-2 rounded-lg text-gray-400 hover:bg-gray-50 hover:text-gray-600 transition-colors"
          aria-label={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
        >
          {isCollapsed ? (
            <ChevronRight className="h-5 w-5" />
          ) : (
            <ChevronLeft className="h-5 w-5" />
          )}
        </button>
      </div>
    </>
  );

  return (
    <>
      {/* Mobile backdrop */}
      {isOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={() => setOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Mobile sidebar (overlay) */}
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-50 w-64 bg-white border-r border-gray-200 flex flex-col transition-transform duration-300 md:hidden",
          isOpen ? "translate-x-0" : "-translate-x-full"
        )}
      >
        {sidebarContent}
      </aside>

      {/* Desktop sidebar */}
      <aside
        className={cn(
          "hidden md:flex h-screen bg-white border-r border-gray-200 flex-col transition-all duration-300 sticky top-0",
          isCollapsed ? "w-16" : "w-64"
        )}
      >
        {sidebarContent}
      </aside>
    </>
  );
}
