"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { tenantApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { TenantResponse } from "@/types";

// ── Constants ─────────────────────────────────────────────────────────────────

const PLAN_OPTIONS = ["Starter", "Clinic", "Hospital", "Enterprise"];

// ── Form types ────────────────────────────────────────────────────────────────

interface TenantFormValues {
  nameAr:         string;
  name:           string;
  adminEmail:     string;
  planType:       string;
  casesAllocated: number;
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function AdminPage() {
  const router = useRouter();
  const { isLoggedIn, role, tenantId } = useAuthStore();

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    if (role && role !== "SuperAdmin" && role !== "HospitalAdmin") {
      router.push("/dashboard"); return;
    }
    // HospitalAdmin goes directly to their tenant page
    if (role === "HospitalAdmin" && tenantId) {
      router.push(`/admin/tenants/${tenantId}`);
    }
  }, [isLoggedIn, role, tenantId, router]);

  if (!isLoggedIn) return null;
  if (role && role !== "SuperAdmin" && role !== "HospitalAdmin") return null;
  // HospitalAdmin will be redirected — show nothing while redirect fires
  if (role === "HospitalAdmin") return null;

  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />
      <main className="flex-1 max-w-4xl w-full mx-auto px-6 py-8">
        <h1 className="text-xl font-bold text-gray-900 mb-6">لوحة الإدارة</h1>
        <TenantsCard />
      </main>
    </div>
  );
}

// ── Tenants Card ──────────────────────────────────────────────────────────────

function TenantsCard() {
  const [tenants,    setTenants]    = useState<TenantResponse[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState<string | null>(null);
  const [showForm,   setShowForm]   = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formError,  setFormError]  = useState<string | null>(null);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<TenantFormValues>({
    defaultValues: { planType: "Clinic", casesAllocated: 100 },
  });

  const fetchTenants = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await tenantApi.getTenants();
      if (res.success && res.data) setTenants(res.data);
      else setError(res.error ?? "تعذر تحميل المؤسسات");
    } catch {
      setError("خطأ في الاتصال");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchTenants(); }, [fetchTenants]);

  const onSubmit = async (values: TenantFormValues) => {
    setFormError(null);
    setSubmitting(true);
    try {
      const slug = values.name.toLowerCase().replace(/\s+/g, "-").replace(/[^a-z0-9-]/g, "");
      const res = await tenantApi.createTenant({
        name:           values.name,
        nameAr:         values.nameAr,
        slug,
        adminEmail:     values.adminEmail,
        planType:       values.planType,
        casesAllocated: Number(values.casesAllocated),
      });
      if (res.success) {
        await fetchTenants();
        setShowForm(false);
        reset();
      } else {
        setFormError(res.error ?? "حدث خطأ أثناء الإنشاء");
      }
    } catch {
      setFormError("خطأ في الاتصال");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="المؤسسات">
      {loading ? (
        <div className="py-8 text-center text-gray-400 text-sm">جاري التحميل...</div>
      ) : error ? (
        <div className="py-6 text-center">
          <p className="text-red-500 text-sm mb-2">{error}</p>
          <button onClick={fetchTenants} className="text-brand-600 text-sm hover:underline">إعادة المحاولة</button>
        </div>
      ) : tenants.length === 0 && !showForm ? (
        <p className="text-gray-400 text-sm py-4">لا توجد مؤسسات مسجلة</p>
      ) : (
        <div className="space-y-2 mb-4">
          {tenants.map((t) => <TenantRow key={t.tenantId} tenant={t} />)}
        </div>
      )}

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-4 border-t border-gray-100 pt-5 space-y-4" noValidate>
          <h3 className="text-sm font-semibold text-gray-800">إضافة مؤسسة جديدة</h3>

          <div className="grid grid-cols-2 gap-4">
            <F label="اسم المؤسسة بالعربية" req err={errors.nameAr?.message}>
              <input
                className={inp(!!errors.nameAr)}
                placeholder="مثال: مستشفى صنعاء العام"
                {...register("nameAr", { required: "مطلوب" })}
              />
            </F>
            <F label="اسم المؤسسة بالإنجليزية" req err={errors.name?.message}>
              <input
                className={inp(!!errors.name)}
                placeholder="e.g. Sanaa General Hospital"
                {...register("name", { required: "مطلوب" })}
              />
            </F>
          </div>

          <F label="البريد الإلكتروني للمدير" req err={errors.adminEmail?.message}>
            <input
              type="email"
              className={inp(!!errors.adminEmail)}
              placeholder="admin@hospital.ye"
              {...register("adminEmail", {
                required: "مطلوب",
                pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: "بريد غير صالح" },
              })}
            />
          </F>

          <div className="grid grid-cols-2 gap-4">
            <F label="نوع الخطة" req err={errors.planType?.message}>
              <select className={inp(!!errors.planType)} {...register("planType", { required: "مطلوب" })}>
                {PLAN_OPTIONS.map((p) => <option key={p} value={p}>{p}</option>)}
              </select>
            </F>
            <F label="عدد الحالات المخصصة" req err={errors.casesAllocated?.message}>
              <input
                type="number"
                min={1}
                className={inp(!!errors.casesAllocated)}
                {...register("casesAllocated", {
                  required:      "مطلوب",
                  valueAsNumber: true,
                  min: { value: 1, message: "يجب أن يكون 1 على الأقل" },
                })}
              />
            </F>
          </div>

          {formError && (
            <div className="px-4 py-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
              {formError}
            </div>
          )}

          <div className="flex gap-3">
            <button
              type="submit"
              disabled={submitting}
              className="px-5 py-2 rounded-xl bg-brand-600 text-white text-sm font-medium hover:bg-brand-800 transition disabled:opacity-60"
            >
              {submitting ? "جارٍ الإنشاء..." : "إنشاء المؤسسة"}
            </button>
            <button
              type="button"
              onClick={() => { setShowForm(false); reset(); setFormError(null); }}
              className="px-5 py-2 rounded-xl border border-gray-200 text-gray-600 text-sm hover:bg-gray-50 transition"
            >
              إلغاء
            </button>
          </div>
        </form>
      )}

      {!showForm && (
        <button
          onClick={() => setShowForm(true)}
          className="mt-3 px-4 py-2 rounded-xl border border-gray-200 text-sm text-gray-700 hover:bg-gray-50 transition"
        >
          + إضافة مؤسسة جديدة
        </button>
      )}
    </Card>
  );
}

function TenantRow({ tenant: t }: { tenant: TenantResponse }) {
  const sub      = t.activeSubscription;
  const pct      = sub?.usagePercentage ?? 0;
  const barColor = pct >= 90 ? "bg-red-500" : pct >= 70 ? "bg-orange-400" : "bg-green-500";
  const cycleEnd = sub?.billingCycleEnd
    ? new Date(sub.billingCycleEnd).toLocaleDateString("ar-YE", { year: "numeric", month: "short", day: "numeric" })
    : null;

  return (
    <Link
      href={`/admin/tenants/${t.tenantId}`}
      className="block px-4 py-3 rounded-xl border border-gray-100 hover:bg-gray-50 hover:border-gray-200 transition"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-medium text-gray-900 text-sm">{t.nameAr ?? t.name}</span>
            {t.nameAr && t.name !== t.nameAr && (
              <span className="text-gray-400 text-xs">{t.name}</span>
            )}
            {sub?.planType && (
              <span className="inline-block px-2 py-0.5 rounded-full text-xs bg-gray-100 text-gray-600">
                {sub.planType}
              </span>
            )}
          </div>
          {sub && (
            <div className="mt-2">
              <div className="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span>{sub.casesUsed} من {sub.casesAllocated} حالة</span>
                {cycleEnd && <span>ينتهي: {cycleEnd}</span>}
              </div>
              <div className="w-full h-1.5 bg-gray-100 rounded-full overflow-hidden">
                <div className={`h-full rounded-full transition-all ${barColor}`} style={{ width: `${Math.min(pct, 100)}%` }} />
              </div>
            </div>
          )}
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <span className={`inline-block px-2.5 py-0.5 rounded-full text-xs font-medium ${
            t.isActive
              ? "bg-green-50 text-green-700 border border-green-200"
              : "bg-red-50   text-red-700   border border-red-200"
          }`}>
            {t.isActive ? "نشط" : "غير نشط"}
          </span>
          <span className="text-gray-300 text-sm">›</span>
        </div>
      </div>
    </Link>
  );
}

// ── Shared primitives ─────────────────────────────────────────────────────────

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
      <div className="px-6 py-4 border-b border-gray-50">
        <h2 className="font-semibold text-gray-800">{title}</h2>
      </div>
      <div className="px-6 py-5">{children}</div>
    </div>
  );
}

const inp = (err: boolean) =>
  `w-full px-4 py-2.5 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 transition bg-gray-50 ${
    err ? "border-red-300 bg-red-50" : "border-gray-200"
  }`;

function F({
  label, req, err, children,
}: {
  label: string; req?: boolean; err?: string; children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {label}{req && <span className="text-red-500 mr-1">*</span>}
      </label>
      {children}
      {err && <p className="text-red-600 text-xs mt-1">{err}</p>}
    </div>
  );
}
