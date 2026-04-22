"use client";
import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { testScenarioApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import ArticleContentViewer from "@/components/ArticleContentViewer";
import type { AgeGroup, TestScenarioResponse, Stage1Output, CreateTestScenarioRequest } from "@/types";

// ── Types ─────────────────────────────────────────────────────────────────────

interface FormValues {
  testPatientName:     string;
  ageGroup:            AgeGroup;
  primaryDiagnosis:    string;
  comorbidities:       string;
  currentMedications:  string;
  allergies:           string;
  medicalRestrictions: string;
}

// ── Constants ─────────────────────────────────────────────────────────────────

const AGE_GROUPS: { value: AgeGroup; label: string }[] = [
  { value: "Child",      label: "طفل (0-12 سنة)"    },
  { value: "Adolescent", label: "مراهق (13-17 سنة)" },
  { value: "Adult",      label: "بالغ (18-65 سنة)"  },
  { value: "Elderly",    label: "كبير السن (65+)"    },
];

const RISK_CLASS: Record<string, string> = {
  LOW:      "bg-green-50  text-green-700  border border-green-200",
  MODERATE: "bg-blue-50   text-blue-700   border border-blue-200",
  HIGH:     "bg-orange-50 text-orange-700 border border-orange-200",
  CRITICAL: "bg-red-50    text-red-700    border border-red-200",
};

const RISK_LABEL: Record<string, string> = {
  LOW: "منخفض", MODERATE: "متوسط", HIGH: "مرتفع", CRITICAL: "حرج",
};

// ── Page ──────────────────────────────────────────────────────────────────────

export default function NewTestScenarioPage() {
  const router = useRouter();
  const { isLoggedIn } = useAuthStore();
  const [submitting,    setSubmitting]    = useState(false);
  const [apiError,      setApiError]      = useState<string | null>(null);
  const [success,       setSuccess]       = useState<TestScenarioResponse | null>(null);
  const [expanded,      setExpanded]      = useState(false);
  const [streaming,     setStreaming]     = useState(false);
  const [streamResult,  setStreamResult]  = useState<Stage1Output | null>(null);
  const [streamError,   setStreamError]   = useState<string | null>(null);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); }
  }, [isLoggedIn, router]);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
    defaultValues: { ageGroup: "Adult" },
  });

  const onSubmit = async (values: FormValues) => {
    setApiError(null);
    setSubmitting(true);
    setExpanded(false);
    try {
      const res = await testScenarioApi.createTestScenario({
        primaryDiagnosis:    values.primaryDiagnosis,
        ageGroup:            values.ageGroup,
        comorbidities:       values.comorbidities    || undefined,
        currentMedications:  values.currentMedications || undefined,
        allergies:           values.allergies         || undefined,
        medicalRestrictions: values.medicalRestrictions || undefined,
      });
      if (res.success && res.data) {
        setSuccess(res.data);
      } else {
        setApiError(res.error ?? "حدث خطأ غير متوقع");
      }
    } catch (e: unknown) {
      setApiError(e instanceof Error ? e.message : "خطأ في الاتصال");
    } finally {
      setSubmitting(false);
    }
  };

  const handleStream = async (values: FormValues) => {
    setStreaming(true);
    setStreamResult(null);
    setStreamError(null);

    const formData: CreateTestScenarioRequest = {
      primaryDiagnosis:    values.primaryDiagnosis,
      ageGroup:            values.ageGroup,
      comorbidities:       values.comorbidities       || undefined,
      currentMedications:  values.currentMedications  || undefined,
      allergies:           values.allergies            || undefined,
      medicalRestrictions: values.medicalRestrictions  || undefined,
    };

    try {
      const token = localStorage.getItem("muafa_token");
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/v1/test-scenarios/generate/stream`,
        {
          method: "POST",
          headers: {
            "Content-Type":  "application/json",
            "Authorization": `Bearer ${token}`,
          },
          body: JSON.stringify(formData),
        }
      );

      if (!response.ok) {
        setStreamError("فشل الاتصال بالخادم");
        return;
      }

      const reader  = response.body!.getReader();
      const decoder = new TextDecoder();
      let buffer    = "";

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split("\n");
        buffer = lines.pop() ?? "";

        for (const line of lines) {
          if (!line.startsWith("data:")) continue;
          const raw = line.slice(5).trim();
          if (raw === "[DONE]") { reader.cancel(); return; }
          try {
            const parsed = JSON.parse(raw);
            if (parsed.error) { setStreamError(parsed.error); return; }
            setStreamResult(parsed as Stage1Output);
          } catch {
            // incomplete chunk — wait for next read
          }
        }
      }
    } catch {
      setStreamError("خطأ في الاتصال");
    } finally {
      setStreaming(false);
    }
  };

  if (!isLoggedIn) return null;

  // ── Success preview ─────────────────────────────────────────────────────────
  if (success) {
    const generated = parseGeneratedContent(success.generatedContentJson);
    const riskLevel = generated?.risk_assessment?.risk_level ?? null;
    const summary   = generated?.summary_article ?? null;
    const articles  = generated?.article_outlines ?? [];

    return (
      <div className="min-h-screen flex flex-col">
        <NavBar />
        <main className="flex-1 max-w-2xl w-full mx-auto px-6 py-8">
          <div className="flex items-center gap-3 mb-6">
            <Link href="/test-scenarios" className="text-gray-400 hover:text-gray-700 text-sm transition">
              ← السيناريوهات
            </Link>
            <span className="text-gray-200">/</span>
            <span className="text-sm font-medium text-gray-700">نتيجة التوليد</span>
          </div>

          <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden mb-4">
            <div className="px-6 py-4 border-b border-gray-50">
              <h2 className="font-semibold text-gray-800 text-sm">المحتوى المولّد</h2>
            </div>
            <div className="px-6 py-5 space-y-4">
              {/* Risk badge */}
              {riskLevel && RISK_CLASS[riskLevel] && (
                <div>
                  <span className={`inline-block px-3 py-1 rounded-full text-sm font-medium ${RISK_CLASS[riskLevel]}`}>
                    مستوى الخطر: {RISK_LABEL[riskLevel] ?? riskLevel}
                  </span>
                </div>
              )}

              {/* Summary article */}
              {summary && (
                <div>
                  <p className="text-xs font-medium text-gray-500 mb-1">الملخص الصحي</p>
                  <div className="text-sm text-gray-700 leading-relaxed bg-gray-50 rounded-xl p-4">
                    {expanded ? summary : summary.slice(0, 500)}
                    {summary.length > 500 && (
                      <>
                        {!expanded && "..."}
                        <button
                          onClick={() => setExpanded(!expanded)}
                          className="block mt-2 text-brand-600 text-xs hover:underline"
                        >
                          {expanded ? "عرض أقل" : "اقرأ المزيد"}
                        </button>
                      </>
                    )}
                  </div>
                </div>
              )}

              {/* Article outlines */}
              {articles.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-gray-500 mb-2">المقالات التفصيلية ({articles.length})</p>
                  <ol className="space-y-1 list-decimal list-inside">
                    {articles.map((a, i) => (
                      <li key={i} className="text-sm text-gray-700">
                        {a.title_ar || a.title_en || `مقالة ${i + 1}`}
                      </li>
                    ))}
                  </ol>
                </div>
              )}
            </div>
          </div>

          <div className="flex flex-col sm:flex-row gap-3">
            <Link
              href={`/test-scenarios/${success.scenarioId}`}
              className="flex-1 text-center px-6 py-2.5 rounded-xl bg-brand-600 text-white text-sm font-medium hover:bg-brand-800 transition"
            >
              تقييم هذا المحتوى
            </Link>
            <button
              onClick={() => { setSuccess(null); reset(); }}
              className="flex-1 px-6 py-2.5 rounded-xl border border-gray-200 text-gray-600 text-sm font-medium hover:bg-gray-50 transition"
            >
              سيناريو جديد
            </button>
          </div>
        </main>
      </div>
    );
  }

  // ── Form ────────────────────────────────────────────────────────────────────
  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />
      <main className="flex-1 max-w-2xl w-full mx-auto px-6 py-8">

        <div className="flex items-center gap-3 mb-6">
          <Link href="/test-scenarios" className="text-gray-400 hover:text-gray-700 text-sm transition">
            ← السيناريوهات
          </Link>
          <span className="text-gray-200">/</span>
          <span className="text-sm font-medium text-gray-700">سيناريو جديد</span>
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden mb-4">
          <div className="px-6 py-5 border-b border-gray-50">
            <h1 className="text-lg font-semibold text-gray-900">سيناريو اختبار جديد</h1>
            <p className="text-sm text-gray-400 mt-0.5">
              أنشئ مريضاً افتراضياً لتقييم جودة المحتوى الطبي المولّد
            </p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="px-6 py-5 space-y-5" noValidate>
            <F label="اسم السيناريو" hint="(اختياري)">
              <input
                type="text"
                placeholder="مثال: مريض سكري مع ضغط الدم"
                className={inp(false)}
                {...register("testPatientName")}
              />
            </F>

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
                  required: "التشخيص مطلوب",
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

            {apiError && (
              <div className="px-4 py-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
                {apiError}
              </div>
            )}

            <div className="flex gap-3">
              <button
                type="submit"
                disabled={submitting || streaming}
                className="flex-1 py-3.5 rounded-xl bg-brand-600 text-white font-semibold text-sm hover:bg-brand-800 transition disabled:opacity-60 flex items-center justify-center gap-2"
              >
                {submitting ? <><Spin /> جارٍ التوليد...</> : "توليد المحتوى للتقييم"}
              </button>
              <button
                type="button"
                onClick={handleSubmit(handleStream)}
                disabled={streaming || submitting}
                className="px-4 py-2 rounded-xl bg-[#355BA7] text-white text-sm font-medium hover:bg-[#283481] disabled:opacity-50 transition-colors flex items-center justify-center gap-2"
              >
                {streaming ? (
                  <span className="flex items-center gap-2">
                    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                    </svg>
                    جارٍ التوليد...
                  </span>
                ) : "معاينة سريعة"}
              </button>
            </div>

            {submitting && (
              <p className="text-center text-xs text-gray-400">
                جارٍ توليد المحتوى... (15-30 ثانية)
              </p>
            )}
          </form>
        </div>

        {streamError && (
          <div className="bg-red-50 border border-red-200 rounded-xl p-4 text-right text-red-700 text-sm mt-4">
            {streamError}
          </div>
        )}

        {streamResult && !streaming && (
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6 mt-4">
            <h2 className="text-right text-lg font-semibold text-[#283481] mb-4">
              نتيجة المعاينة السريعة
            </h2>
            <ArticleContentViewer
              riskLevel={streamResult.risk_assessment?.risk_level ?? null}
              summaryArticle={streamResult.summary_article ?? null}
              articleOutlines={streamResult.article_outlines ?? []}
              mode="test-scenario"
            />
          </div>
        )}

      </main>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function parseGeneratedContent(json: string | null): Stage1Output | null {
  if (!json) return null;
  try { return JSON.parse(json) as Stage1Output; } catch { return null; }
}

const inp = (err: boolean) =>
  `w-full px-4 py-3 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 transition bg-gray-50 ${
    err ? "border-red-300 bg-red-50" : "border-gray-200"
  }`;

const Spin = () => (
  <span className="inline-block w-4 h-4 border-2 border-current/30 border-t-current rounded-full animate-spin flex-shrink-0" />
);

function F({
  label, hint, req, err, children,
}: {
  label: string; hint?: string; req?: boolean; err?: string; children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {label}
        {req  && <span className="text-red-500 mr-1">*</span>}
        {hint && <span className="text-gray-400 font-normal text-xs mr-2">{hint}</span>}
      </label>
      {children}
      {err && <p className="text-red-600 text-xs mt-1">{err}</p>}
    </div>
  );
}
