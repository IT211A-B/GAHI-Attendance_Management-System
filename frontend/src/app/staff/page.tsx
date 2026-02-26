"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, QrCode, Pencil, Trash2 } from "lucide-react";
import {
  Card,
  Button,
  DataTable,
  Pagination,
  SearchBar,
  Badge,
  ConfirmDialog,
} from "@/components/ui";
import { staffService } from "@/services";
import type { StaffResponse, PagedResult } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";
import StaffFormModal from "./_components/staff-form-modal";

export default function StaffPage() {
  const [data, setData] = useState<PagedResult<StaffResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editingStaff, setEditingStaff] = useState<StaffResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const loadStaff = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await staffService.getAll({
        page,
        pageSize: DEFAULT_PAGE_SIZE,
        search: search || undefined,
      });
      if (res.success && res.data) setData(res.data);
    } catch {
      notify.error("Failed to load staff");
    } finally {
      setIsLoading(false);
    }
  }, [page, search]);

  useEffect(() => {
    loadStaff();
  }, [loadStaff]);

  const handleSearch = useCallback((query: string) => {
    setSearch(query);
    setPage(1);
  }, []);

  const handleDelete = async () => {
    if (!deletingId) return;
    setIsDeleting(true);
    try {
      await staffService.delete(deletingId);
      notify.success("Staff member deleted");
      setDeletingId(null);
      loadStaff();
    } catch {
      notify.error("Failed to delete staff");
    } finally {
      setIsDeleting(false);
    }
  };

  const handleRegenerateQr = async (id: string) => {
    try {
      await staffService.regenerateQr(id);
      notify.success("QR code regenerated");
      loadStaff();
    } catch {
      notify.error("Failed to regenerate QR code");
    }
  };

  const staffTypeLabel = (type: string) => {
    const map: Record<string, string> = {
      Teaching: "Teaching",
      NonTeaching: "Non-Teaching",
      Security: "Security",
      Administrative: "Admin",
    };
    return map[type] ?? type;
  };

  const columns = [
    {
      key: "employeeIdNumber",
      label: "Employee ID",
      render: (s: StaffResponse) => (
        <span className="font-mono text-sm font-medium">{s.employeeIdNumber}</span>
      ),
    },
    {
      key: "fullName",
      label: "Name",
      render: (s: StaffResponse) => (
        <div>
          <p className="font-medium text-gray-900">{s.fullName}</p>
          {s.email && <p className="text-xs text-gray-500">{s.email}</p>}
        </div>
      ),
    },
    {
      key: "staffType",
      label: "Type",
      render: (s: StaffResponse) => (
        <Badge variant="info">{staffTypeLabel(s.staffType)}</Badge>
      ),
    },
    { key: "departmentName", label: "Department" },
    {
      key: "createdAt",
      label: "Created",
      render: (s: StaffResponse) => (
        <span className="text-sm text-gray-500">{formatDate(s.createdAt)}</span>
      ),
    },
    {
      key: "actions",
      label: "",
      className: "text-right",
      render: (s: StaffResponse) => (
        <div className="flex items-center justify-end gap-1">
          <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); handleRegenerateQr(s.id); }} aria-label="Regenerate QR code">
            <QrCode className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setEditingStaff(s); setShowForm(true); }} aria-label="Edit staff">
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(s.id); }} aria-label="Delete staff">
            <Trash2 className="h-4 w-4 text-red-500" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Staff</h1>
          <p className="text-sm text-gray-500 mt-1">Manage staff members</p>
        </div>
        <Button onClick={() => { setEditingStaff(null); setShowForm(true); }}>
          <Plus className="h-4 w-4" />
          Add Staff
        </Button>
      </div>

      <Card>
        <div className="mb-4">
          <SearchBar placeholder="Search by name or employee ID..." onSearch={handleSearch} className="max-w-sm" />
        </div>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(s) => s.id} isLoading={isLoading} emptyMessage="No staff found." />
        {data && data.totalPages > 1 && (
          <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />
        )}
      </Card>

      <StaffFormModal isOpen={showForm} onClose={() => { setShowForm(false); setEditingStaff(null); }} onSuccess={() => { setShowForm(false); setEditingStaff(null); loadStaff(); }} staff={editingStaff} />
      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Staff" message="Are you sure you want to delete this staff member?" isLoading={isDeleting} />
    </div>
  );
}
