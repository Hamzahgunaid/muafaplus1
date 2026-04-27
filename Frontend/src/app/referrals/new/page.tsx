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

const RISK_STYLE: Record<string, React.CSSProperties> = {
  LOW:      { background: '#E6F4EC', color: '#197540', border: '1px solid #C1E3CD' },
  MODERATE: { background: '#FFF8E6', color: '#BA7517', border: '1px solid #F5DFA0' },
  HIGH:     { background: '#FFF0E6', color: '#D85A30', border: '1px solid #F5C6A0' },
  CRITICAL: { background: '#FBE5E5', color: '#D64545', border: '1px solid #F5B8B8' },
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
  const [hydrated,   setHydrated]   = useState(false);

  useEffect(() => {
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (hydrated && !isLoggedIn) { router.push("/login"); }
  }, [hydrated, isLoggedIn, router]);

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
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), 60000);

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

      clearTimeout(timeout);

      if (res.success && res.data) {
        setSuccess(res.data);
      } else {
        setApiError(res.error ?? "حدث خطأ غير متوقع — يرجى المحاولة مرة أخرى");
      }
    } catch (e: unknown) {
      if (e instanceof Error && e.name === 'AbortError') {
        setApiError("انتهت مهلة الطلب — الخادم لم يستجب خلال 60 ثانية. يرجى المحاولة مرة أخرى.");
      } else {
        const msg = e instanceof Error ? e.message : "خطأ في الاتصال بالخادم";
        setApiError(msg);
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (!hydrated) return null;
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
            <div
              className="w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4"
              style={{ background: '#EEF1F7' }}
            >
              <span className="text-3xl" style={{ color: '#1E3A72' }}>✓</span>
            </div>
            <h1
              className="text-xl font-bold mb-2"
              style={{ color: '#0E1726', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              تمت الإحالة بنجاح!
            </h1>
            <p
              className="text-sm mb-6"
              style={{ color: '#5A6478', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              رمز الإحالة:{" "}
              <span className="font-mono font-medium" style={{ color: '#0E1726' }}>
                {success.referralCode}
              </span>
            </p>

            {success.riskLevel && RISK_STYLE[success.riskLevel] && (
              <div className="flex justify-center mb-4">
                <span
                  className="inline-block px-3 py-1 rounded-full text-sm font-medium"
                  style={{ ...RISK_STYLE[success.riskLevel], fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                >
                  مستوى الخطر: {RISK_LABEL[success.riskLevel] ?? success.riskLevel}
                </span>
              </div>
            )}

            {deliveryTime && (
              <p
                className="text-sm mb-8"
                style={{ color: '#5A6478', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                سيتم إرسال رسالة واتساب في:{" "}
                <span className="font-medium" style={{ color: '#0E1726' }}>{deliveryTime}</span>
              </p>
            )}

            <div className="flex flex-col sm:flex-row gap-3 justify-center">
              <Link
                href={`/referrals/${success.referralId}`}
                className="px-6 py-2.5 rounded-xl text-white text-sm font-semibold transition"
                style={{ background: '#1E3A72', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                عرض الإحالة
              </Link>
              <button
                onClick={() => { setSuccess(null); reset(); }}
                className="px-6 py-2.5 rounded-xl text-sm font-medium transition"
                style={{
                  border: '1px solid #EEF0F5',
                  color: '#5A6478',
                  background: 'white',
                  fontFamily: "IBM Plex Sans Arabic, system-ui",
                }}
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
    <div className="min-h-screen flex flex-col" style={{ background: '#F6F7FB' }} dir="rtl">
      <NavBar />

      <main className="flex-1 max-w-3xl w-full mx-auto px-6 py-8">

        {/* Breadcrumb */}
        <div className="flex items-center gap-3 mb-6">
          <Link
            href="/referrals"
            className="text-sm transition"
            style={{ color: '#5A6478', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            ← الإحالات
          </Link>
          <span style={{ color: '#EEF0F5' }}>/</span>
          <span
            className="text-sm font-medium"
            style={{ color: '#0E1726', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            إحالة جديدة
          </span>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} noValidate>

          {/* ── Single white card ──────────────────────────────────────────── */}
          <div
            className="bg-white rounded-2xl overflow-hidden mb-6"
            style={{ border: '1px solid #EEF0F5' }}
          >

            {/* ── Section 1: Patient info ────────────────────────────────── */}
            <SectionHeader
              icon="👤"
              iconBg="#EEF1F7"
              iconColor="#1E3A72"
              title="معلومات المريض"
            />
            <div className="px-6 py-5 space-y-5" style={{ borderBottom: '1px solid #EEF0F5' }}>

              <F label="رقم الهاتف" req err={errors.patientPhone?.message}>
                <FI
                  type="text"
                  placeholder="+967XXXXXXXXX"
                  hasError={!!errors.patientPhone}
                  {...register("patientPhone", {
                    required: "رقم الهاتف مطلوب",
                    pattern:  { value: /^\+?[0-9]{9,15}$/, message: "رقم هاتف غير صالح" },
                  })}
                />
              </F>

              {/* WhatsApp checkbox row */}
              <div>
                <label
                  className="flex items-center justify-between p-4 rounded-xl cursor-pointer"
                  style={{ background: '#F6F7FB', border: '1px solid #EEF0F5' }}
                >
                  <div>
                    <div
                      className="text-sm font-semibold"
                      style={{ color: '#2D3748', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                    >
                      إرسال عبر واتساب
                    </div>
                    <div
                      className="text-xs mt-0.5"
                      style={{ color: '#5A6478', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                    >
                      {whatsApp ? "سيتم إرسال المحتوى الطبي للمريض عبر واتساب" : "رسالة نصية فقط"}
                    </div>
                  </div>
                  <input
                    type="checkbox"
                    className="w-5 h-5 accent-[#1E3A72]"
                    {...register("whatsAppDelivery")}
                  />
                </label>
              </div>

              <F label="اسم المريض" hint="(اختياري)" err={errors.patientName?.message}>
                <FI
                  type="text"
                  placeholder="مثال: محمد علي"
                  hasError={!!errors.patientName}
                  {...register("patientName")}
                />
              </F>
            </div>

            {/* ── Section 2: Medical info ────────────────────────────────── */}
            <SectionHeader
              icon="🏥"
              iconBg="#FDF3E1"
              iconColor="#E87A2F"
              title="المعلومات الطبية"
            />
            <div className="px-6 py-5 space-y-5" style={{ borderBottom: '1px solid #EEF0F5' }}>

              <F label="الفئة العمرية" req err={errors.ageGroup?.message}>
                <FI
                  as="select"
                  hasError={!!errors.ageGroup}
                  {...register("ageGroup", { required: "مطلوب" })}
                >
                  {AGE_GROUPS.map((g) => (
                    <option key={g.value} value={g.value}>{g.label}</option>
                  ))}
                </FI>
              </F>

              <F label="التشخيص الرئيسي" req err={errors.primaryDiagnosis?.message}>
                <FI
                  as="textarea"
                  rows={3}
                  placeholder="مثال: داء السكري من النوع الثاني"
                  hasError={!!errors.primaryDiagnosis}
                  {...register("primaryDiagnosis", {
                    required:  "التشخيص مطلوب",
                    minLength: { value: 3, message: "3 أحرف على الأقل" },
                    maxLength: { value: 300, message: "الحد الأقصى 300 حرف" },
                  })}
                />
              </F>

              <F label="الأمراض المصاحبة" err={errors.comorbidities?.message}>
                <FI
                  as="textarea"
                  rows={2}
                  placeholder="مثال: ارتفاع ضغط الدم، السمنة"
                  hasError={!!errors.comorbidities}
                  {...register("comorbidities", { maxLength: { value: 500, message: "الحد الأقصى 500" } })}
                />
              </F>

              <F label="الأدوية الحالية" err={errors.currentMedications?.message}>
                <FI
                  as="textarea"
                  rows={2}
                  placeholder="مثال: ميتفورمين 500 ملغ مرتين يومياً"
                  hasError={!!errors.currentMedications}
                  {...register("currentMedications", { maxLength: { value: 1000, message: "الحد الأقصى 1000" } })}
                />
              </F>

              <F label="الحساسية الدوائية" err={errors.allergies?.message}>
                <FI
                  type="text"
                  placeholder="مثال: البنسلين"
                  hasError={!!errors.allergies}
                  {...register("allergies", { maxLength: { value: 300, message: "الحد الأقصى 300" } })}
                />
              </F>

              <F label="القيود الطبية" err={errors.medicalRestrictions?.message}>
                <FI
                  as="textarea"
                  rows={2}
                  placeholder="مثال: الحمل، الفشل الكلوي"
                  hasError={!!errors.medicalRestrictions}
                  {...register("medicalRestrictions", { maxLength: { value: 500, message: "الحد الأقصى 500" } })}
                />
              </F>
            </div>

            {/* ── Section 3: Delivery settings ──────────────────────────── */}
            <SectionHeader
              icon="📱"
              iconBg="#EEF1F7"
              iconColor="#1E3A72"
              title="إعدادات التوصيل"
            />
            <div className="px-6 py-5">
              <F label="تأخير إرسال الواتساب" err={errors.notificationDelayHours?.message}>
                <FI
                  as="select"
                  hasError={!!errors.notificationDelayHours}
                  {...register("notificationDelayHours", { valueAsNumber: true })}
                >
                  {DELAY_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </FI>
              </F>
            </div>

          </div>

          {/* API error */}
          {apiError && (
            <div
              className="mb-4 px-4 py-3 rounded-xl text-sm"
              style={{
                background: '#FBE5E5',
                border: '1px solid #F5B8B8',
                color: '#D64545',
                fontFamily: "IBM Plex Sans Arabic, system-ui",
              }}
            >
              {apiError}
            </div>
          )}

          {/* Submit */}
          <button
            type="submit"
            disabled={submitting}
            className="w-full py-3.5 rounded-xl text-white font-semibold text-sm transition disabled:opacity-60 flex items-center justify-center gap-2"
            style={{
              background: submitting ? '#4E6AA3' : '#1E3A72',
              fontFamily: "IBM Plex Sans Arabic, system-ui",
              boxShadow: '0 4px 16px rgba(30,58,114,0.25)',
            }}
          >
            {submitting ? <><Spin /> جارٍ الإرسال...</> : "إنشاء الإحالة وتوليد المحتوى"}
          </button>

          {submitting && (
            <p
              className="text-center text-xs mt-3"
              style={{ color: '#5A6478', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
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

function SectionHeader({
  icon,
  iconBg,
  iconColor,
  title,
}: {
  icon:      string;
  iconBg:    string;
  iconColor: string;
  title:     string;
}) {
  return (
    <div className="px-6 py-4" style={{ borderBottom: '1px solid #EEF0F5' }}>
      <div className="flex items-center gap-3">
        <div
          className="w-8 h-8 rounded-lg flex items-center justify-center text-base flex-shrink-0"
          style={{ background: iconBg, color: iconColor }}
        >
          {icon}
        </div>
        <h2
          className="font-semibold text-sm"
          style={{ color: '#0E1726', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {title}
        </h2>
      </div>
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
        className="block text-sm font-semibold mb-1.5"
        style={{ color: '#2D3748', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        {label}
        {req  && <span className="text-red-500 mr-1">*</span>}
        {hint && <span className="font-normal text-xs mr-2" style={{ color: '#8A93A6' }}>{hint}</span>}
      </label>
      {children}
      {err && (
        <p
          className="text-xs mt-1"
          style={{ color: '#D64545', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {err}
        </p>
      )}
    </div>
  );
}

// FI = focusable input — zero hooks, no SSR/client mismatch
const FI = ({
  as: Tag = "input",
  hasError,
  children,
  ...props
}: {
  as?: "input" | "textarea" | "select";
  hasError?: boolean;
  children?: React.ReactNode;
  [key: string]: unknown;
}) => {
  const { hasError: _h, ...rest } = { hasError, ...props };
  void _h;

  return (
    <Tag
      style={{
        width: '100%',
        padding: '12px 16px',
        borderRadius: '12px',
        border: `1.5px solid ${hasError ? '#F5B8B8' : '#EEF0F5'}`,
        fontSize: '14px',
        outline: 'none',
        background: hasError ? '#FBE5E5' : 'white',
        color: '#0E1726',
        fontFamily: "IBM Plex Sans Arabic, system-ui",
        transition: 'border-color 0.15s',
      }}
      onFocus={(e: React.FocusEvent<HTMLElement>) => {
        e.currentTarget.style.borderColor = hasError ? '#F5B8B8' : '#1E3A72';
      }}
      onBlur={(e: React.FocusEvent<HTMLElement>) => {
        e.currentTarget.style.borderColor = hasError ? '#F5B8B8' : '#EEF0F5';
      }}
      {...(rest as React.HTMLAttributes<HTMLElement>)}
    >
      {children}
    </Tag>
  );
};

const Spin = () => (
  <span className="inline-block w-4 h-4 border-2 border-current/30 border-t-current rounded-full animate-spin flex-shrink-0" />
);
