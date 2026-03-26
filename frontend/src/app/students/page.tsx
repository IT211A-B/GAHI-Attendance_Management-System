"use client";

import { useEffect, useState, useCallback } from "react";
import { Plus, QrCode, Pencil, Trash2 } from "lucide-react";
import {
  Card,
  Button,
  DataTable,
  Pagination,
  SearchBar,
  Badge,
  ConfirmDialog,
} from "@/components/ui";
import { studentService } from "@/services";
import type { StudentResponse, PagedResult } from "@/types/api";
import { formatDate } from "@/lib/utils";
import { notify } from "@/lib/toast";
import { DEFAULT_PAGE_SIZE } from "@/lib/constants";
import StudentFormModal from "./_components/student-form-modal";

export default function StudentsPage() {
  const [data, setData] = useState<PagedResult<StudentResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editingStudent, setEditingStudent] = useState<StudentResponse | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const loadStudents = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await studentService.getAll({
        page,
        pageSize: DEFAULT_PAGE_SIZE,
        search: search || undefined,
      });
      if (res.success && res.data) setData(res.data);
    } catch {
      notify.error("Failed to load students");
    } finally {
      setIsLoading(false);
    }
  }, [page, search]);

  useEffect(() => {
    loadStudents();
  }, [loadStudents]);

  const handleSearch = useCallback((query: string) => {
    setSearch(query);
    setPage(1);
  }, []);

  const handleDelete = async () => {
    if (!deletingId) return;
    setIsDeleting(true);
    try {
      await studentService.delete(deletingId);
      notify.success("Student deleted successfully");
      setDeletingId(null);
      loadStudents();
    } catch {
      notify.error("Failed to delete student");
    } finally {
      setIsDeleting(false);
    }
  };

  const handleRegenerateQr = async (id: string) => {
    try {
      await studentService.regenerateQr(id);
      notify.success("QR code regenerated");
      loadStudents();
    } catch {
      notify.error("Failed to regenerate QR code");
    }
  };

  const columns = [
    {
      key: "studentIdNumber",
      label: "ID Number",
      render: (s: StudentResponse) => (
        <span className="font-mono text-sm font-medium">{s.studentIdNumber}</span>
      ),
    },
    {
      key: "fullName",
      label: "Name",
      render: (s: StudentResponse) => (
        <div>
          <p className="font-medium text-gray-900">{s.fullName}</p>
          {s.email && <p className="text-xs text-gray-500">{s.email}</p>}
        </div>
      ),
    },
    {
      key: "sectionName",
      label: "Section",
      render: (s: StudentResponse) => (
        <div>
          <p className="text-sm text-gray-900">{s.sectionName}</p>
          <p className="text-xs text-gray-500">{s.academicProgramName}</p>
        </div>
      ),
    },
    {
      key: "departmentName",
      label: "Department",
    },
    {
      key: "enrollmentStatus",
      label: "Status",
      render: (s: StudentResponse) => (
        <Badge
          variant={
            s.enrollmentStatus === "Active"
              ? "success"
              : s.enrollmentStatus === "Inactive"
              ? "default"
              : "warning"
          }
        >
          {s.enrollmentStatus}
        </Badge>
      ),
    },
    {
      key: "createdAt",
      label: "Created",
      render: (s: StudentResponse) => (
        <span className="text-sm text-gray-500">{formatDate(s.createdAt)}</span>
      ),
    },
    {
      key: "actions",
      label: "",
      className: "text-right",
      render: (s: StudentResponse) => (
        <div className="flex items-center justify-end gap-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              handleRegenerateQr(s.id);
            }}
            aria-label="Regenerate QR code"
          >
            <QrCode className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              setEditingStudent(s);
              setShowForm(true);
            }}
            aria-label="Edit student"
          >
            <Pencil className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              setDeletingId(s.id);
            }}
            aria-label="Delete student"
          >
            <Trash2 className="h-4 w-4 text-red-500" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Students</h1>
          <p className="text-sm text-gray-500 mt-1">
            Manage student records and QR codes
          </p>
        </div>
        <Button
          onClick={() => {
            setEditingStudent(null);
            setShowForm(true);
          }}
        >
          <Plus className="h-4 w-4" />
          Add Student
        </Button>
      </div>

      {/* Search & Table */}
      <Card>
        <div className="mb-4">
          <SearchBar
            placeholder="Search by name or ID number..."
            onSearch={handleSearch}
            className="max-w-sm"
          />
        </div>

        <DataTable
          columns={columns}
          data={data?.items ?? []}
          keyExtractor={(s) => s.id}
          isLoading={isLoading}
          emptyMessage="No students found."
        />

        {data && data.totalPages > 1 && (
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hasPrevious={data.hasPrevious}
            hasNext={data.hasNext}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onPageChange={setPage}
          />
        )}
      </Card>

      {/* Form Modal */}
      <StudentFormModal
        isOpen={showForm}
        onClose={() => {
          setShowForm(false);
          setEditingStudent(null);
        }}
        onSuccess={() => {
          setShowForm(false);
          setEditingStudent(null);
          loadStudents();
        }}
        student={editingStudent}
      />

      {/* Delete Confirm */}
      <ConfirmDialog
        isOpen={!!deletingId}
        onClose={() => setDeletingId(null)}
        onConfirm={handleDelete}
        title="Delete Student"
        message="Are you sure you want to delete this student? This action cannot be undone."
        isLoading={isDeleting}
      />
    </div>
  );
}
