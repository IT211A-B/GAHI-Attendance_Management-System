"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, Pencil, Trash2 } from "lucide-react";
import { Card, Button, DataTable, Pagination, ConfirmDialog, Modal, Input, Select } from "@/components/ui";
import { sectionService, academicProgramService, academicPeriodService } from "@/services";
import type { SectionResponse, PagedResult, AcademicProgramResponse, AcademicPeriodResponse } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE, DROPDOWN_PAGE_SIZE } from "@/lib/constants";

export default function SectionsPage() {
  const [data, setData] = useState<PagedResult<SectionResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<SectionResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [programs, setPrograms] = useState<AcademicProgramResponse[]>([]);
  const [periods, setPeriods] = useState<AcademicPeriodResponse[]>([]);
  const [form, setForm] = useState({ name: "", yearLevel: "1", academicProgramId: "", academicPeriodId: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await sectionService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE });
      if (res.success && res.data) setData(res.data);
    } catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const loadDropdowns = async () => {
    try {
      const [progRes, periodRes] = await Promise.all([
        academicProgramService.getAll({ pageSize: DROPDOWN_PAGE_SIZE }),
        academicPeriodService.getAll({ pageSize: DROPDOWN_PAGE_SIZE }),
      ]);
      if (progRes.success && progRes.data) setPrograms(progRes.data.items);
      if (periodRes.success && periodRes.data) setPeriods(periodRes.data.items);
    } catch (err) { notify.error(err); }
  };

  const openCreate = () => { setEditing(null); setForm({ name: "", yearLevel: "1", academicProgramId: "", academicPeriodId: "" }); setErrors({}); loadDropdowns(); setShowForm(true); };
  const openEdit = (s: SectionResponse) => { setEditing(s); setForm({ name: s.name, yearLevel: String(s.yearLevel), academicProgramId: s.academicProgramId, academicPeriodId: s.academicPeriodId }); setErrors({}); loadDropdowns(); setShowForm(true); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.name.trim()) errs.name = "Required";
    if (!form.academicProgramId) errs.academicProgramId = "Required";
    if (!form.academicPeriodId) errs.academicPeriodId = "Required";
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;
    setIsSaving(true);
    try {
      const payload = { name: form.name, yearLevel: Number(form.yearLevel), academicProgramId: form.academicProgramId, academicPeriodId: form.academicPeriodId };
      if (editing) { await sectionService.update(editing.id, payload); notify.success("Section updated"); }
      else { await sectionService.create(payload); notify.success("Section created"); }
      setShowForm(false); load();
    } catch (err) { notify.error(err); }
    finally { setIsSaving(false); }
  };

  const handleDelete = async () => { if (!deletingId) return; setIsDeleting(true); try { await sectionService.delete(deletingId); notify.success("Section deleted"); setDeletingId(null); load(); } catch (err) { notify.error(err); } finally { setIsDeleting(false); } };

  const columns = [
    { key: "name", label: "Name", render: (s: SectionResponse) => <span className="font-medium text-gray-900">{s.name}</span> },
    { key: "yearLevel", label: "Year Level" },
    { key: "academicProgramName", label: "Program" },
    { key: "academicPeriodName", label: "Period" },
    { key: "studentCount", label: "Students" },
    { key: "createdAt", label: "Created", render: (s: SectionResponse) => <span className="text-sm text-gray-500">{formatDate(s.createdAt)}</span> },
    { key: "actions", label: "", className: "text-right", render: (s: SectionResponse) => (
      <div className="flex items-center justify-end gap-1">
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); openEdit(s); }} aria-label="Edit section"><Pencil className="h-4 w-4" /></Button>
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(s.id); }} aria-label="Delete section"><Trash2 className="h-4 w-4 text-red-500" /></Button>
      </div>
    )},
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div><h1 className="text-2xl font-bold text-gray-900">Sections</h1><p className="text-sm text-gray-500 mt-1">Manage class sections</p></div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Section</Button>
      </div>
      <Card>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(s) => s.id} isLoading={isLoading} emptyMessage="No sections found." />
        {data && data.totalPages > 1 && <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit Section" : "Add Section"}>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input id="name" label="Section Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} error={errors.name} placeholder="e.g. BSIT-3A" />
          <div className="grid grid-cols-3 gap-4">
            <Input id="yearLevel" label="Year Level" type="number" min="1" max="6" value={form.yearLevel} onChange={(e) => setForm({ ...form, yearLevel: e.target.value })} />
            <Select id="academicProgramId" label="Program" value={form.academicProgramId} onChange={(e) => setForm({ ...form, academicProgramId: e.target.value })} options={programs.map((p) => ({ label: `${p.code} - ${p.name}`, value: p.id }))} placeholder="Select program" error={errors.academicProgramId} />
            <Select id="academicPeriodId" label="Period" value={form.academicPeriodId} onChange={(e) => setForm({ ...form, academicPeriodId: e.target.value })} options={periods.map((p) => ({ label: p.name, value: p.id }))} placeholder="Select period" error={errors.academicPeriodId} />
          </div>
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>
      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Section" message="This will permanently delete this section." isLoading={isDeleting} />
    </div>
  );
}
