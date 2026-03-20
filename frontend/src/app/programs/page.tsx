"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, Pencil, Trash2 } from "lucide-react";
import { Card, Button, DataTable, Pagination, ConfirmDialog, Modal, Input, Select } from "@/components/ui";
import { academicProgramService, departmentService } from "@/services";
import type { AcademicProgramResponse, PagedResult, DepartmentResponse } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE, DROPDOWN_PAGE_SIZE } from "@/lib/constants";

export default function ProgramsPage() {
  const [data, setData] = useState<PagedResult<AcademicProgramResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<AcademicProgramResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [departments, setDepartments] = useState<DepartmentResponse[]>([]);
  const [form, setForm] = useState({ name: "", code: "", description: "", departmentId: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try { const res = await academicProgramService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE }); if (res.success && res.data) setData(res.data); }
    catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const loadDepts = async () => { try { const res = await departmentService.getAll({ pageSize: DROPDOWN_PAGE_SIZE }); if (res.success && res.data) setDepartments(res.data.items); } catch (err) { notify.error(err); } };

  const openCreate = () => { setEditing(null); setForm({ name: "", code: "", description: "", departmentId: "" }); setErrors({}); loadDepts(); setShowForm(true); };
  const openEdit = (p: AcademicProgramResponse) => { setEditing(p); setForm({ name: p.name, code: p.code, description: p.description ?? "", departmentId: p.departmentId }); setErrors({}); loadDepts(); setShowForm(true); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.name.trim()) errs.name = "Required";
    if (!form.code.trim()) errs.code = "Required";
    if (!form.departmentId) errs.departmentId = "Required";
    setErrors(errs); if (Object.keys(errs).length > 0) return;
    setIsSaving(true);
    try {
      if (editing) { await academicProgramService.update(editing.id, { name: form.name, code: form.code, description: form.description || undefined, departmentId: form.departmentId }); notify.success("Program updated"); }
      else { await academicProgramService.create({ name: form.name, code: form.code, description: form.description || undefined, departmentId: form.departmentId }); notify.success("Program created"); }
      setShowForm(false); load();
    } catch (err) { notify.error(err); }
    finally { setIsSaving(false); }
  };

  const handleDelete = async () => { if (!deletingId) return; setIsDeleting(true); try { await academicProgramService.delete(deletingId); notify.success("Program deleted"); setDeletingId(null); load(); } catch (err) { notify.error(err); } finally { setIsDeleting(false); } };

  const columns = [
    { key: "code", label: "Code", render: (p: AcademicProgramResponse) => <span className="font-mono font-medium">{p.code}</span> },
    { key: "name", label: "Name", render: (p: AcademicProgramResponse) => <span className="font-medium text-gray-900">{p.name}</span> },
    { key: "departmentName", label: "Department" },
    { key: "sectionCount", label: "Sections" },
    { key: "createdAt", label: "Created", render: (p: AcademicProgramResponse) => <span className="text-sm text-gray-500">{formatDate(p.createdAt)}</span> },
    { key: "actions", label: "", className: "text-right", render: (p: AcademicProgramResponse) => (
      <div className="flex items-center justify-end gap-1">
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); openEdit(p); }} aria-label="Edit program"><Pencil className="h-4 w-4" /></Button>
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(p.id); }} aria-label="Delete program"><Trash2 className="h-4 w-4 text-red-500" /></Button>
      </div>
    )},
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div><h1 className="text-2xl font-bold text-gray-900">Academic Programs</h1><p className="text-sm text-gray-500 mt-1">Manage academic programs and courses</p></div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Program</Button>
      </div>
      <Card>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(p) => p.id} isLoading={isLoading} emptyMessage="No programs found." />
        {data && data.totalPages > 1 && <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit Program" : "Add Program"} size="lg">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <Input id="name" label="Program Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} error={errors.name} placeholder="e.g. BS Information Technology" />
            <Input id="code" label="Code" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} error={errors.code} placeholder="e.g. BSIT" disabled={!!editing} />
          </div>
          <Select id="departmentId" label="Department" value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value })} options={departments.map((d) => ({ label: d.name, value: d.id }))} placeholder="Select department" error={errors.departmentId} />
          <Input id="description" label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="Optional description" />
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>
      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Program" message="This will permanently delete this program." isLoading={isDeleting} />
    </div>
  );
}
