"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, Pencil, Trash2 } from "lucide-react";
import { Card, Button, DataTable, Pagination, ConfirmDialog, Modal, Input, Select, Checkbox, Badge } from "@/components/ui";
import { gateTerminalService } from "@/services";
import type { GateTerminalResponse, PagedResult, TerminalType } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";

const terminalTypeOptions = [
  { label: "QR Scanner", value: "QRScanner" },
  { label: "Manual", value: "Manual" },
];

export default function GateTerminalsPage() {
  const [data, setData] = useState<PagedResult<GateTerminalResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<GateTerminalResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [form, setForm] = useState({ name: "", location: "", terminalType: "QRScanner", isActive: true });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try { const res = await gateTerminalService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE }); if (res.success && res.data) setData(res.data); }
    catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => { setEditing(null); setForm({ name: "", location: "", terminalType: "QRScanner", isActive: true }); setErrors({}); setShowForm(true); };
  const openEdit = (t: GateTerminalResponse) => { setEditing(t); setForm({ name: t.name, location: t.location, terminalType: t.terminalType, isActive: t.isActive }); setErrors({}); setShowForm(true); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.name.trim()) errs.name = "Required";
    if (!form.location.trim()) errs.location = "Required";
    setErrors(errs); if (Object.keys(errs).length > 0) return;
    setIsSaving(true);
    try {
      if (editing) { await gateTerminalService.update(editing.id, { ...form, terminalType: form.terminalType as TerminalType }); notify.success("Terminal updated"); }
      else { await gateTerminalService.create({ name: form.name, location: form.location, terminalType: form.terminalType as TerminalType }); notify.success("Terminal created"); }
      setShowForm(false); load();
    } catch (err) { notify.error(err); }
    finally { setIsSaving(false); }
  };

  const handleDelete = async () => { if (!deletingId) return; setIsDeleting(true); try { await gateTerminalService.delete(deletingId); notify.success("Terminal deleted"); setDeletingId(null); load(); } catch (err) { notify.error(err); } finally { setIsDeleting(false); } };

  const columns = [
    { key: "name", label: "Name", render: (t: GateTerminalResponse) => <span className="font-medium text-gray-900">{t.name}</span> },
    { key: "location", label: "Location" },
    { key: "terminalType", label: "Type", render: (t: GateTerminalResponse) => <Badge variant="info">{t.terminalType === "QRScanner" ? "QR Scanner" : "Manual"}</Badge> },
    { key: "isActive", label: "Status", render: (t: GateTerminalResponse) => <Badge variant={t.isActive ? "success" : "danger"}>{t.isActive ? "Active" : "Inactive"}</Badge> },
    { key: "createdAt", label: "Created", render: (t: GateTerminalResponse) => <span className="text-sm text-gray-500">{formatDate(t.createdAt)}</span> },
    { key: "actions", label: "", className: "text-right", render: (t: GateTerminalResponse) => (
      <div className="flex items-center justify-end gap-1">
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); openEdit(t); }} aria-label="Edit terminal"><Pencil className="h-4 w-4" /></Button>
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(t.id); }} aria-label="Delete terminal"><Trash2 className="h-4 w-4 text-red-500" /></Button>
      </div>
    )},
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div><h1 className="text-2xl font-bold text-gray-900">Gate Terminals</h1><p className="text-sm text-gray-500 mt-1">Manage scanning terminals</p></div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Terminal</Button>
      </div>
      <Card>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(t) => t.id} isLoading={isLoading} emptyMessage="No terminals found." />
        {data && data.totalPages > 1 && <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit Terminal" : "Add Terminal"}>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input id="name" label="Terminal Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} error={errors.name} placeholder="e.g. Main Gate Scanner" />
          <Input id="location" label="Location" value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} error={errors.location} placeholder="e.g. Building A Entrance" />
          <Select id="terminalType" label="Terminal Type" value={form.terminalType} onChange={(e) => setForm({ ...form, terminalType: e.target.value })} options={terminalTypeOptions} />
          {editing && (
            <Checkbox id="isActive" label="Active" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
          )}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>
      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Terminal" message="This will permanently delete this terminal." isLoading={isDeleting} />
    </div>
  );
}
