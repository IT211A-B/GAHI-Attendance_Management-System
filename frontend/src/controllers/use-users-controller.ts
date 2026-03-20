"use client";

import { useCallback, useEffect, useState } from "react";
import { userService } from "@/services";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";
import type { UserResponse, PagedResult } from "@/types/api";

type UserFormState = {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
};

const emptyForm = (): UserFormState => ({
  username: "",
  email: "",
  password: "",
  firstName: "",
  lastName: "",
  isActive: true,
});

export function useUsersController() {
  const [data, setData] = useState<PagedResult<UserResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<UserResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [form, setForm] = useState<UserFormState>(emptyForm());
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await userService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE });
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

  const openEdit = useCallback((u: UserResponse) => {
    setEditing(u);
    setForm({
      username: u.username,
      email: u.email,
      password: "",
      firstName: u.firstName,
      lastName: u.lastName,
      isActive: u.isActive,
    });
    setErrors({});
    setShowForm(true);
  }, []);

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!editing && !form.username.trim()) errs.username = "Required";
    if (!form.email.trim()) errs.email = "Required";
    if (!editing && !form.password.trim()) errs.password = "Required";
    if (!form.firstName.trim()) errs.firstName = "Required";
    if (!form.lastName.trim()) errs.lastName = "Required";
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setIsSaving(true);
    try {
      if (editing) {
        await userService.update(editing.id, {
          email: form.email,
          firstName: form.firstName,
          lastName: form.lastName,
          isActive: form.isActive,
        });
        notify.success("User updated");
      } else {
        await userService.create({
          username: form.username,
          email: form.email,
          password: form.password,
          firstName: form.firstName,
          lastName: form.lastName,
          roleIds: [],
        });
        notify.success("User created");
      }
      setShowForm(false);
      load();
    } catch (err) {
      notify.error(err);
    } finally {
      setIsSaving(false);
    }
  }, [editing, form, load]);

  const handleDelete = useCallback(async () => {
    if (!deletingId) return;
    setIsDeleting(true);
    try {
      await userService.delete(deletingId);
      notify.success("User deleted");
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
