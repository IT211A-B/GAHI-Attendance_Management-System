"use client";

import { useState, useEffect } from "react";
import { Modal, Button, Input, Select } from "@/components/ui";
import { staffService, departmentService } from "@/services";
import type {
  StaffResponse,
  CreateStaffRequest,
  UpdateStaffRequest,
  DepartmentResponse,
  StaffType,
} from "@/types/api";
import { notify } from "@/lib/toast";
import { DROPDOWN_PAGE_SIZE } from "@/lib/constants";

interface StaffFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  staff: StaffResponse | null;
}

const staffTypeOptions = [
  { label: "Teaching", value: "Teaching" },
  { label: "Non-Teaching", value: "NonTeaching" },
  { label: "Security", value: "Security" },
  { label: "Administrative", value: "Administrative" },
];

export default function StaffFormModal({
  isOpen,
  onClose,
  onSuccess,
  staff,
}: StaffFormModalProps) {
  const isEditing = !!staff;
  const [isLoading, setIsLoading] = useState(false);
  const [departments, setDepartments] = useState<DepartmentResponse[]>([]);

  const [form, setForm] = useState({
    employeeIdNumber: "",
    firstName: "",
    middleName: "",
    lastName: "",
    email: "",
    contactNumber: "",
    staffType: "Teaching" as string,
    departmentId: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      loadDepartments();
      if (staff) {
        setForm({
          employeeIdNumber: staff.employeeIdNumber,
          firstName: staff.firstName,
          middleName: staff.middleName ?? "",
          lastName: staff.lastName,
          email: staff.email ?? "",
          contactNumber: staff.contactNumber ?? "",
          staffType: staff.staffType,
          departmentId: staff.departmentId,
        });
      } else {
        setForm({
          employeeIdNumber: "",
          firstName: "",
          middleName: "",
          lastName: "",
          email: "",
          contactNumber: "",
          staffType: "Teaching",
          departmentId: "",
        });
      }
      setErrors({});
    }
  }, [isOpen, staff]);

  const loadDepartments = async () => {
    try {
      const res = await departmentService.getAll({ pageSize: DROPDOWN_PAGE_SIZE });
      if (res.success && res.data) setDepartments(res.data.items);
    } catch (err) {
      notify.error(err);
    }
  };

  const validate = (): boolean => {
    const errs: Record<string, string> = {};
    if (!form.employeeIdNumber.trim()) errs.employeeIdNumber = "Employee ID is required";
    if (!form.firstName.trim()) errs.firstName = "First name is required";
    if (!form.lastName.trim()) errs.lastName = "Last name is required";
    if (!form.departmentId) errs.departmentId = "Department is required";
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    setIsLoading(true);
    try {
      if (isEditing) {
        const data: UpdateStaffRequest = {
          firstName: form.firstName,
          middleName: form.middleName || undefined,
          lastName: form.lastName,
          email: form.email || undefined,
          contactNumber: form.contactNumber || undefined,
          staffType: form.staffType as StaffType,
          departmentId: form.departmentId,
        };
        await staffService.update(staff!.id, data);
        notify.success("Staff updated successfully");
      } else {
        const data: CreateStaffRequest = {
          employeeIdNumber: form.employeeIdNumber,
          firstName: form.firstName,
          middleName: form.middleName || undefined,
          lastName: form.lastName,
          email: form.email || undefined,
          contactNumber: form.contactNumber || undefined,
          staffType: form.staffType as StaffType,
          departmentId: form.departmentId,
        };
        await staffService.create(data);
        notify.success("Staff created successfully");
      }
      onSuccess();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string; errors?: string[] } } };
      notify.error(axiosErr.response?.data?.errors?.[0] || axiosErr.response?.data?.message || "Operation failed");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={isEditing ? "Edit Staff" : "Add Staff"} size="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input id="employeeIdNumber" label="Employee ID" value={form.employeeIdNumber} onChange={(e) => setForm({ ...form, employeeIdNumber: e.target.value })} error={errors.employeeIdNumber} disabled={isEditing} placeholder="e.g. EMP-0001" />
          <Select id="departmentId" label="Department" value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value })} options={departments.map((d) => ({ label: d.name, value: d.id }))} placeholder="Select department" error={errors.departmentId} />
        </div>

        <div className="grid grid-cols-3 gap-4">
          <Input id="firstName" label="First Name" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} error={errors.firstName} placeholder="First name" />
          <Input id="middleName" label="Middle Name" value={form.middleName} onChange={(e) => setForm({ ...form, middleName: e.target.value })} placeholder="(optional)" />
          <Input id="lastName" label="Last Name" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} error={errors.lastName} placeholder="Last name" />
        </div>

        <div className="grid grid-cols-3 gap-4">
          <Input id="email" label="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="(optional)" />
          <Input id="contactNumber" label="Contact Number" value={form.contactNumber} onChange={(e) => setForm({ ...form, contactNumber: e.target.value })} placeholder="(optional)" />
          <Select id="staffType" label="Staff Type" value={form.staffType} onChange={(e) => setForm({ ...form, staffType: e.target.value })} options={staffTypeOptions} />
        </div>

        <div className="flex justify-end gap-3 pt-4 border-t">
          <Button variant="outline" type="button" onClick={onClose}>Cancel</Button>
          <Button type="submit" isLoading={isLoading}>{isEditing ? "Update Staff" : "Create Staff"}</Button>
        </div>
      </form>
    </Modal>
  );
}
