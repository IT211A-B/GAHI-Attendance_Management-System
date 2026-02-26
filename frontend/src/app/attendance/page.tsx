"use client";

import { useEffect, useState, useCallback } from "react";
import {
  Card,
  CardHeader,
  CardTitle,
  DataTable,
  Pagination,
  Badge,
  Select,
  Input,
} from "@/components/ui";
import { attendanceService } from "@/services";
import type {
  AttendanceLogResponse,
  PagedResult,
  AttendanceFilterRequest,
} from "@/types/api";
import { formatDateTime } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";

export default function AttendancePage() {
  const [data, setData] = useState<PagedResult<AttendanceLogResponse> | null>(
    null
  );
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<AttendanceFilterRequest>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });

  const loadAttendance = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await attendanceService.getAll(filters);
      if (res.success && res.data) setData(res.data);
    } catch {
      notify.error("Failed to load attendance logs");
    } finally {
      setIsLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    loadAttendance();
  }, [loadAttendance]);

  const statusVariant = (s: string) => {
    if (s === "OnTime") return "success" as const;
    if (s === "Late") return "warning" as const;
    if (s === "Absent") return "danger" as const;
    return "default" as const;
  };

  const columns = [
    {
      key: "scannedAt",
      label: "Time",
      render: (a: AttendanceLogResponse) => (
        <span className="text-sm">{formatDateTime(a.scannedAt)}</span>
      ),
    },
    {
      key: "person",
      label: "Person",
      render: (a: AttendanceLogResponse) => (
        <div>
          <p className="font-medium text-gray-900">
            {a.studentName || a.staffName || "Unknown"}
          </p>
          <p className="text-xs text-gray-500">
            {a.studentIdNumber || a.employeeIdNumber}
          </p>
        </div>
      ),
    },
    {
      key: "personType",
      label: "Type",
      render: (a: AttendanceLogResponse) => (
        <Badge variant={a.personType === "Student" ? "info" : "default"}>
          {a.personType}
        </Badge>
      ),
    },
    {
      key: "scanType",
      label: "Scan",
      render: (a: AttendanceLogResponse) => (
        <Badge variant={a.scanType === "Entry" ? "success" : "warning"}>
          {a.scanType}
        </Badge>
      ),
    },
    {
      key: "status",
      label: "Status",
      render: (a: AttendanceLogResponse) => (
        <Badge variant={statusVariant(a.status)}>{a.status}</Badge>
      ),
    },
    {
      key: "verificationStatus",
      label: "Verification",
      render: (a: AttendanceLogResponse) => (
        <Badge
          variant={
            a.verificationStatus === "Verified" ? "success" : "danger"
          }
        >
          {a.verificationStatus}
        </Badge>
      ),
    },
    { key: "gateTerminalName", label: "Terminal" },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Attendance Logs</h1>
        <p className="text-sm text-gray-500 mt-1">
          View all attendance scan records
        </p>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
        </CardHeader>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <Input
            id="date"
            label="Date"
            type="date"
            value={filters.date ?? ""}
            onChange={(e) =>
              setFilters({ ...filters, date: e.target.value || undefined, page: 1 })
            }
          />
          <Select
            id="personType"
            label="Person Type"
            value={filters.personType ?? ""}
            onChange={(e) =>
              setFilters({
                ...filters,
                personType: (e.target.value as AttendanceFilterRequest["personType"]) || undefined,
                page: 1,
              })
            }
            options={[
              { label: "All", value: "" },
              { label: "Student", value: "Student" },
              { label: "Staff", value: "Staff" },
            ]}
          />
          <Select
            id="status"
            label="Status"
            value={filters.status ?? ""}
            onChange={(e) =>
              setFilters({
                ...filters,
                status: (e.target.value as AttendanceFilterRequest["status"]) || undefined,
                page: 1,
              })
            }
            options={[
              { label: "All", value: "" },
              { label: "On Time", value: "OnTime" },
              { label: "Late", value: "Late" },
              { label: "Absent", value: "Absent" },
            ]}
          />
        </div>
      </Card>

      {/* Table */}
      <Card>
        <DataTable
          columns={columns}
          data={data?.items ?? []}
          keyExtractor={(a) => a.id}
          isLoading={isLoading}
          emptyMessage="No attendance records found."
        />
        {data && data.totalPages > 1 && (
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hasPrevious={data.hasPrevious}
            hasNext={data.hasNext}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={(p) => setFilters({ ...filters, page: p })}
          />
        )}
      </Card>
    </div>
  );
}
