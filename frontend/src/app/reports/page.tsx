"use client";

import { useEffect, useState, useCallback } from "react";
import { Card, CardHeader, CardTitle, Input, Badge } from "@/components/ui";
import { reportService } from "@/services";
import type { DailyReportResponse, WeeklyReportResponse, DepartmentAttendanceSummary } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";

export default function ReportsPage() {
  const [tab, setTab] = useState<"daily" | "weekly" | "department">("daily");
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [dailyReport, setDailyReport] = useState<DailyReportResponse | null>(null);
  const [weeklyReport, setWeeklyReport] = useState<WeeklyReportResponse | null>(null);
  const [deptSummary, setDeptSummary] = useState<DepartmentAttendanceSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const loadReport = useCallback(async () => {
    setIsLoading(true);
    try {
      if (tab === "daily") {
        const res = await reportService.getDaily(date);
        if (res.success && res.data) setDailyReport(res.data);
      } else if (tab === "weekly") {
        const res = await reportService.getWeekly(date);
        if (res.success && res.data) setWeeklyReport(res.data);
      } else {
        const res = await reportService.getDepartmentSummary(date);
        if (res.success && res.data) setDeptSummary(res.data);
      }
    } catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [tab, date]);

  useEffect(() => { loadReport(); }, [loadReport]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Reports</h1>
        <p className="text-sm text-gray-500 mt-1">Attendance analytics and insights</p>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-4">
        <div className="flex bg-white border rounded-lg p-1" role="tablist" aria-label="Report type">
          {(["daily", "weekly", "department"] as const).map((t) => (
            <button key={t} onClick={() => setTab(t)} role="tab" aria-selected={tab === t} className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${tab === t ? "bg-blue-600 text-white" : "text-gray-600 hover:text-gray-900"}`}>
              {t.charAt(0).toUpperCase() + t.slice(1)}
            </button>
          ))}
        </div>
        <Input id="reportDate" label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} className="max-w-[180px]" />
      </div>

      {isLoading ? (
        <div className="space-y-4">{[...Array(3)].map((_, i) => <div key={i} className="h-24 bg-white rounded-xl animate-pulse border" />)}</div>
      ) : (
        <>
          {/* Daily Report */}
          {tab === "daily" && dailyReport && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <StatBox label="Total Scans" value={dailyReport.totalScans} />
                <StatBox label="On Time" value={dailyReport.onTimeCount} color="text-green-600" />
                <StatBox label="Late" value={dailyReport.lateCount} color="text-yellow-600" />
                <StatBox label="Absent" value={dailyReport.absentCount} color="text-red-600" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <StatBox label="Unique Students" value={dailyReport.uniqueStudents} />
                <StatBox label="Unique Staff" value={dailyReport.uniqueStaff} />
              </div>
              {dailyReport.byDepartment.length > 0 && (
                <Card>
                  <CardHeader><CardTitle>By Department</CardTitle></CardHeader>
                  <DeptTable data={dailyReport.byDepartment} />
                </Card>
              )}
            </div>
          )}
          {tab === "daily" && !dailyReport && !isLoading && (
            <Card><p className="text-sm text-gray-500 text-center py-8">No daily report available for this date.</p></Card>
          )}

          {/* Weekly Report */}
          {tab === "weekly" && weeklyReport && (
            <div className="space-y-4">
              <Card>
                <CardHeader><CardTitle>Weekly Summary ({formatDate(weeklyReport.startDate)} — {formatDate(weeklyReport.endDate)})</CardTitle></CardHeader>
                <div className="grid grid-cols-3 gap-6">
                  <StatBox label="Total Scans" value={weeklyReport.totalScans} />
                  <StatBox label="Avg On-Time Rate" value={`${(weeklyReport.averageOnTimeRate * 100).toFixed(1)}%`} color="text-green-600" />
                  <StatBox label="Avg Late Rate" value={`${(weeklyReport.averageLateRate * 100).toFixed(1)}%`} color="text-yellow-600" />
                </div>
              </Card>
              <Card>
                <CardHeader><CardTitle>Daily Breakdown</CardTitle></CardHeader>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead><tr className="border-b"><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Date</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Scans</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">On Time</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Late</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Absent</th></tr></thead>
                    <tbody className="divide-y divide-gray-100">
                      {weeklyReport.dailyBreakdown.map((d) => (
                        <tr key={d.date} className="hover:bg-gray-50">
                          <td className="py-2 px-4">{formatDate(d.date)}</td>
                          <td className="py-2 px-4">{d.totalScans}</td>
                          <td className="py-2 px-4 text-green-600">{d.onTimeCount}</td>
                          <td className="py-2 px-4 text-yellow-600">{d.lateCount}</td>
                          <td className="py-2 px-4 text-red-600">{d.absentCount}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </Card>
            </div>
          )}
          {tab === "weekly" && !weeklyReport && !isLoading && (
            <Card><p className="text-sm text-gray-500 text-center py-8">No weekly report available for this date range.</p></Card>
          )}

          {/* Department Summary */}
          {tab === "department" && deptSummary.length > 0 && (
            <Card>
              <CardHeader><CardTitle>Department Attendance Summary</CardTitle></CardHeader>
              <DeptTable data={deptSummary} />
            </Card>
          )}
          {tab === "department" && deptSummary.length === 0 && !isLoading && (
            <Card><p className="text-sm text-gray-500 text-center py-8">No department data available for this date.</p></Card>
          )}
        </>
      )}
    </div>
  );
}

function StatBox({ label, value, color }: { label: string; value: string | number; color?: string }) {
  return (
    <div className="bg-white rounded-xl border p-4">
      <p className="text-sm text-gray-500">{label}</p>
      <p className={`text-2xl font-bold ${color ?? "text-gray-900"} mt-1`}>{value}</p>
    </div>
  );
}

function DeptTable({ data }: { data: DepartmentAttendanceSummary[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead><tr className="border-b"><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Department</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Total</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Present</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Late</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Absent</th><th className="text-left py-2 px-4 font-medium text-gray-500 text-xs">Rate</th></tr></thead>
        <tbody className="divide-y divide-gray-100">
          {data.map((d) => (
            <tr key={d.departmentId} className="hover:bg-gray-50">
              <td className="py-2 px-4 font-medium">{d.departmentName}</td>
              <td className="py-2 px-4">{d.totalPersonnel}</td>
              <td className="py-2 px-4 text-green-600">{d.presentCount}</td>
              <td className="py-2 px-4 text-yellow-600">{d.lateCount}</td>
              <td className="py-2 px-4 text-red-600">{d.absentCount}</td>
              <td className="py-2 px-4"><Badge variant={(d.attendanceRate * 100) >= 80 ? "success" : (d.attendanceRate * 100) >= 60 ? "warning" : "danger"}>{(d.attendanceRate * 100).toFixed(1)}%</Badge></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
