"use client";

import { useCallback, useEffect, useState } from "react";
import { sectionService } from "@/services";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";
import type { SectionResponse, PagedResult } from "@/types/api";

type SectionFormState = {
  name: string;
};

const emptyForm = (): SectionFormState => ({ name: "" });

export function useSectionsController() {
  const [data, setData] = useState<PagedResult<SectionResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<SectionResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [form, setForm] = useState<SectionFormState>(emptyForm());
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await sectionService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE });
      if (res.success && res.data) setData(res.data);
    } catch (err) {
      notify.error(err);
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

  const openEdit = useCallback((s: SectionResponse) => {
    setEditing(s);
    setForm({ name: s.name });
    setErrors({});
    setShowForm(true);
  }, []);

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!form.name.trim()) errs.name = "Required";
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setIsSaving(true);
    try {
      const payload = { name: form.name };
      if (editing) {
        await sectionService.update(editing.id, payload);
        notify.success("Section updated");
      } else {
        await sectionService.create(payload);
        notify.success("Section created");
      }
      setShowForm(false);
      load();
    } catch (err) {
      notify.error(err);
    } finally {
      setIsSaving(false);
    }
  }, [editing, form.name, load]);

  const handleDelete = useCallback(async () => {
    if (!deletingId) return;
    setIsDeleting(true);
    try {
      await sectionService.delete(deletingId);
      notify.success("Section deleted");
      setDeletingId(null);
      load();
    } catch (err) {
      notify.error(err);
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
