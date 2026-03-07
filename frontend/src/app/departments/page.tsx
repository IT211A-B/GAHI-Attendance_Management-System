"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, Pencil, Trash2 } from "lucide-react";
import {
  Card,
  Button,
  DataTable,
  Pagination,
  ConfirmDialog,
  Modal,
  Input,
} from "@/components/ui";
import { departmentService } from "@/services";
import type { DepartmentResponse, PagedResult } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";

export default function DepartmentsPage() {
  const [data, setData] = useState<PagedResult<DepartmentResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<DepartmentResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const [form, setForm] = useState({ name: "", code: "", description: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await departmentService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE });
      if (res.success && res.data) setData(res.data);
    } catch {
      notify.error("Failed to load departments");
    } finally {
      setIsLoading(false);
    }
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => {
    setEditing(null);
    setForm({ name: "", code: "", description: "" });
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (d: DepartmentResponse) => {
    setEditing(d);
    setForm({ name: d.name, code: d.code, description: d.description ?? "" });
    setErrors({});
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.name.trim()) errs.name = "Name is required";
    if (!form.code.trim()) errs.code = "Code is required";
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setIsSaving(true);
    try {
      if (editing) {
        await departmentService.update(editing.id, {
          name: form.name,
          code: form.code,
          description: form.description || undefined,
        });
        notify.success("Department updated");
      } else {
        await departmentService.create({
          name: form.name,
          code: form.code,
          description: form.description || undefined,
        });
        notify.success("Department created");
      }
      setShowForm(false);
      load();
    } catch {
      notify.error("Operation failed");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deletingId) return;
    setIsDeleting(true);
    try {
      await departmentService.delete(deletingId);
      notify.success("Department deleted");
      setDeletingId(null);
      load();
    } catch {
      notify.error("Failed to delete");
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = [
    { key: "code", label: "Code", render: (d: DepartmentResponse) => <span className="font-mono font-medium">{d.code}</span> },
    { key: "name", label: "Name", render: (d: DepartmentResponse) => <span className="font-medium text-gray-900">{d.name}</span> },
    { key: "description", label: "Description", render: (d: DepartmentResponse) => <span className="text-sm text-gray-500">{d.description || "—"}</span> },
    { key: "programCount", label: "Programs" },
    { key: "staffCount", label: "Staff" },
    { key: "createdAt", label: "Created", render: (d: DepartmentResponse) => <span className="text-sm text-gray-500">{formatDate(d.createdAt)}</span> },
    {
      key: "actions", label: "", className: "text-right",
      render: (d: DepartmentResponse) => (
        <div className="flex items-center justify-end gap-1">
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); openEdit(d); }} aria-label="Edit department"><Pencil className="h-4 w-4" /></Button>
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(d.id); }} aria-label="Delete department"><Trash2 className="h-4 w-4 text-red-500" /></Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Departments</h1>
          <p className="text-sm text-gray-500 mt-1">Manage academic departments</p>
        </div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Department</Button>
      </div>

      <Card>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(d) => d.id} isLoading={isLoading} emptyMessage="No departments found." />
        {data && data.totalPages > 1 && (
          <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />
        )}
      </Card>

      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit Department" : "Add Department"}>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input id="name" label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} error={errors.name} placeholder="Department name" />
          <Input id="code" label="Code" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} error={errors.code} placeholder="e.g. BSIT" disabled={!!editing} />
          <Input id="description" label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="Optional description" />
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Department" message="This will permanently delete this department." isLoading={isDeleting} />
    </div>
  );
}
