"use client";

import { useEffect, useState, useCallback } from "react";
import { FileText, Filter } from "lucide-react";
import { Card, DataTable, Pagination, Input, Select, Badge } from "@/components/ui";
import { auditLogService } from "@/services";
import type { AuditLogResponse, PagedResult } from "@/types/api";
import { formatDateTime } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { LARGE_PAGE_SIZE } from "@/lib/constants";

const actionColors: Record<string, "default" | "success" | "warning" | "danger" | "info"> = {
  Create: "success",
  Update: "info",
  Delete: "danger",
  Login: "default",
  Logout: "default",
};

export default function AuditLogsPage() {
  const [data, setData] = useState<PagedResult<AuditLogResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState({ entityType: "", action: "", startDate: "", endDate: "" });

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const params: Record<string, string | number> = { page, pageSize: LARGE_PAGE_SIZE };
      if (filters.entityType) params.entityType = filters.entityType;
      if (filters.action) params.action = filters.action;
      if (filters.startDate) params.startDate = filters.startDate;
      if (filters.endDate) params.endDate = filters.endDate;
      const res = await auditLogService.getAll(params);
      if (res.success && res.data) setData(res.data);
    } catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [page, filters]);

  useEffect(() => { load(); }, [load]);

  const handleFilter = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setPage(1);
  };

  const columns = [
    { key: "performedAt", label: "Timestamp", render: (log: AuditLogResponse) => <span className="text-sm font-medium text-gray-900 whitespace-nowrap">{formatDateTime(log.performedAt)}</span> },
    { key: "action", label: "Action", render: (log: AuditLogResponse) => <Badge variant={actionColors[log.action] ?? "default"}>{log.action}</Badge> },
    { key: "entityName", label: "Entity", render: (log: AuditLogResponse) => (
      <div>
        <p className="text-sm font-medium text-gray-900">{log.entityName}</p>
        <p className="text-xs text-gray-400 font-mono">{log.entityId?.slice(0, 8)}…</p>
      </div>
    )},
    { key: "performedBy", label: "Performed By", render: (log: AuditLogResponse) => <span className="text-sm text-gray-700">{log.performedByUsername ?? "System"}</span> },
    { key: "oldValues", label: "Old Values", render: (log: AuditLogResponse) => <span className="text-sm text-gray-500 max-w-xs truncate block">{log.oldValues ?? "—"}</span> },
    { key: "newValues", label: "New Values", render: (log: AuditLogResponse) => <span className="text-sm text-gray-500 max-w-xs truncate block">{log.newValues ?? "—"}</span> },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <FileText className="h-7 w-7 text-gray-400" />
        <div><h1 className="text-2xl font-bold text-gray-900">Audit Logs</h1><p className="text-sm text-gray-500">System activity and change history</p></div>
      </div>

      <Card>
        <div className="p-4 border-b flex flex-wrap items-end gap-4">
          <Filter className="h-4 w-4 text-gray-400 mt-6" />
          <Select id="entityType" label="Entity Type" value={filters.entityType} onChange={(e) => handleFilter("entityType", e.target.value)}
            options={[{ value: "", label: "All Entities" }, { value: "Student", label: "Student" }, { value: "Staff", label: "Staff" }, { value: "Department", label: "Department" }, { value: "Section", label: "Section" }, { value: "AttendanceLog", label: "Attendance" }, { value: "User", label: "User" }, { value: "GateTerminal", label: "Gate Terminal" }]} />
          <Select id="action" label="Action" value={filters.action} onChange={(e) => handleFilter("action", e.target.value)}
            options={[{ value: "", label: "All Actions" }, { value: "Create", label: "Create" }, { value: "Update", label: "Update" }, { value: "Delete", label: "Delete" }, { value: "Login", label: "Login" }, { value: "Logout", label: "Logout" }]}
          />
          <Input id="startDate" label="From" type="date" value={filters.startDate} onChange={(e) => handleFilter("startDate", e.target.value)} />
          <Input id="endDate" label="To" type="date" value={filters.endDate} onChange={(e) => handleFilter("endDate", e.target.value)} />
        </div>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(log) => log.id} isLoading={isLoading} emptyMessage="No audit logs found." />
        {data && data.totalPages > 1 && <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
    </div>
  );
}
