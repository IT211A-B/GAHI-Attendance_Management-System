"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, Pencil, Trash2 } from "lucide-react";
import { Card, Button, DataTable, Pagination, ConfirmDialog, Modal, Input, Checkbox, Badge } from "@/components/ui";
import { userService } from "@/services";
import type { UserResponse, PagedResult } from "@/types/api";
import { formatDate, formatDateTime } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";

export default function UsersPage() {
  const [data, setData] = useState<PagedResult<UserResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<UserResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [form, setForm] = useState({ username: "", email: "", password: "", firstName: "", lastName: "", isActive: true });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try { const res = await userService.getAll({ page, pageSize: DEFAULT_PAGE_SIZE }); if (res.success && res.data) setData(res.data); }
    catch (err) { notify.error(err); }
    finally { setIsLoading(false); }
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => { setEditing(null); setForm({ username: "", email: "", password: "", firstName: "", lastName: "", isActive: true }); setErrors({}); setShowForm(true); };
  const openEdit = (u: UserResponse) => { setEditing(u); setForm({ username: u.username, email: u.email, password: "", firstName: u.firstName, lastName: u.lastName, isActive: u.isActive }); setErrors({}); setShowForm(true); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!editing && !form.username.trim()) errs.username = "Required";
    if (!form.email.trim()) errs.email = "Required";
    if (!editing && !form.password.trim()) errs.password = "Required";
    if (!form.firstName.trim()) errs.firstName = "Required";
    if (!form.lastName.trim()) errs.lastName = "Required";
    setErrors(errs); if (Object.keys(errs).length > 0) return;
    setIsSaving(true);
    try {
      if (editing) {
        await userService.update(editing.id, { email: form.email, firstName: form.firstName, lastName: form.lastName, isActive: form.isActive });
        notify.success("User updated");
      } else {
        await userService.create({ username: form.username, email: form.email, password: form.password, firstName: form.firstName, lastName: form.lastName, roleIds: [] });
        notify.success("User created");
      }
      setShowForm(false); load();
    } catch (err) { notify.error(err); }
    finally { setIsSaving(false); }
  };

  const handleDelete = async () => { if (!deletingId) return; setIsDeleting(true); try { await userService.delete(deletingId); notify.success("User deleted"); setDeletingId(null); load(); } catch (err) { notify.error(err); } finally { setIsDeleting(false); } };

  const columns = [
    { key: "username", label: "Username", render: (u: UserResponse) => <span className="font-mono font-medium">{u.username}</span> },
    { key: "name", label: "Name", render: (u: UserResponse) => <div><p className="font-medium text-gray-900">{u.firstName} {u.lastName}</p><p className="text-xs text-gray-500">{u.email}</p></div> },
    { key: "roles", label: "Roles", render: (u: UserResponse) => <div className="flex gap-1 flex-wrap">{u.roles.map((r) => <Badge key={r} variant="info">{r}</Badge>)}</div> },
    { key: "isActive", label: "Status", render: (u: UserResponse) => <Badge variant={u.isActive ? "success" : "danger"}>{u.isActive ? "Active" : "Inactive"}</Badge> },
    { key: "lastLoginAt", label: "Last Login", render: (u: UserResponse) => <span className="text-sm text-gray-500">{u.lastLoginAt ? formatDateTime(u.lastLoginAt) : "Never"}</span> },
    { key: "createdAt", label: "Created", render: (u: UserResponse) => <span className="text-sm text-gray-500">{formatDate(u.createdAt)}</span> },
    { key: "actions", label: "", className: "text-right", render: (u: UserResponse) => (
      <div className="flex items-center justify-end gap-1">
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); openEdit(u); }} aria-label="Edit user"><Pencil className="h-4 w-4" /></Button>
        <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setDeletingId(u.id); }} aria-label="Delete user"><Trash2 className="h-4 w-4 text-red-500" /></Button>
      </div>
    )},
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div><h1 className="text-2xl font-bold text-gray-900">Users</h1><p className="text-sm text-gray-500 mt-1">Manage system users and roles</p></div>
        <Button onClick={openCreate}><Plus className="h-4 w-4" /> Add User</Button>
      </div>
      <Card>
        <DataTable columns={columns} data={data?.items ?? []} keyExtractor={(u) => u.id} isLoading={isLoading} emptyMessage="No users found." />
        {data && data.totalPages > 1 && <Pagination page={data.page} totalPages={data.totalPages} hasPrevious={data.hasPrevious} hasNext={data.hasNext} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
      <Modal isOpen={showForm} onClose={() => setShowForm(false)} title={editing ? "Edit User" : "Add User"} size="lg">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <Input id="username" label="Username" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} error={errors.username} disabled={!!editing} placeholder="Username" />
            <Input id="email" label="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} error={errors.email} placeholder="Email address" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <Input id="firstName" label="First Name" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} error={errors.firstName} placeholder="First name" />
            <Input id="lastName" label="Last Name" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} error={errors.lastName} placeholder="Last name" />
          </div>
          {!editing && <Input id="password" label="Password" type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} error={errors.password} placeholder="Password" />}
          {editing && (
            <Checkbox id="isActive" label="Active account" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
          )}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" type="button" onClick={() => setShowForm(false)}>Cancel</Button>
            <Button type="submit" isLoading={isSaving}>{editing ? "Update" : "Create"}</Button>
          </div>
        </form>
      </Modal>
      <ConfirmDialog isOpen={!!deletingId} onClose={() => setDeletingId(null)} onConfirm={handleDelete} title="Delete User" message="This will permanently delete this user." isLoading={isDeleting} />
    </div>
  );
}
