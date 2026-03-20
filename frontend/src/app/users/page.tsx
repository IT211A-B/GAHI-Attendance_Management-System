"use client";

import { Plus, Pencil, Trash2 } from "lucide-react";
import { Card, Button, DataTable, Pagination, ConfirmDialog, Modal, Input, Checkbox, Badge } from "@/components/ui";
import { useUsersController } from "@/controllers";
import type { UserResponse } from "@/types/api";
import { formatDate, formatDateTime } from "@/lib/utils";

export default function UsersPage() {
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
  } = useUsersController();

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
