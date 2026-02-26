"use client";

import { useEffect, useState, useCallback } from "react";
import {
  Users,
  GraduationCap,
  ClipboardCheck,
  Clock,
  AlertTriangle,
  UserX,
} from "lucide-react";
import { StatCard, Card, CardHeader, CardTitle } from "@/components/ui";
import { reportService } from "@/services";
import type { DailyReportResponse, DepartmentAttendanceSummary } from "@/types/api";
import { notify } from "@/lib/toast";
import { formatDate } from "@/lib/utils";

export default function DashboardPage() {
  const [report, setReport] = useState<DailyReportResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);

  const loadDashboard = useCallback(async () => {
    setHasError(false);
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

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-32 bg-white rounded-xl animate-pulse border" />
          ))}
        </div>
        <div className="h-64 bg-white rounded-xl animate-pulse border" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-sm text-gray-500 mt-1">
          Attendance overview for {report ? formatDate(report.date) : "today"}
        </p>
      </div>

      {/* Stat Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Total Scans"
          value={report?.totalScans ?? 0}
          icon={ClipboardCheck}
          color="blue"
          description="Today's scan count"
        />
        <StatCard
          title="On Time"
          value={report?.onTimeCount ?? 0}
          icon={Clock}
          color="green"
          description="Arrived on schedule"
        />
        <StatCard
          title="Late"
          value={report?.lateCount ?? 0}
          icon={AlertTriangle}
          color="yellow"
          description="Arrived late"
        />
        <StatCard
          title="Absent"
          value={report?.absentCount ?? 0}
          icon={UserX}
          color="red"
          description="Did not attend"
        />
      </div>

      {/* People Overview */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <Card>
          <CardHeader>
            <CardTitle>People Present</CardTitle>
          </CardHeader>
          <div className="grid grid-cols-2 gap-6">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-50 rounded-lg">
                <GraduationCap className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">
                  {report?.uniqueStudents ?? 0}
                </p>
                <p className="text-sm text-gray-500">Students</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="p-2 bg-purple-50 rounded-lg">
                <Users className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">
                  {report?.uniqueStaff ?? 0}
                </p>
                <p className="text-sm text-gray-500">Staff</p>
              </div>
            </div>
          </div>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Attendance Rate by Status</CardTitle>
          </CardHeader>
          <div className="space-y-3">
            {report && report.totalScans > 0 ? (
              <>
                <AttendanceBar
                  label="On Time"
                  count={report.onTimeCount}
                  total={report.totalScans}
                  color="bg-green-500"
                />
                <AttendanceBar
                  label="Late"
                  count={report.lateCount}
                  total={report.totalScans}
                  color="bg-yellow-500"
                />
                <AttendanceBar
                  label="Absent"
                  count={report.absentCount}
                  total={report.totalScans}
                  color="bg-red-500"
                />
              </>
            ) : (
              <p className="text-sm text-gray-500 py-4 text-center">
                No attendance data for today yet.
              </p>
            )}
          </div>
        </Card>
      </div>

      {/* Department Table */}
      {report?.byDepartment && report.byDepartment.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Department Summary</CardTitle>
          </CardHeader>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left py-3 px-4 font-medium text-gray-500 text-xs uppercase">
                    Department
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-gray-500 text-xs uppercase">
                    Total
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-gray-500 text-xs uppercase">
                    Present
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-gray-500 text-xs uppercase">
                    Late
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-gray-500 text-xs uppercase">
                    Absent
                  </th>
                  <th className="text-left py-3 px-4 font-medium text-gray-500 text-xs uppercase">
                    Rate
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {report.byDepartment.map((dept: DepartmentAttendanceSummary) => (
                  <tr key={dept.departmentId} className="hover:bg-gray-50">
                    <td className="py-3 px-4 font-medium text-gray-900">
                      {dept.departmentName}
                    </td>
                    <td className="py-3 px-4 text-gray-600">
                      {dept.totalPersonnel}
                    </td>
                    <td className="py-3 px-4 text-green-600 font-medium">
                      {dept.presentCount}
                    </td>
                    <td className="py-3 px-4 text-yellow-600 font-medium">
                      {dept.lateCount}
                    </td>
                    <td className="py-3 px-4 text-red-600 font-medium">
                      {dept.absentCount}
                    </td>
                    <td className="py-3 px-4">
                      <span className="text-sm font-semibold text-gray-900">
                        {(dept.attendanceRate * 100).toFixed(1)}%
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}

function AttendanceBar({
  label,
  count,
  total,
  color,
}: {
  label: string;
  count: number;
  total: number;
  color: string;
}) {
  const percentage = total > 0 ? (count / total) * 100 : 0;
  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <span className="text-sm text-gray-600">{label}</span>
        <span className="text-sm font-medium text-gray-900">
          {count} ({percentage.toFixed(1)}%)
        </span>
      </div>
      <div className="w-full bg-gray-100 rounded-full h-2">
        <div
          className={`h-2 rounded-full ${color} transition-all duration-500`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}
