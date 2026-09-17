import { useState } from "react";
import { Modal } from "../../../components/Modal";
import type { Department } from "../../organization/api/organizationApi";
import { employmentTypes, type RequisitionWrite } from "../api/requisitionsApi";
import { employmentTypeLabels } from "./requisitionStatusBadge";

interface Props {
  open: boolean;
  departments: Department[];
  busy: boolean;
  error?: string;
  onClose: () => void;
  onSubmit: (write: RequisitionWrite) => void;
}

export function NewRequisitionModal({ open, departments, busy, error, onClose, onSubmit }: Props) {
  const [title, setTitle] = useState("");
  const [departmentId, setDepartmentId] = useState<number>();
  const [employmentType, setEmploymentType] = useState<RequisitionWrite["employmentType"]>("full_time");
  const [targetHeadcount, setTargetHeadcount] = useState(1);
  const [salaryMin, setSalaryMin] = useState("");
  const [salaryMax, setSalaryMax] = useState("");
  const [location, setLocation] = useState("");

  const canSubmit = title.trim().length > 0 && departmentId !== undefined && targetHeadcount >= 1;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Tạo Yêu Cầu Tuyển Dụng Mới"
      width="lg"
      footer={
        <>
          <button type="button" onClick={onClose} className="rounded-lg px-4 py-2 text-sm font-medium text-slate-500 hover:bg-slate-100">
            Huỷ
          </button>
          <button
            type="button"
            disabled={!canSubmit || busy}
            onClick={() =>
              departmentId !== undefined &&
              onSubmit({
                title: title.trim(),
                departmentId,
                employmentType,
                targetHeadcount,
                salaryMin: salaryMin ? Number(salaryMin) : undefined,
                salaryMax: salaryMax ? Number(salaryMax) : undefined,
                location: location.trim() || undefined,
              })
            }
            className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {busy ? "Đang tạo…" : "Đăng Tin Tuyển Dụng"}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Chức Danh Tuyển Dụng *</span>
          <input
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>

        <div className="grid grid-cols-2 gap-4">
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Phòng Ban *</span>
            <select
              value={departmentId ?? ""}
              onChange={(event) => setDepartmentId(event.target.value ? Number(event.target.value) : undefined)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
            >
              <option value="">Chọn phòng ban</option>
              {departments.map((department) => (
                <option key={department.id} value={department.id}>
                  {department.name}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Hình Thức Làm Việc *</span>
            <select
              value={employmentType}
              onChange={(event) => setEmploymentType(event.target.value as RequisitionWrite["employmentType"])}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
            >
              {employmentTypes.map((type) => (
                <option key={type} value={type}>
                  {employmentTypeLabels[type]}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Chỉ Tiêu (Headcount) *</span>
            <input
              type="number"
              min={1}
              value={targetHeadcount}
              onChange={(event) => setTargetHeadcount(Number(event.target.value))}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Địa điểm</span>
            <input
              value={location}
              onChange={(event) => setLocation(event.target.value)}
              placeholder="Hà Nội / Hybrid"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Lương Tối Thiểu (VNĐ)</span>
            <input
              type="number"
              value={salaryMin}
              onChange={(event) => setSalaryMin(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 font-mono text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Lương Tối Đa (VNĐ)</span>
            <input
              type="number"
              value={salaryMax}
              onChange={(event) => setSalaryMax(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 font-mono text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
        </div>
      </div>
    </Modal>
  );
}
