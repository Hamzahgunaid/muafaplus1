"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { useAuthStore } from "@/lib/store";
import {
  tenantApi, userApi, tenantSettingsApi, assistantLinkApi, subscriptionApi,
} from "@/services/api";
import NavBar from "@/components/NavBar";
import type {
  TenantResponse, TenantSubscriptionSummary,
  TenantSettingsResponse, UpdateTenantSettingsRequest,
  UserResponse, AssistantLinkResponse,
} from "@/types";

// ── Constants ─────────────────────────────────────────────────────────────────

const PLAN_OPTIONS = ["Starter", "Clinic", "Hospital", "Enterprise"];

const ROLE_OPTIONS = [
  { value: "HospitalAdmin", label: "مدير مستشفى" },
  { value: "Physician",     label: "طبيب"          },
  { value: "Assistant",     label: "مساعد"          },
];

const EXPIRY_OPTIONS = [
  { value: 7,  label: "7 أيام"   },
  { value: 30, label: "30 يوماً" },
  { value: 90, label: "90 يوماً" },
];

const PATIENT_NAME_POLICIES = [
  { value: "phone",    label: "رقم الهاتف فقط"              },
  { value: "override", label: "اسم المريض (إذا توفر)"       },
  { value: "always",   label: "الاسم مطلوب دائماً"           },
];

// ── Form types ────────────────────────────────────────────────────────────────

interface TenantFormValues {
  nameAr:         string;
  name:           string;
  adminEmail:     string;
  planType:       string;
  casesAllocated: number;
}

interface InviteFormValues {
  role:          string;
  expiresInDays: number;
}

interface GeneratedCode {
  code:      string;
  expiresAt: string;
  role:      string;
}

interface LinkFormValues {
  assistantId: string;
  physicianId: string;
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function AdminPage() {
  const router = useRouter();
  const { isLoggedIn, role, tenantId } = useAuthStore();

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    if (role && role !== "SuperAdmin" && role !== "HospitalAdmin") {
      router.push("/dashboard");
    }
  }, [isLoggedIn, role, router]);

  if (!isLoggedIn) return null;
  if (role && role !== "SuperAdmin" && role !== "HospitalAdmin") return null;

  const isSuperAdmin    = role === "SuperAdmin";
  const isHospitalAdmin = role === "HospitalAdmin";

  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />
      <main className="flex-1 max-w-4xl w-full mx-auto px-6 py-8">
        <h1 className="text-xl font-bold text-gray-900 mb-6">لوحة الإدارة</h1>
        <div className="space-y-6">
          <TenantsCard isSuperAdmin={isSuperAdmin} />
          <InvitationCodeCard />
          {isHospitalAdmin && tenantId && (
            <>
              <SubscriptionCard    tenantId={tenantId} />
              <TenantSettingsCard  tenantId={tenantId} />
              <UsersCard           tenantId={tenantId} />
              <AssistantLinksCard  tenantId={tenantId} />
            </>
          )}
        </div>
      </main>
    </div>
  );
}

// ── Section 1: Tenants ────────────────────────────────────────────────────────

function TenantsCard({ isSuperAdmin }: { isSuperAdmin: boolean }) {
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
      if (isSuperAdmin) {
        const res = await tenantApi.getTenants();
        if (res.success && res.data) setTenants(res.data);
        else setError(res.error ?? "تعذر تحميل المؤسسات");
      } else {
        const tenantId = localStorage.getItem("muafa_tenantid");
        if (!tenantId) { setError("تعذر تحديد المؤسسة"); setLoading(false); return; }
        const res = await tenantApi.getTenant(tenantId);
        if (res.success && res.data) setTenants([res.data]);
        else setError(res.error ?? "تعذر تحميل بيانات المؤسسة");
      }
    } catch {
      setError("خطأ في الاتصال");
    } finally {
      setLoading(false);
    }
  }, [isSuperAdmin]);

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
    <Card title="المستأجرون">
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
        <div className="space-y-3 mb-4">
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

      {!showForm && isSuperAdmin && (
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
    <div className="px-4 py-3 rounded-xl border border-gray-100 hover:bg-gray-50 transition">
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
        <span className={`shrink-0 inline-block px-2.5 py-0.5 rounded-full text-xs font-medium ${
          t.isActive
            ? "bg-green-50 text-green-700 border border-green-200"
            : "bg-red-50   text-red-700   border border-red-200"
        }`}>
          {t.isActive ? "نشط" : "غير نشط"}
        </span>
      </div>
    </div>
  );
}

// ── Section 2: Invitation Codes ───────────────────────────────────────────────

function InvitationCodeCard() {
  const [generatedCodes, setGeneratedCodes] = useState<GeneratedCode[]>([]);
  const [submitting,     setSubmitting]     = useState(false);
  const [error,          setError]          = useState<string | null>(null);
  const [copied,         setCopied]         = useState<string | null>(null);

  const { register, handleSubmit, formState: { errors } } = useForm<InviteFormValues>({
    defaultValues: { role: "Physician", expiresInDays: 30 },
  });

  const onSubmit = async (values: InviteFormValues) => {
    setError(null);
    setSubmitting(true);
    try {
      const expiresAt = new Date(
        Date.now() + Number(values.expiresInDays) * 24 * 60 * 60 * 1000
      ).toISOString();
      const res = await tenantApi.generateInvitationCode({ role: values.role, expiresAt });
      if (res.success && res.data) {
        setGeneratedCodes((prev) => [
          { code: res.data!.code, expiresAt: res.data!.expiresAt, role: values.role },
          ...prev,
        ]);
      } else {
        setError(res.error ?? "حدث خطأ");
      }
    } catch {
      setError("خطأ في الاتصال");
    } finally {
      setSubmitting(false);
    }
  };

  const copyCode = async (code: string) => {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(code);
      setTimeout(() => setCopied(null), 2000);
    } catch { /* clipboard not available */ }
  };

  return (
    <Card title="رموز الدعوة">
      <h3 className="text-sm font-semibold text-gray-800 mb-4">إنشاء رمز دعوة جديد</h3>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        <div className="grid grid-cols-2 gap-4">
          <F label="نوع الدور" req err={errors.role?.message}>
            <select className={inp(!!errors.role)} {...register("role", { required: "مطلوب" })}>
              {ROLE_OPTIONS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </F>
          <F label="صلاحية الرمز" req err={errors.expiresInDays?.message}>
            <select
              className={inp(!!errors.expiresInDays)}
              {...register("expiresInDays", { required: "مطلوب", valueAsNumber: true })}
            >
              {EXPIRY_OPTIONS.map((e) => <option key={e.value} value={e.value}>{e.label}</option>)}
            </select>
          </F>
        </div>

        {error && (
          <div className="px-4 py-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">{error}</div>
        )}

        <button
          type="submit"
          disabled={submitting}
          className="px-5 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-medium hover:bg-brand-800 transition disabled:opacity-60"
        >
          {submitting ? "جارٍ الإنشاء..." : "إنشاء الرمز"}
        </button>
      </form>

      {generatedCodes.length > 0 && (
        <div className="mt-6 space-y-3">
          <p className="text-xs font-medium text-gray-500">الرموز المنشأة في هذه الجلسة</p>
          {generatedCodes.map((gc, i) => {
            const expiry    = new Date(gc.expiresAt).toLocaleDateString("ar-YE", { year: "numeric", month: "short", day: "numeric" });
            const roleLabel = ROLE_OPTIONS.find((r) => r.value === gc.role)?.label ?? gc.role;
            return (
              <div key={i} className="rounded-xl bg-brand-50 border border-brand-100 overflow-hidden">
                <div className="flex items-center gap-3 px-4 py-3">
                  <span className="flex-1 font-mono font-bold text-brand-800 text-lg tracking-widest">{gc.code}</span>
                  <button
                    type="button"
                    onClick={() => copyCode(gc.code)}
                    className="text-xs px-3 py-1.5 rounded-lg border border-brand-200 text-brand-700 hover:bg-brand-100 transition"
                  >
                    {copied === gc.code ? "✓ تم النسخ" : "نسخ"}
                  </button>
                </div>
                <div className="flex items-center justify-between text-xs text-brand-600 px-4 pb-3">
                  <span>الدور: {roleLabel}</span>
                  <span>ينتهي في: {expiry}</span>
                </div>
              </div>
            );
          })}
          <p className="text-xs text-gray-400">أرسل هذا الرمز للمستخدم الجديد</p>
        </div>
      )}
    </Card>
  );
}

// ── Section 3: Subscription ───────────────────────────────────────────────────

function SubscriptionCard({ tenantId }: { tenantId: string }) {
  const [sub,     setSub]     = useState<TenantSubscriptionSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);

  useEffect(() => {
    subscriptionApi.getSubscription(tenantId)
      .then((res) => { if (res.success && res.data) setSub(res.data); else setError(res.error ?? "تعذر التحميل"); })
      .catch(() => setError("خطأ في الاتصال"))
      .finally(() => setLoading(false));
  }, [tenantId]);

  const pct      = sub?.usagePercentage ?? 0;
  const barColor = pct >= 90 ? "bg-red-500" : pct >= 70 ? "bg-orange-400" : "bg-green-500";
  const cycleEnd = sub?.billingCycleEnd
    ? new Date(sub.billingCycleEnd).toLocaleDateString("ar-YE", { year: "numeric", month: "long", day: "numeric" })
    : null;

  return (
    <Card title="الاشتراك">
      {loading ? (
        <div className="py-6 text-center text-gray-400 text-sm">جاري التحميل...</div>
      ) : error ? (
        <p className="text-red-500 text-sm py-4">{error}</p>
      ) : sub ? (
        <div className="space-y-4">
          <div className="flex items-center gap-3">
            <span className="inline-block px-3 py-1 rounded-full text-sm font-medium bg-brand-50 text-brand-700 border border-brand-100">
              {sub.planType}
            </span>
            {cycleEnd && (
              <span className="text-xs text-gray-400">ينتهي في {cycleEnd}</span>
            )}
          </div>
          <div>
            <div className="flex items-center justify-between text-sm mb-2">
              <span className="text-gray-700">الحالات المستخدمة</span>
              <span className="font-semibold text-gray-900">{sub.casesUsed} / {sub.casesAllocated}</span>
            </div>
            <div className="w-full h-2 bg-gray-100 rounded-full overflow-hidden">
              <div className={`h-full rounded-full transition-all ${barColor}`} style={{ width: `${Math.min(pct, 100)}%` }} />
            </div>
            <p className="text-xs text-gray-400 mt-1">{pct.toFixed(1)}% مستخدم</p>
          </div>
        </div>
      ) : (
        <p className="text-gray-400 text-sm py-4">لا توجد بيانات اشتراك</p>
      )}
    </Card>
  );
}

// ── Section 4: Tenant Settings ────────────────────────────────────────────────

function TenantSettingsCard({ tenantId }: { tenantId: string }) {
  const [settings,   setSettings]   = useState<TenantSettingsResponse | null>(null);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState<string | null>(null);
  const [saving,     setSaving]     = useState(false);
  const [saveError,  setSaveError]  = useState<string | null>(null);
  const [saveOk,     setSaveOk]     = useState(false);

  useEffect(() => {
    tenantSettingsApi.getSettings(tenantId)
      .then((res) => { if (res.success && res.data) setSettings(res.data); else setError(res.error ?? "تعذر التحميل"); })
      .catch(() => setError("خطأ في الاتصال"))
      .finally(() => setLoading(false));
  }, [tenantId]);

  const handleSave = async (patch: UpdateTenantSettingsRequest) => {
    setSaveError(null);
    setSaveOk(false);
    setSaving(true);
    try {
      const res = await tenantSettingsApi.updateSettings(tenantId, patch);
      if (res.success && res.data) {
        setSettings(res.data);
        setSaveOk(true);
        setTimeout(() => setSaveOk(false), 2500);
      } else {
        setSaveError(res.error ?? "حدث خطأ");
      }
    } catch {
      setSaveError("خطأ في الاتصال");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card title="إعدادات المؤسسة">
      {loading ? (
        <div className="py-6 text-center text-gray-400 text-sm">جاري التحميل...</div>
      ) : error ? (
        <p className="text-red-500 text-sm py-4">{error}</p>
      ) : settings ? (
        <div className="space-y-5">
          {/* Patient name policy */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">سياسة اسم المريض</label>
            <select
              className={inp(false)}
              value={settings.patientNamePolicy}
              onChange={(e) => setSettings({ ...settings, patientNamePolicy: e.target.value })}
              disabled={saving}
            >
              {PATIENT_NAME_POLICIES.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
            </select>
          </div>

          {/* Notification delay */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              تأخير إرسال الإشعار (ساعات)
            </label>
            <input
              type="number"
              min={0}
              max={72}
              className={inp(false)}
              value={settings.notificationDelayHours}
              onChange={(e) => setSettings({ ...settings, notificationDelayHours: Number(e.target.value) })}
              disabled={saving}
            />
          </div>

          {/* Toggles */}
          <div className="space-y-3">
            <ToggleRow
              label="واتساب مفعّل"
              description="إرسال المحتوى للمرضى عبر واتساب"
              checked={settings.whatsAppEnabled}
              disabled={saving}
              onChange={(v) => setSettings({ ...settings, whatsAppEnabled: v })}
            />
            <ToggleRow
              label="المحادثة مفعّلة"
              description="السماح للمرضى بالمحادثة مع الطبيب"
              checked={settings.chatEnabled}
              disabled={saving}
              onChange={(v) => setSettings({ ...settings, chatEnabled: v })}
            />
          </div>

          {saveError && <p className="text-red-600 text-xs">{saveError}</p>}
          {saveOk    && <p className="text-green-600 text-xs">تم الحفظ بنجاح</p>}

          <button
            onClick={() => handleSave({
              patientNamePolicy:      settings.patientNamePolicy,
              whatsAppEnabled:        settings.whatsAppEnabled,
              chatEnabled:            settings.chatEnabled,
              notificationDelayHours: settings.notificationDelayHours,
            })}
            disabled={saving}
            className="px-5 py-2 rounded-xl bg-brand-600 text-white text-sm font-medium hover:bg-brand-800 transition disabled:opacity-60"
          >
            {saving ? "جارٍ الحفظ..." : "حفظ الإعدادات"}
          </button>
        </div>
      ) : (
        <p className="text-gray-400 text-sm py-4">لا توجد إعدادات</p>
      )}
    </Card>
  );
}

// ── Section 5: Users ──────────────────────────────────────────────────────────

function UsersCard({ tenantId }: { tenantId: string }) {
  const [users,   setUsers]   = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await userApi.getUsersByTenant(tenantId);
      if (res.success && res.data) setUsers(res.data);
      else setError(res.error ?? "تعذر تحميل المستخدمين");
    } catch {
      setError("خطأ في الاتصال");
    } finally {
      setLoading(false);
    }
  }, [tenantId]);

  useEffect(() => { fetchUsers(); }, [fetchUsers]);

  const ROLE_LABEL: Record<string, string> = {
    HospitalAdmin: "مدير مستشفى",
    Physician:     "طبيب",
    Assistant:     "مساعد",
  };

  return (
    <Card title="المستخدمون">
      {loading ? (
        <div className="py-6 text-center text-gray-400 text-sm">جاري التحميل...</div>
      ) : error ? (
        <div className="py-4 text-center">
          <p className="text-red-500 text-sm mb-2">{error}</p>
          <button onClick={fetchUsers} className="text-brand-600 text-sm hover:underline">إعادة المحاولة</button>
        </div>
      ) : users.length === 0 ? (
        <p className="text-gray-400 text-sm py-4">لا يوجد مستخدمون مسجلون</p>
      ) : (
        <ul className="divide-y divide-gray-50">
          {users.map((u) => (
            <li key={u.userId} className="py-3 flex items-center justify-between gap-4">
              <div>
                <span className="text-sm font-medium text-gray-800">{u.fullName}</span>
                <span className="block text-xs text-gray-400 mt-0.5">{u.email}</span>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <span className="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-gray-600">
                  {ROLE_LABEL[u.role] ?? u.role}
                </span>
                <span className={`text-xs px-2 py-0.5 rounded-full ${
                  u.isActive
                    ? "bg-green-50 text-green-700 border border-green-200"
                    : "bg-red-50 text-red-700 border border-red-200"
                }`}>
                  {u.isActive ? "نشط" : "غير نشط"}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

// ── Section 6: Assistant Links ────────────────────────────────────────────────

function AssistantLinksCard({ tenantId }: { tenantId: string }) {
  const [links,      setLinks]      = useState<AssistantLinkResponse[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState<string | null>(null);
  const [showForm,   setShowForm]   = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formError,  setFormError]  = useState<string | null>(null);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<LinkFormValues>();

  const fetchLinks = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await assistantLinkApi.getLinks(tenantId);
      if (res.success && res.data) setLinks(res.data);
      else setError(res.error ?? "تعذر تحميل الروابط");
    } catch {
      setError("خطأ في الاتصال");
    } finally {
      setLoading(false);
    }
  }, [tenantId]);

  useEffect(() => { fetchLinks(); }, [fetchLinks]);

  const onSubmit = async (values: LinkFormValues) => {
    setFormError(null);
    setSubmitting(true);
    try {
      const res = await assistantLinkApi.createLink(tenantId, {
        assistantId: values.assistantId.trim(),
        physicianId: values.physicianId.trim(),
      });
      if (res.success) {
        await fetchLinks();
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
    <Card title="روابط المساعدين">
      {loading ? (
        <div className="py-6 text-center text-gray-400 text-sm">جاري التحميل...</div>
      ) : error ? (
        <div className="py-4 text-center">
          <p className="text-red-500 text-sm mb-2">{error}</p>
          <button onClick={fetchLinks} className="text-brand-600 text-sm hover:underline">إعادة المحاولة</button>
        </div>
      ) : links.length === 0 && !showForm ? (
        <p className="text-gray-400 text-sm py-4">لا توجد روابط مساعدين</p>
      ) : (
        <ul className="divide-y divide-gray-50 mb-4">
          {links.map((l) => (
            <li key={l.linkId} className="py-3 flex items-center justify-between gap-4">
              <div>
                <span className="text-sm font-medium text-gray-800">{l.assistantName}</span>
                <span className="text-gray-400 text-xs mx-2">←</span>
                <span className="text-sm text-gray-700">{l.physicianName}</span>
              </div>
              <span className="text-xs text-gray-400">
                {new Date(l.createdAt).toLocaleDateString("ar-YE")}
              </span>
            </li>
          ))}
        </ul>
      )}

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-4 border-t border-gray-100 pt-5 space-y-4" noValidate>
          <h3 className="text-sm font-semibold text-gray-800">ربط مساعد بطبيب</h3>

          <div className="grid grid-cols-2 gap-4">
            <F label="معرّف المساعد" req err={errors.assistantId?.message}>
              <input
                className={inp(!!errors.assistantId)}
                placeholder="مثال: AST001"
                {...register("assistantId", { required: "مطلوب" })}
              />
            </F>
            <F label="معرّف الطبيب" req err={errors.physicianId?.message}>
              <input
                className={inp(!!errors.physicianId)}
                placeholder="مثال: PHY001"
                {...register("physicianId", { required: "مطلوب" })}
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
              {submitting ? "جارٍ الربط..." : "إنشاء الرابط"}
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
          + ربط مساعد بطبيب
        </button>
      )}
    </Card>
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

function ToggleRow({
  label, description, checked, disabled, onChange,
}: {
  label: string; description: string; checked: boolean; disabled: boolean; onChange: (v: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between gap-6">
      <div>
        <p className="text-sm text-gray-700">{label}</p>
        <p className="text-xs text-gray-400 mt-0.5">{description}</p>
      </div>
      <button
        type="button"
        onClick={() => onChange(!checked)}
        disabled={disabled}
        className="relative shrink-0"
        aria-label={label}
      >
        <div className={`w-12 h-6 rounded-full transition ${checked ? "bg-brand-600" : "bg-gray-300"} ${disabled ? "opacity-50" : ""}`}>
          <div className={`absolute top-1 w-4 h-4 rounded-full bg-white shadow transition-all ${checked ? "right-1" : "left-1"}`} />
        </div>
      </button>
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
