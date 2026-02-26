"use client";

import { useState, useEffect } from "react";
import { Modal, Button, Input, Select } from "@/components/ui";
import { studentService, sectionService } from "@/services";
import type {
  StudentResponse,
  CreateStudentRequest,
  UpdateStudentRequest,
  SectionResponse,
} from "@/types/api";
import { notify } from "@/lib/toast";
import { DROPDOWN_PAGE_SIZE } from "@/lib/constants";

interface StudentFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  student: StudentResponse | null;
}

const enrollmentOptions = [
  { label: "Active", value: "Active" },
  { label: "Inactive", value: "Inactive" },
  { label: "Graduated", value: "Graduated" },
  { label: "Dropped Out", value: "DroppedOut" },
  { label: "Suspended", value: "Suspended" },
  { label: "On Leave", value: "OnLeave" },
];

export default function StudentFormModal({
  isOpen,
  onClose,
  onSuccess,
  student,
}: StudentFormModalProps) {
  const isEditing = !!student;
  const [isLoading, setIsLoading] = useState(false);
  const [sections, setSections] = useState<SectionResponse[]>([]);

  const [form, setForm] = useState({
    studentIdNumber: "",
    firstName: "",
    middleName: "",
    lastName: "",
    email: "",
    contactNumber: "",
    sectionId: "",
    enrollmentStatus: "Active",
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      loadSections();
      if (student) {
        setForm({
          studentIdNumber: student.studentIdNumber,
          firstName: student.firstName,
          middleName: student.middleName ?? "",
          lastName: student.lastName,
          email: student.email ?? "",
          contactNumber: student.contactNumber ?? "",
          sectionId: student.sectionId,
          enrollmentStatus: student.enrollmentStatus,
        });
      } else {
        setForm({
          studentIdNumber: "",
          firstName: "",
          middleName: "",
          lastName: "",
          email: "",
          contactNumber: "",
          sectionId: "",
          enrollmentStatus: "Active",
        });
      }
      setErrors({});
    }
  }, [isOpen, student]);

  const loadSections = async () => {
    try {
      const res = await sectionService.getAll({ pageSize: DROPDOWN_PAGE_SIZE });
      if (res.success && res.data) setSections(res.data.items);
    } catch (err) {
      notify.error(err);
    }
  };

  const validate = (): boolean => {
    const errs: Record<string, string> = {};
    if (!form.studentIdNumber.trim()) errs.studentIdNumber = "ID number is required";
    if (!form.firstName.trim()) errs.firstName = "First name is required";
    if (!form.lastName.trim()) errs.lastName = "Last name is required";
    if (!form.sectionId) errs.sectionId = "Section is required";
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setIsLoading(true);
    try {
      if (isEditing) {
        const data: UpdateStudentRequest = {
          firstName: form.firstName,
          middleName: form.middleName || undefined,
          lastName: form.lastName,
          email: form.email || undefined,
          contactNumber: form.contactNumber || undefined,
          sectionId: form.sectionId,
          enrollmentStatus: form.enrollmentStatus as UpdateStudentRequest["enrollmentStatus"],
        };
        await studentService.update(student!.id, data);
        notify.success("Student updated successfully");
      } else {
        const data: CreateStudentRequest = {
          studentIdNumber: form.studentIdNumber,
          firstName: form.firstName,
          middleName: form.middleName || undefined,
          lastName: form.lastName,
          email: form.email || undefined,
          contactNumber: form.contactNumber || undefined,
          sectionId: form.sectionId,
        };
        await studentService.create(data);
        notify.success("Student created successfully");
      }
      onSuccess();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string; errors?: string[] } } };
      const msg = axiosErr.response?.data?.errors?.[0] || axiosErr.response?.data?.message || "Operation failed";
      notify.error(msg);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEditing ? "Edit Student" : "Add Student"}
      size="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input
            id="studentIdNumber"
            label="Student ID Number"
            value={form.studentIdNumber}
            onChange={(e) =>
              setForm({ ...form, studentIdNumber: e.target.value })
            }
            error={errors.studentIdNumber}
            disabled={isEditing}
            placeholder="e.g. 2026-0001"
          />
          <Select
            id="sectionId"
            label="Section"
            value={form.sectionId}
            onChange={(e) => setForm({ ...form, sectionId: e.target.value })}
            options={sections.map((s) => ({
              label: `${s.name} - ${s.academicProgramName}`,
              value: s.id,
            }))}
            placeholder="Select section"
            error={errors.sectionId}
          />
        </div>

        <div className="grid grid-cols-3 gap-4">
          <Input
            id="firstName"
            label="First Name"
            value={form.firstName}
            onChange={(e) => setForm({ ...form, firstName: e.target.value })}
            error={errors.firstName}
            placeholder="First name"
          />
          <Input
            id="middleName"
            label="Middle Name"
            value={form.middleName}
            onChange={(e) => setForm({ ...form, middleName: e.target.value })}
            placeholder="Middle name (optional)"
          />
          <Input
            id="lastName"
            label="Last Name"
            value={form.lastName}
            onChange={(e) => setForm({ ...form, lastName: e.target.value })}
            error={errors.lastName}
            placeholder="Last name"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <Input
            id="email"
            label="Email"
            type="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            placeholder="Email (optional)"
          />
          <Input
            id="contactNumber"
            label="Contact Number"
            value={form.contactNumber}
            onChange={(e) =>
              setForm({ ...form, contactNumber: e.target.value })
            }
            placeholder="Contact number (optional)"
          />
        </div>

        {isEditing && (
          <Select
            id="enrollmentStatus"
            label="Enrollment Status"
            value={form.enrollmentStatus}
            onChange={(e) =>
              setForm({ ...form, enrollmentStatus: e.target.value })
            }
            options={enrollmentOptions}
          />
        )}

        <div className="flex justify-end gap-3 pt-4 border-t">
          <Button variant="outline" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" isLoading={isLoading}>
            {isEditing ? "Update Student" : "Create Student"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
