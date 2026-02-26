"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, Pencil, Trash2, Settings } from "lucide-react";
import { Card, Button, DataTable, Pagination, Modal, Input, Textarea, Badge, ConfirmDialog } from "@/components/ui";
import { businessRuleService } from "@/services";
import type { BusinessRuleResponse, PagedResult } from "@/types/api";
import { formatDateTime } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { LARGE_PAGE_SIZE } from "@/lib/constants";

export default function BusinessRulesPage() {
  const [data, setData] = useState<PagedResult<BusinessRuleResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<BusinessRuleResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [form, setForm] = useState({ ruleKey: "", ruleValue: "", description: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await businessRuleService.getAll({ page, pageSize: LARGE_PAGE_SIZE });
      if (res.success && res.data) setData(res.data);
    } catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => {
    setEditing(null);
    setForm({ ruleKey: "", ruleValue: "", description: "" });
    setErrors({});
    setShowForm(true);
  };

  const openEdit = (r: BusinessRuleResponse) => {
    setEditing(r);
    setForm({ ruleKey: r.ruleKey, ruleValue: r.ruleValue, description: r.description ?? "" });
    setErrors({});
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.ruleKey.trim()) errs.ruleKey = "Required";
    if (!form.ruleValue.trim()) errs.ruleValue = "Required";
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;
    setIsSaving(true);
    try {
      if (editing) {
        await businessRuleService.update(editing.id, { ruleValue: form.ruleValue, description: form.description || undefined });
        notify.success("Rule updated");
      } else {
        await businessRuleService.create({ ruleKey: form.ruleKey, ruleValue: form.ruleValue, description: form.description || undefined });
        notify.success("Rule created");
      }
      setShowForm(false);
      load();
    } catch (err) { notify.error(err); }
    finally { setIsSaving(false); }
  };

  const handleDelete = async () => {
    if (!deletingId) return;
    setIsDeleting(true);
    try { await businessRuleService.delete(deletingId); notify.success("Rule deleted"); setDeletingId(null); load(); }
    catch (err) { notify.error(err); }
    finally { setIsDeleting(false); }
  };

  const columns = [
    { key: "ruleKey", label: "Key", render: (r: BusinessRuleResponse) => (
      <div className="flex items-center gap-2">
        <Settings className="h-4 w-4 text-gray-400" />
        <span className="font-mono text-sm font-medium text-gray-900">{r.ruleKey}</span>
      </div>
    )},
    { key: "ruleValue", label: "Value", render: (r: BusinessRuleResponse) => <Badge variant="info">{r.ruleValue}</Badge> },
    { key: "description", label: "Description", render: (r: BusinessRuleResponse) => <span className="text-sm text-gray-500">{r.description ?? "—"}</span> },
    { key: "departmentName", label: "Department", render: (r: BusinessRuleResponse) => <span className="text-sm text-gray-500">{r.departmentName ?? "Global"}</span> },
    { key: "updatedAt", label: "Last Updated", render: (r: BusinessRuleResponse) => <span className="text-sm text-gray-500">{r.updatedAt ? formatDateTime(r.updatedAt) : "—"}</span> },
    { key: "actions", label: "", className: "text-right", render: (r: BusinessRuleResponse) => (
      <div className="flex items-center justify-end gap-1">
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); openEdit(r); }} aria-label="Edit rule"><Pencil className="h-4 w-4" /></Button>
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(r.id); }} aria-label="Delete rule"><Trash2 className="h-4 w-4 text-red-500" /></Button>
      </div>
    )},
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Business Rules</h1>
          <p className="text-sm text-gray-500 mt-1">Configure system-wide parameters</p>
        </div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add Rule</Button>
      </div>

      <Card>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(r) => r.id} isLoading={isLoading} emptyMessage="No business rules configured." />
        {data && data.totalPages > 1 && (
          <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />
        )}
      </Card>

      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit Rule" : "Add Rule"}>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input id="ruleKey" label="Rule Key" value={form.ruleKey} onChange={(e) => setForm({ ...form, ruleKey: e.target.value })} error={errors.ruleKey} placeholder="e.g. LATE_THRESHOLD_MINUTES" disabled={!!editing} />
          <Input id="ruleValue" label="Value" value={form.ruleValue} onChange={(e) => setForm({ ...form, ruleValue: e.target.value })} error={errors.ruleValue} placeholder="Enter value" />
          <Textarea id="description" label="Description" rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="Optional description" />
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Rule" message="This will permanently delete this business rule." isLoading={isDeleting} />
    </div>
  );
}
