/**
 * DashboardPage  (/dashboard)
 * ──────────────────────
 * Animated dashboard overview for the Attendance Management System.
 * Fetches today's daily report and composes all animated UI components.
 *
 * Data source:
 *  reportService.getDaily() → DailyReportResponse
 *
 * Layout:
 *  ┌─────────────────────────────────────────────┐
 *  │ FloatingParticles (absolute, z-0)              │
 *  │ WelcomeBanner (gradient hero + Lottie)          │
 *  │ [StatCard x4]  Total Scans / OnTime / Late / Absent │
 *  │ [DonutChart]  [PeopleCard + AttendanceBars]     │
 *  │ [DepartmentTable] (conditional)                 │
 *  └─────────────────────────────────────────────┘
 *
 * States:
 *  - Loading:  GSAP-pulsing skeleton blocks (tween killed on exit)
 *  - Error:    WelcomeBanner + error card + Retry button
 *  - Success:  Full animated dashboard
 *
 * Animation libraries used:
 *  - GSAP:     StatCards, AttendanceBars, DepartmentTable, skeleton, particles
 *  - Anime.js: DonutChart, PeopleCard, LivePulseIndicator
 *  - Lottie:   WelcomeBanner illustration
 *
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useState, useCallback, useRef } from "react";
import {
  ClipboardCheck,
  Clock,
  AlertTriangle,
  UserX,
} from "lucide-react";
import { gsap } from "gsap";
import { Card, CardHeader, CardTitle, Button } from "@/components/ui";
import AnimatedStatCard from "@/components/ui/animated-stat-card";
import AnimatedDonutChart from "@/components/ui/animated-donut-chart";
import AnimatedAttendanceBar from "@/components/ui/animated-attendance-bar";
import AnimatedDepartmentTable from "@/components/ui/animated-department-table";
import AnimatedPeopleCard from "@/components/ui/animated-people-card";
import WelcomeBanner from "@/components/ui/welcome-banner";
import FloatingParticles from "@/components/ui/floating-particles";
import LivePulseIndicator from "@/components/ui/live-pulse-indicator";
import { reportService } from "@/services";
import type { DailyReportResponse } from "@/types/api";
import { notify } from "@/lib/toast";
import { formatDate } from "@/lib/utils";

export default function DashboardPage() {
  const [report, setReport] = useState<DailyReportResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const loaderRef = useRef<HTMLDivElement>(null);

  const loadDashboard = useCallback(async () => {
    setHasError(false);
    setIsLoading(true);
    try {
      const res = await reportService.getDaily();
      if (res.success && res.data) {
        setReport(res.data);
      }
    } catch (err) {
      setHasError(true);
      notify.error(err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  // Animated skeleton loader
  useEffect(() => {
    if (!isLoading || !loaderRef.current) return;
    const skeletons = loaderRef.current.querySelectorAll(".skeleton-block");
    const tween = gsap.fromTo(
      skeletons,
      { opacity: 0.3 },
      {
        opacity: 0.7,
        duration: 0.8,
        stagger: 0.1,
        repeat: -1,
        yoyo: true,
        ease: "sine.inOut",
      }
    );

    return () => {
      tween.kill();
    };
  }, [isLoading]);

  if (isLoading) {
    return (
      <div ref={loaderRef} className="space-y-6">
        <div className="skeleton-block h-28 bg-gradient-to-r from-indigo-100 to-purple-100 rounded-2xl border border-indigo-200/30" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <div
              key={i}
              className="skeleton-block h-32 bg-white rounded-2xl border border-gray-200 shadow-sm"
            />
          ))}
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <div className="skeleton-block h-64 bg-white rounded-2xl border" />
          <div className="skeleton-block h-64 bg-white rounded-2xl border" />
        </div>
      </div>
    );
  }

  if (hasError && !report) {
    return (
      <div className="space-y-6">
        <WelcomeBanner dateText="today" />
        <Card>
          <div className="text-center py-12">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-red-50 mb-4">
              <AlertTriangle className="h-8 w-8 text-red-400" />
            </div>
            <p className="text-sm text-gray-500 mb-4">
              Failed to load dashboard data.
            </p>
            <Button onClick={loadDashboard} variant="outline">
              Retry
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className="relative space-y-6">
      {/* Floating background particles */}
      <FloatingParticles />

      {/* Welcome Banner with Lottie */}
      <WelcomeBanner
        dateText={report ? formatDate(report.date) : "today"}
      />

      {/* Animated Stat Cards - GSAP powered */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <AnimatedStatCard
          title="Total Scans"
          value={report?.totalScans ?? 0}
          icon={ClipboardCheck}
          color="blue"
          description="Today's scan count"
          delay={0}
        />
        <AnimatedStatCard
          title="On Time"
          value={report?.onTimeCount ?? 0}
          icon={Clock}
          color="green"
          description="Arrived on schedule"
          delay={1}
        />
        <AnimatedStatCard
          title="Late"
          value={report?.lateCount ?? 0}
          icon={AlertTriangle}
          color="yellow"
          description="Arrived late"
          delay={2}
        />
        <AnimatedStatCard
          title="Absent"
          value={report?.absentCount ?? 0}
          icon={UserX}
          color="red"
          description="Did not attend"
          delay={3}
        />
      </div>

      {/* Charts & People Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Donut Chart - Anime.js powered */}
        <Card className="relative overflow-hidden">
          <CardHeader>
            <CardTitle>Attendance Distribution</CardTitle>
            <LivePulseIndicator />
          </CardHeader>
          <AnimatedDonutChart
            onTime={report?.onTimeCount ?? 0}
            late={report?.lateCount ?? 0}
            absent={report?.absentCount ?? 0}
          />
        </Card>

        {/* People Present - Anime.js ring charts */}
        <Card className="relative overflow-hidden">
          <CardHeader>
            <CardTitle>People Present</CardTitle>
          </CardHeader>
          <AnimatedPeopleCard
            students={report?.uniqueStudents ?? 0}
            staff={report?.uniqueStaff ?? 0}
          />

          {/* Attendance Rate Bars - GSAP powered */}
          <div className="mt-6 pt-5 border-t border-gray-100 space-y-3">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">
              Status Breakdown
            </p>
            {report && report.totalScans > 0 ? (
              <>
                <AnimatedAttendanceBar
                  label="On Time"
                  count={report.onTimeCount}
                  total={report.totalScans}
                  color="bg-green-500"
                  accentHex="#22c55e"
                  delay={0}
                />
                <AnimatedAttendanceBar
                  label="Late"
                  count={report.lateCount}
                  total={report.totalScans}
                  color="bg-amber-500"
                  accentHex="#f59e0b"
                  delay={1}
                />
                <AnimatedAttendanceBar
                  label="Absent"
                  count={report.absentCount}
                  total={report.totalScans}
                  color="bg-red-500"
                  accentHex="#ef4444"
                  delay={2}
                />
              </>
            ) : (
              <p className="text-sm text-gray-400 py-4 text-center">
                No attendance data for today yet.
              </p>
            )}
          </div>
        </Card>
      </div>

      {/* Department Table - GSAP stagger animated */}
      {report?.byDepartment && report.byDepartment.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Department Summary</CardTitle>
            <div className="flex items-center gap-3">
              <LivePulseIndicator />
              <span className="text-xs text-gray-400 font-medium">
                {report.byDepartment.length} departments
              </span>
            </div>
          </CardHeader>
          <AnimatedDepartmentTable departments={report.byDepartment} />
        </Card>
      )}
    </div>
  );
}
