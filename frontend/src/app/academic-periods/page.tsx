"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, Pencil, Trash2, Star } from "lucide-react";
import { Card, Button, DataTable, Pagination, ConfirmDialog, Modal, Input, Checkbox, Badge } from "@/components/ui";
import { academicPeriodService } from "@/services";
import type { AcademicPeriodResponse, PagedResult } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";

export default function AcademicPeriodsPage() {
  const [data, setData] = useState<PagedResult<AcademicPeriodResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<AcademicPeriodResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [form, setForm] = useState({ name: "", startDate: "", endDate: "", isCurrent: false });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try { const res = await academicPeriodService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE }); if (res.success && res.data) setData(res.data); }
    catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => { setEditing(null); setForm({ name: "", startDate: "", endDate: "", isCurrent: false }); setErrors({}); setShowForm(true); };
  const openEdit = (p: AcademicPeriodResponse) => { setEditing(p); setForm({ name: p.name, startDate: p.startDate.slice(0, 10), endDate: p.endDate.slice(0, 10), isCurrent: p.isCurrent }); setErrors({}); setShowForm(true); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.name.trim()) errs.name = "Required";
    if (!form.startDate) errs.startDate = "Required";
    if (!form.endDate) errs.endDate = "Required";
    setErrors(errs); if (Object.keys(errs).length > 0) return;
    setIsSaving(true);
    try {
      if (editing) { await academicPeriodService.update(editing.id, form); notify.success("Period updated"); }
      else { await academicPeriodService.create(form); notify.success("Period created"); }
      setShowForm(false); load();
    } catch (err) { notify.error(err); }
    finally { setIsSaving(false); }
  };

  const handleSetCurrent = async (id: string) => {
    try { await academicPeriodService.setCurrent(id); notify.success("Current period updated"); load(); }
    catch (err) { notify.error(err); }
  };

  const handleDelete = async () => { if (!deletingId) return; setIsDeleting(true); try { await academicPeriodService.delete(deletingId); notify.success("Period deleted"); setDeletingId(null); load(); } catch (err) { notify.error(err); } finally { setIsDeleting(false); } };

  const columns = [
    { key: "name", label: "Name", render: (p: AcademicPeriodResponse) => (
      <div className="flex items-center gap-2">
        <span className="font-medium text-gray-900">{p.name}</span>
        {p.isCurrent && <Badge variant="success">Current</Badge>}
      </div>
    )},
    { key: "startDate", label: "Start", render: (p: AcademicPeriodResponse) => <span className="text-sm">{formatDate(p.startDate)}</span> },
    { key: "endDate", label: "End", render: (p: AcademicPeriodResponse) => <span className="text-sm">{formatDate(p.endDate)}</span> },
    { key: "sectionCount", label: "Sections" },
    { key: "actions", label: "", className: "text-right", render: (p: AcademicPeriodResponse) => (
      <div className="flex items-center justify-end gap-1">
        {!p.isCurrent && <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); handleSetCurrent(p.id); }} aria-label="Set as current period"><Star className="h-4 w-4 text-yellow-500" /></Button>}
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); openEdit(p); }} aria-label="Edit period"><Pencil className="h-4 w-4" /></Button>
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(p.id); }} aria-label="Delete period"><Trash2 className="h-4 w-4 text-red-500" /></Button>
      </div>
    )},
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div><h1 className="text-2xl font-bold text-gray-900">Academic Periods</h1><p className="text-sm text-gray-500 mt-1">Manage semesters and academic terms</p></div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Period</Button>
      </div>
      <Card>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(p) => p.id} isLoading={isLoading} emptyMessage="No academic periods found." />
        {data && data.totalPages > 1 && <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit Period" : "Add Period"}>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input id="name" label="Period Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} error={errors.name} placeholder="e.g. 1st Semester 2025-2026" />
          <div className="grid grid-cols-2 gap-4">
            <Input id="startDate" label="Start Date" type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} error={errors.startDate} />
            <Input id="endDate" label="End Date" type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} error={errors.endDate} />
          </div>
          <Checkbox id="isCurrent" label="Set as current period" checked={form.isCurrent} onChange={(e) => setForm({ ...form, isCurrent: e.target.checked })} />
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>
      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Period" message="This will permanently delete this period." isLoading={isDeleting} />
    </div>
  );
}
