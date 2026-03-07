/**
 * AnimatedDepartmentTable
 * ───────────────────────
 * A full-width data table with GSAP stagger entrance animations.
 *
 * Animation sequence (on mount / departments change):
 *  1. Header row fades in from y:-10 (400ms)
 *  2. Data rows stagger in from x:-30, opacity:0, scale:0.98 — 80ms between rows
 *  3. Rate bars animate from 0% to attendanceRate×100% in sync with rows
 *
 * Visual features:
 *  - Colored status pills for Present (green) / Late (amber) / Absent (red)
 *  - Rate bar color: green ≥ 90%, amber ≥ 70%, red < 70%
 *  - Drop-shadow glow on rate bars matching their threshold color
 *  - Hover: row highlights, department name turns blue
 *
 * @prop departments - DepartmentAttendanceSummary[] from the daily report API
 *
 * Library: GSAP
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef } from "react";
import { gsap } from "gsap";
import type { DepartmentAttendanceSummary } from "@/types/api";

interface AnimatedDepartmentTableProps {
  departments: DepartmentAttendanceSummary[];
}

export default function AnimatedDepartmentTable({
  departments,
}: AnimatedDepartmentTableProps) {
  const tableRef = useRef<HTMLDivElement>(null);
  const headerRef = useRef<HTMLTableRowElement>(null);

  useEffect(() => {
    if (!tableRef.current) return;

    const rows = tableRef.current.querySelectorAll(".dept-row");
    const rateBars = tableRef.current.querySelectorAll(".rate-bar-fill");

    const tl = gsap.timeline({ delay: 0.2 });

    // Header slide in
    if (headerRef.current) {
      tl.fromTo(
        headerRef.current,
        { opacity: 0, y: -10 },
        { opacity: 1, y: 0, duration: 0.4, ease: "power2.out" }
      );
    }

    // Stagger rows
    tl.fromTo(
      rows,
      { opacity: 0, x: -30, scale: 0.98 },
      {
        opacity: 1,
        x: 0,
        scale: 1,
        duration: 0.5,
        stagger: 0.08,
        ease: "power3.out",
      },
      "-=0.2"
    );

    // Animate rate bars
    tl.fromTo(
      rateBars,
      { width: "0%" },
      {
        width: (i: number) => `${(departments[i]?.attendanceRate ?? 0) * 100}%`,
        duration: 1,
        stagger: 0.08,
        ease: "power3.out",
      },
      "-=0.3"
    );

    return () => {
      tl.kill();
    };
  }, [departments]);

  const getStatusColor = (rate: number) => {
    if (rate >= 0.9) return "from-green-400 to-emerald-500";
    if (rate >= 0.7) return "from-yellow-400 to-amber-500";
    return "from-red-400 to-rose-500";
  };

  const getStatusGlow = (rate: number) => {
    if (rate >= 0.9) return "rgba(34,197,94,0.3)";
    if (rate >= 0.7) return "rgba(245,158,11,0.3)";
    return "rgba(239,68,68,0.3)";
  };

  return (
    <div ref={tableRef} className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr ref={headerRef} className="border-b-2 border-gray-200/80" style={{ opacity: 0 }}>
            <th className="text-left py-3 px-4 font-semibold text-gray-600 text-xs uppercase tracking-wider">
              Department
            </th>
            <th className="text-center py-3 px-4 font-semibold text-gray-600 text-xs uppercase tracking-wider">
              Total
            </th>
            <th className="text-center py-3 px-4 font-semibold text-gray-600 text-xs uppercase tracking-wider">
              Present
            </th>
            <th className="text-center py-3 px-4 font-semibold text-gray-600 text-xs uppercase tracking-wider">
              Late
            </th>
            <th className="text-center py-3 px-4 font-semibold text-gray-600 text-xs uppercase tracking-wider">
              Absent
            </th>
            <th className="text-left py-3 px-4 font-semibold text-gray-600 text-xs uppercase tracking-wider min-w-[160px]">
              Rate
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {departments.map((dept) => (
            <tr
              key={dept.departmentId}
              className="dept-row hover:bg-gray-50/80 transition-colors group"
              style={{ opacity: 0 }}
            >
              <td className="py-3.5 px-4 font-medium text-gray-900 group-hover:text-blue-700 transition-colors">
                {dept.departmentName}
              </td>
              <td className="py-3.5 px-4 text-center text-gray-600 font-mono">
                {dept.totalPersonnel}
              </td>
              <td className="py-3.5 px-4 text-center">
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-green-50 text-green-700">
                  {dept.presentCount}
                </span>
              </td>
              <td className="py-3.5 px-4 text-center">
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700">
                  {dept.lateCount}
                </span>
              </td>
              <td className="py-3.5 px-4 text-center">
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-red-50 text-red-700">
                  {dept.absentCount}
                </span>
              </td>
              <td className="py-3.5 px-4">
                <div className="flex items-center gap-2">
                  <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                    <div
                      className={`rate-bar-fill h-full rounded-full bg-gradient-to-r ${getStatusColor(
                        dept.attendanceRate
                      )}`}
                      style={{
                        width: 0,
                        boxShadow: `0 0 6px ${getStatusGlow(dept.attendanceRate)}`,
                      }}
                    />
                  </div>
                  <span className="text-xs font-bold text-gray-700 tabular-nums w-12 text-right">
                    {(dept.attendanceRate * 100).toFixed(1)}%
                  </span>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
