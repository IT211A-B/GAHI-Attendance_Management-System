"use client";

import { Plus, Pencil, Trash2 } from "lucide-react";
import { Card, Button, DataTable, Pagination, ConfirmDialog, Modal, Input } from "@/components/ui";
import { useSectionsController } from "@/controllers";
import type { SectionResponse } from "@/types/api";
import { formatDate } from "@/lib/utils";

export default function SectionsPage() {
  const {
    data,
    isLoading,
    page,
    setPage,
    showForm,
    setShowForm,
    editing,
    deletingId,
    setDeletingId,
    isDeleting,
    isSaving,
    form,
    setForm,
    errors,
    openCreate,
    openEdit,
    handleSubmit,
    handleDelete,
  } = useSectionsController();

  const columns = [
    { key: "name", label: "Name", render: (s: SectionResponse) => <span className="font-medium text-gray-900">{s.name}</span> },
    { key: "studentCount", label: "Students", render: () => <span className="text-sm text-gray-400">N/A</span> },
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
