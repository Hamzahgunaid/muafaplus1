"use client";
import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { referralApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { AgeGroup, ReferralResponse } from "@/types";

// ── Types ─────────────────────────────────────────────────────────────────────

interface FormValues {
  patientPhone:            string;
  patientName:             string;
  whatsAppDelivery:        boolean;
  ageGroup:                AgeGroup;
  primaryDiagnosis:        string;
  comorbidities:           string;
  currentMedications:      string;
  allergies:               string;
  medicalRestrictions:     string;
  notificationDelayHours:  number;
}

// ── Constants ─────────────────────────────────────────────────────────────────

const AGE_GROUPS: { value: AgeGroup; label: string }[] = [
  { value: "Child",      label: "طفل (0-12 سنة)"      },
  { value: "Adolescent", label: "مراهق (13-17 سنة)"   },
  { value: "Adult",      label: "بالغ (18-65 سنة)"    },
  { value: "Elderly",    label: "كبير السن (65+)"      },
];

const DELAY_OPTIONS = [
  { value: 0,  label: "فوري (الآن)"         },
  { value: 2,  label: "ساعتان (موصى به)"    },
  { value: 4,  label: "4 ساعات"              },
  { value: 8,  label: "8 ساعات"              },
];

const RISK_CLASS: Record<string, string> = {
  LOW:      "bg-green-50  text-green-700  border border-green-200",
  MODERATE: "bg-amber-50  text-amber-700  border border-amber-200",
  HIGH:     "bg-orange-50 text-orange-700 border border-orange-200",
  CRITICAL: "bg-red-50    text-red-700    border border-red-200",
};

const RISK_LABEL: Record<string, string> = {
  LOW:      "منخفض",
  MODERATE: "متوسط",
  HIGH:     "مرتفع",
  CRITICAL: "حرج",
};

// ── Page ──────────────────────────────────────────────────────────────────────

export default function NewReferralPage() {
  const router = useRouter();
  const { isLoggedIn } = useAuthStore();
  const [submitting, setSubmitting] = useState(false);
  const [apiError,   setApiError]   = useState<string | null>(null);
  const [success,    setSuccess]    = useState<ReferralResponse | null>(null);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); }
  }, [isLoggedIn, router]);

  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      patientPhone:           "",
      patientName:            "",
      whatsAppDelivery:       true,
      ageGroup:               "Adult",
      primaryDiagnosis:       "",
      comorbidities:          "",
      currentMedications:     "",
      allergies:              "",
      medicalRestrictions:    "",
      notificationDelayHours: 2,
    },
  });

  const whatsApp = watch("whatsAppDelivery");

  const onSubmit = async (values: FormValues) => {
    setApiError(null);
    setSubmitting(true);
    try {
      const res = await referralApi.createReferral({
        patientPhone:            values.patientPhone,
        patientNameOverride:     values.patientName || undefined,
        primaryDiagnosis:        values.primaryDiagnosis,
        ageGroup:                values.ageGroup,
        comorbidities:           values.comorbidities   || undefined,
        currentMedications:      values.currentMedications || undefined,
        allergies:               values.allergies        || undefined,
        medicalRestrictions:     values.medicalRestrictions || undefined,
        notificationDelayHours:  Number(values.notificationDelayHours),
        whatsAppDelivery:        values.whatsAppDelivery,
      });
      if (res.success && res.data) {
        setSuccess(res.data);
      } else {
        setApiError(res.error ?? "حدث خطأ غير متوقع");
      }
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "خطأ في الاتصال بالخادم";
      setApiError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  if (!isLoggedIn) return null;

  // ── Success card ────────────────────────────────────────────────────────────
  if (success) {
    const deliveryTime = success.scheduledDeliveryAt
      ? new Date(success.scheduledDeliveryAt).toLocaleString("ar-YE", {
          weekday: "short", year: "numeric", month: "short",
          day: "numeric", hour: "2-digit", minute: "2-digit",
        })
      : null;

    return (
      <div className="min-h-screen flex flex-col bg-ink-50" dir="rtl">
        <NavBar />
        <main className="flex-1 max-w-2xl w-full mx-auto px-6 py-10">
          <div className="bg-white rounded-2xl border border-ink-100 px-8 py-10 text-center">
            <div className="w-16 h-16 rounded-full bg-navy-50 flex items-center justify-center mx-auto mb-4">
              <span className="text-3xl text-navy-600">✓</span>
            </div>
            <h1
              className="text-xl font-bold text-ink-900 mb-2"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              تمت الإحالة بنجاح!
            </h1>
            <p
              className="text-sm text-ink-400 mb-6"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              رمز الإحالة: <span className="font-mono font-medium text-ink-700">{success.referralCode}</span>
            </p>

            {success.riskLevel && RISK_CLASS[success.riskLevel] && (
              <div className="flex justify-center mb-4">
                <span
                  className={`inline-block px-3 py-1 rounded-full text-sm font-medium ${RISK_CLASS[success.riskLevel]}`}
                  style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                >
                  مستوى الخطر: {RISK_LABEL[success.riskLevel] ?? success.riskLevel}
                </span>
              </div>
            )}

            {deliveryTime && (
              <p
                className="text-sm text-ink-500 mb-8"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                سيتم إرسال رسالة واتساب في:{" "}
                <span className="font-medium text-ink-700">{deliveryTime}</span>
              </p>
            )}

            <div className="flex flex-col sm:flex-row gap-3 justify-center">
              <Link
                href={`/referrals/${success.referralId}`}
                className="px-6 py-2.5 rounded-xl bg-navy-600 text-white text-sm font-semibold hover:bg-navy-700 transition"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                عرض الإحالة
              </Link>
              <button
                onClick={() => { setSuccess(null); reset(); }}
                className="px-6 py-2.5 rounded-xl border border-ink-100 text-ink-500 text-sm font-medium hover:bg-ink-50 transition"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                إحالة جديدة
              </button>
            </div>
          </div>
        </main>
      </div>
    );
  }

  // ── Form ────────────────────────────────────────────────────────────────────
  return (
    <div className="min-h-screen flex flex-col bg-ink-50" dir="rtl">
      <NavBar />

      <main className="flex-1 max-w-2xl w-full mx-auto px-6 py-8">

        {/* Breadcrumb */}
        <div className="flex items-center gap-3 mb-6">
          <Link
            href="/referrals"
            className="text-ink-400 hover:text-ink-700 text-sm transition"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            ← الإحالات
          </Link>
          <span className="text-ink-100">/</span>
          <span
            className="text-sm font-medium text-ink-700"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            إحالة جديدة
          </span>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} noValidate>

          {/* ── Section 1: Patient info ──────────────────────────────────────── */}
          <SectionCard title="معلومات المريض" className="mb-4">
            <F label="رقم الهاتف" req err={errors.patientPhone?.message}>
              <input
                type="text"
                placeholder="+967XXXXXXXXX"
                className={inp(!!errors.patientPhone)}
                {...register("patientPhone", {
                  required: "رقم الهاتف مطلوب",
                  pattern:  { value: /^\+?[0-9]{9,15}$/, message: "رقم هاتف غير صالح" },
                })}
              />
            </F>

            {/* WhatsApp toggle */}
            <div>
              <label
                className="block text-sm font-medium text-ink-700 mb-2"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                واتساب متاح
              </label>
              <button
                type="button"
                onClick={() => {
                  const curr = watch("whatsAppDelivery");
                  // setValue isn't available without destructuring — use hidden input trick via register
                }}
                className="hidden"
                aria-hidden
              />
              <label className="flex items-center gap-3 cursor-pointer w-fit">
                <input type="checkbox" className="sr-only" {...register("whatsAppDelivery")} />
                <div className={`relative w-11 h-6 rounded-full transition ${whatsApp ? "bg-navy-600" : "bg-ink-100"}`}>
                  <div className={`absolute top-1 w-4 h-4 rounded-full bg-white shadow transition-all ${whatsApp ? "right-1" : "left-1"}`} />
                </div>
                <span
                  className="text-sm text-ink-700"
                  style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                >
                  {whatsApp ? "واتساب ✓" : "رسالة نصية فقط"}
                </span>
              </label>
            </div>

            <F label="اسم المريض" hint="(اختياري)" err={errors.patientName?.message}>
              <input
                type="text"
                placeholder="مثال: محمد علي"
                className={inp(!!errors.patientName)}
                {...register("patientName")}
              />
            </F>
          </SectionCard>

          {/* ── Section 2: Medical info ──────────────────────────────────────── */}
          <SectionCard title="المعلومات الطبية" className="mb-4">
            <F label="الفئة العمرية" req err={errors.ageGroup?.message}>
              <select
                className={inp(!!errors.ageGroup)}
                {...register("ageGroup", { required: "مطلوب" })}
              >
                {AGE_GROUPS.map((g) => (
                  <option key={g.value} value={g.value}>{g.label}</option>
                ))}
              </select>
            </F>

            <F label="التشخيص الرئيسي" req err={errors.primaryDiagnosis?.message}>
              <textarea
                rows={3}
                placeholder="مثال: داء السكري من النوع الثاني"
                className={inp(!!errors.primaryDiagnosis)}
                {...register("primaryDiagnosis", {
                  required:  "التشخيص مطلوب",
                  minLength: { value: 3, message: "3 أحرف على الأقل" },
                  maxLength: { value: 300, message: "الحد الأقصى 300 حرف" },
                })}
              />
            </F>

            <F label="الأمراض المصاحبة" err={errors.comorbidities?.message}>
              <textarea
                rows={2}
                placeholder="مثال: ارتفاع ضغط الدم، السمنة"
                className={inp(!!errors.comorbidities)}
                {...register("comorbidities", { maxLength: { value: 500, message: "الحد الأقصى 500" } })}
              />
            </F>

            <F label="الأدوية الحالية" err={errors.currentMedications?.message}>
              <textarea
                rows={2}
                placeholder="مثال: ميتفورمين 500 ملغ مرتين يومياً"
                className={inp(!!errors.currentMedications)}
                {...register("currentMedications", { maxLength: { value: 1000, message: "الحد الأقصى 1000" } })}
              />
            </F>

            <F label="الحساسية الدوائية" err={errors.allergies?.message}>
              <input
                type="text"
                placeholder="مثال: البنسلين"
                className={inp(!!errors.allergies)}
                {...register("allergies", { maxLength: { value: 300, message: "الحد الأقصى 300" } })}
              />
            </F>

            <F label="القيود الطبية" err={errors.medicalRestrictions?.message}>
              <textarea
                rows={2}
                placeholder="مثال: الحمل، الفشل الكلوي"
                className={inp(!!errors.medicalRestrictions)}
                {...register("medicalRestrictions", { maxLength: { value: 500, message: "الحد الأقصى 500" } })}
              />
            </F>
          </SectionCard>

          {/* ── Section 3: Delivery settings ─────────────────────────────────── */}
          <SectionCard title="إعدادات التوصيل" className="mb-6">
            <F label="تأخير إرسال الواتساب" err={errors.notificationDelayHours?.message}>
              <select
                className={inp(!!errors.notificationDelayHours)}
                {...register("notificationDelayHours", { valueAsNumber: true })}
              >
                {DELAY_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            </F>
          </SectionCard>

          {/* API error */}
          {apiError && (
            <div
              className="mb-4 px-4 py-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {apiError}
            </div>
          )}

          {/* Submit */}
          <button
            type="submit"
            disabled={submitting}
            className="w-full py-3.5 rounded-xl bg-navy-600 text-white font-semibold text-sm hover:bg-navy-700 transition disabled:opacity-60 flex items-center justify-center gap-2"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            {submitting ? <><Spin /> جارٍ الإرسال...</> : "إنشاء الإحالة وتوليد المحتوى"}
          </button>

          {submitting && (
            <p
              className="text-center text-xs text-ink-400 mt-3"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              جارٍ تحليل الحالة وتوليد المحتوى الطبي... (10-30 ثانية)
            </p>
          )}
        </form>
      </main>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

const inp = (err: boolean) =>
  `w-full px-4 py-3 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-navy-400 transition bg-white ${
    err ? "border-red-300 bg-red-50" : "border-ink-100"
  }`;

const Spin = () => (
  <span className="inline-block w-4 h-4 border-2 border-current/30 border-t-current rounded-full animate-spin flex-shrink-0" />
);

function SectionCard({
  title,
  children,
  className = "",
}: {
  title: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={`bg-white rounded-2xl border border-ink-100 overflow-hidden ${className}`}>
      <div className="px-6 py-4 border-b border-ink-100">
        <h2
          className="font-semibold text-ink-900 text-sm"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {title}
        </h2>
      </div>
      <div className="px-6 py-5 space-y-5">{children}</div>
    </div>
  );
}

function F({
  label,
  hint,
  req,
  err,
  children,
}: {
  label:    string;
  hint?:    string;
  req?:     boolean;
  err?:     string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label
        className="block text-sm font-medium text-ink-700 mb-1"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        {label}
        {req  && <span className="text-red-500 mr-1">*</span>}
        {hint && <span className="text-ink-400 font-normal text-xs mr-2">{hint}</span>}
      </label>
      {children}
      {err && (
        <p
          className="text-red-600 text-xs mt-1"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {err}
        </p>
      )}
    </div>
  );
}
