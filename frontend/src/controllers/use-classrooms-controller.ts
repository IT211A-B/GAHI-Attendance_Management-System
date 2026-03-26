"use client";

import { useCallback, useEffect, useState } from "react";
import { departmentService } from "@/services";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";
import type { DepartmentResponse, PagedResult } from "@/types/api";

type ClassroomFormState = {
  name: string;
  code: string;
  description: string;
};

const emptyForm = (): ClassroomFormState => ({
  name: "",
  code: "",
  description: "",
});

export function useClassroomsController() {
  const [data, setData] = useState<PagedResult<DepartmentResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<DepartmentResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [form, setForm] = useState<ClassroomFormState>(emptyForm());
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

  useEffect(() => {
    load();
  }, [load]);

  const openCreate = useCallback(() => {
    setEditing(null);
    setForm(emptyForm());
    setErrors({});
    setShowForm(true);
  }, []);

  const openEdit = useCallback((d: DepartmentResponse) => {
    setEditing(d);
    setForm({ name: d.name, code: d.code, description: d.description ?? "" });
    setErrors({});
    setShowForm(true);
  }, []);

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
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
  }, [editing, form, load]);

  const handleDelete = useCallback(async () => {
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
  }, [deletingId, load]);

  return {
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
  };
}
