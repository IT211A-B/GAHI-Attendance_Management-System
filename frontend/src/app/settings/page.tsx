"use client";

import { useState } from "react";
import { Card, CardHeader, CardTitle, Button, Input } from "@/components/ui";
import { authService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import { Lock, User } from "lucide-react";
import { notify } from "@/lib/toast";
import { MIN_PASSWORD_LENGTH } from "@/lib/constants";

export default function SettingsPage() {
  const { user } = useAuthStore();
  const [pwForm, setPwForm] = useState({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs: Record<string, string> = {};
    if (!pwForm.currentPassword) errs.currentPassword = "Required";
    if (!pwForm.newPassword) errs.newPassword = "Required";
    if (pwForm.newPassword.length < MIN_PASSWORD_LENGTH) errs.newPassword = `Min ${MIN_PASSWORD_LENGTH} characters`;
    if (pwForm.newPassword !== pwForm.confirmPassword) errs.confirmPassword = "Passwords don't match";
    setErrors(errs); if (Object.keys(errs).length > 0) return;
    setIsSaving(true);
    try {
      const res = await authService.changePassword({ currentPassword: pwForm.currentPassword, newPassword: pwForm.newPassword, confirmPassword: pwForm.confirmPassword });
      if (res.success) { notify.success("Password changed successfully"); setPwForm({ currentPassword: "", newPassword: "", confirmPassword: "" }); }
      else notify.error(res.message ?? "Failed to change password");
    } catch (err) { notify.error(err); }
    finally { setIsSaving(false); }
  };

  return (
    <div className="space-y-6 max-w-2xl">
      <div><h1 className="text-2xl font-bold text-gray-900">Settings</h1><p className="text-sm text-gray-500 mt-1">Manage your account preferences</p></div>

      {/* Profile Info */}
      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><User className="h-5 w-5" /> Profile Information</CardTitle></CardHeader>
        <div className="p-6 pt-0 space-y-3">
          <div className="grid grid-cols-2 gap-4">
            <div><span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Username</span><p className="mt-1 text-sm font-medium text-gray-900">{user?.username ?? "—"}</p></div>
            <div><span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Email</span><p className="mt-1 text-sm font-medium text-gray-900">{user?.email ?? "—"}</p></div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div><span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Full Name</span><p className="mt-1 text-sm font-medium text-gray-900">{user?.fullName ?? "—"}</p></div>
            <div><span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Roles</span><p className="mt-1 text-sm font-medium text-gray-900">{user?.roles?.join(", ") ?? "—"}</p></div>
          </div>
        </div>
      </Card>

      {/* Change Password */}
      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Lock className="h-5 w-5" /> Change Password</CardTitle></CardHeader>
        <form onSubmit={handleChangePassword} className="p-6 pt-0 space-y-4">
          <Input id="currentPassword" label="Current Password" type="password" value={pwForm.currentPassword} onChange={(e) => setPwForm({ ...pwForm, currentPassword: e.target.value })} error={errors.currentPassword} placeholder="Enter current password" />
          <Input id="newPassword" label="New Password" type="password" value={pwForm.newPassword} onChange={(e) => setPwForm({ ...pwForm, newPassword: e.target.value })} error={errors.newPassword} placeholder="Enter new password" />
          <Input id="confirmPassword" label="Confirm New Password" type="password" value={pwForm.confirmPassword} onChange={(e) => setPwForm({ ...pwForm, confirmPassword: e.target.value })} error={errors.confirmPassword} placeholder="Confirm new password" />
          <div className="pt-2"><Button type="submit" isLoading={isSaving}>Update Password</Button></div>
        </form>
      </Card>
    </div>
  );
}
