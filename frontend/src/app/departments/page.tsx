"use client";

import { useEffect, useState, useCallback, useRef } from "react";
import { Plus, Pencil, Trash2 } from "lucide-react";
import { animate, stagger } from "animejs";
import {
  Card,
  Button,
  DataTable,
  Pagination,
  ConfirmDialog,
  Modal,
  Input,
} from "@/components/ui";
import { useClassroomsController } from "@/controllers";
import type { DepartmentResponse } from "@/types/api";
import { formatDate } from "@/lib/utils";

export default function DepartmentsPage() {
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
  } = useClassroomsController();
  const pageRef = useRef<HTMLDivElement>(null);
  const headerRef = useRef<HTMLDivElement>(null);
  const cardRef = useRef<HTMLDivElement>(null);
  const createBtnRef = useRef<HTMLDivElement>(null);
  const animsRef = useRef<ReturnType<typeof animate>[]>([]);
  const hoverAnimRef = useRef<ReturnType<typeof animate> | null>(null);

  useEffect(() => {
    if (!pageRef.current || !headerRef.current || !cardRef.current) return;

    animsRef.current.forEach((a) => a.pause());
    animsRef.current = [];

    animsRef.current.push(
      animate(headerRef.current, {
        opacity: [0, 1],
        translateY: [-14, 0],
        duration: 600,
        ease: "outExpo",
      })
    );

    animsRef.current.push(
      animate(cardRef.current, {
        opacity: [0, 1],
        translateY: [20, 0],
        delay: 120,
        duration: 700,
        ease: "outExpo",
      })
    );

    return () => {
      animsRef.current.forEach((a) => a.pause());
      animsRef.current = [];
    };
  }, []);

  useEffect(() => {
    if (isLoading || !cardRef.current) return;
    const rows = cardRef.current.querySelectorAll("tbody tr");
    if (!rows.length) return;

    const rowAnim = animate(rows, {
      opacity: [0, 1],
      translateX: [-10, 0],
      delay: stagger(45),
      duration: 420,
      ease: "outQuad",
    });
    animsRef.current.push(rowAnim);
    return () => {
      rowAnim.pause();
    };
  }, [isLoading, data?.items]);

  const handleCreateHover = (enter: boolean) => {
    if (!createBtnRef.current) return;
    if (hoverAnimRef.current) hoverAnimRef.current.pause();
    hoverAnimRef.current = animate(createBtnRef.current, {
      scale: enter ? [1, 1.04] : [1.04, 1],
      duration: enter ? 180 : 220,
      ease: enter ? "outBack(2)" : "outQuad",
    });
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
    <div ref={pageRef} className="space-y-6">
      <div ref={headerRef} className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Classrooms</h1>
          <p className="text-sm text-gray-500 mt-1">Manage classrooms and room numbers</p>
        </div>
        <div ref={createBtnRef} className="inline-block">
          <Button
            onClick={openCreate}
            onMouseEnter={() => handleCreateHover(true)}
            onMouseLeave={() => handleCreateHover(false)}
          >
            <Plus className="h-4 w-4" /> Add Classroom
          </Button>
        </div>
      </div>

      <div ref={cardRef}>
        <Card>
          <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(d) => d.id} isLoading={isLoading} emptyMessage="No classrooms found." />
          {data && data.totalPages > 1 && (
            <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />
          )}
        </Card>
      </div>

      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit Classroom" : "Add Classroom"}>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input id="name" label="Classroom Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} error={errors.name} placeholder="Classroom name" />
          <Input id="code" label="Room Number" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} error={errors.code} placeholder="e.g. R-204" />
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete Classroom" message="This will permanently delete this classroom." isLoading={isDeleting} />
    </div>
  );
}
