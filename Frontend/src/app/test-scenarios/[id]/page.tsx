"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter, useParams } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { testScenarioApi, formatRelativeTime } from "@/services/api";
import NavBar from "@/components/NavBar";
import ArticleContentViewer from "@/components/ArticleContentViewer";
import type {
  TestScenarioResponse,
  SubmitEvaluationRequest,
  ContentEvaluationResponse,
  Stage1Output,
} from "@/types";

// ── Constants ─────────────────────────────────────────────────────────────────

const PATIENT_LABELS: Record<string, string> = {
  primaryDiagnosis:    "التشخيص الرئيسي",
  ageGroup:            "الفئة العمرية",
  comorbidities:       "الأمراض المصاحبة",
  currentMedications:  "الأدوية الحالية",
  allergies:           "الحساسية الدوائية",
  medicalRestrictions: "القيود الطبية",
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function parseJson<T>(json: string | null): T | null {
  if (!json) return null;
  try { return JSON.parse(json) as T; } catch { return null; }
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function TestScenarioDetailPage() {
  const router = useRouter();
  const params = useParams();
  const scenarioId = params.id as string;
  const { isLoggedIn } = useAuthStore();

  const [scenario, setScenario] = useState<TestScenarioResponse | null>(null);
  const [loading,  setLoading]  = useState(true);
  const [error,    setError]    = useState<string | null>(null);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); }
  }, [isLoggedIn, router]);

  const fetchScenario = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await testScenarioApi.getTestScenario(scenarioId);
      if (res.success && res.data) {
        setScenario(res.data);
      } else {
        setError(res.error ?? "لم يتم العثور على السيناريو");
      }
    } catch {
      setError("خطأ في الاتصال بالخادم");
    } finally {
      setLoading(false);
    }
  }, [scenarioId]);

  useEffect(() => {
    if (isLoggedIn && scenarioId) fetchScenario();
  }, [isLoggedIn, scenarioId, fetchScenario]);

  if (!isLoggedIn) return null;

  return (
    <div className="min-h-screen flex flex-col bg-ink-50" dir="rtl">
      <NavBar />
      <main className="flex-1 max-w-3xl w-full mx-auto px-6 py-8">

        {/* Breadcrumb */}
        <div className="flex items-center gap-3 mb-6">
          <Link
            href="/test-scenarios"
            className="text-ink-400 hover:text-ink-700 text-sm transition"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            ← السيناريوهات
          </Link>
          <span className="text-ink-100">/</span>
          <span
            className="text-sm font-medium text-ink-700"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            تفاصيل السيناريو
          </span>
        </div>

        {loading && <SkeletonCards />}

        {!loading && error && (
          <div className="bg-white rounded-2xl border border-ink-100 px-8 py-12 text-center">
            <p
              className="text-red-600 text-sm mb-4"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {error}
            </p>
            <Link
              href="/test-scenarios"
              className="text-navy-600 text-sm font-medium hover:underline"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              ← العودة إلى السيناريوهات
            </Link>
          </div>
        )}

        {!loading && !error && scenario && (
          <div className="space-y-4">
            <PatientCard scenario={scenario} />
            <GeneratedContentCard scenario={scenario} />
            <EvaluationCard scenario={scenario} onEvaluated={fetchScenario} />
          </div>
        )}
      </main>
    </div>
  );
}

// ── Section 1: Patient Data ───────────────────────────────────────────────────

function PatientCard({ scenario }: { scenario: TestScenarioResponse }) {
  const patient = parseJson<Record<string, string>>(scenario.patientDataJson) ?? {};
  const orderedKeys = Object.keys(PATIENT_LABELS) as (keyof typeof PATIENT_LABELS)[];

  return (
    <Card title="بيانات المريض الافتراضي">
      <dl className="space-y-2">
        {orderedKeys.map((key) => {
          const val = patient[key];
          if (!val) return null;
          return (
            <div key={key} className="flex items-start gap-2 text-sm">
              <dt
                className="text-ink-400 min-w-[160px] shrink-0"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {PATIENT_LABELS[key]}:
              </dt>
              <dd
                className="text-ink-700"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {val}
              </dd>
            </div>
          );
        })}
      </dl>
    </Card>
  );
}

// ── Section 2: Generated Content ──────────────────────────────────────────────

function GeneratedContentCard({ scenario }: { scenario: TestScenarioResponse }) {
  const generated = parseJson<Stage1Output>(scenario.generatedContentJson);
  if (!generated) return null;

  const initialContent: Record<number, string> = (() => {
    if (!scenario.generatedArticlesJson) return {};
    try {
      const saved = JSON.parse(scenario.generatedArticlesJson);
      const result: Record<number, string> = {};
      Object.entries(saved).forEach(([key, value]) => {
        result[parseInt(key)] = value as string;
      });
      return result;
    } catch {
      return {};
    }
  })();

  return (
    <Card title="المحتوى المولّد">
      <ArticleContentViewer
        riskLevel={generated?.risk_assessment?.risk_level ?? null}
        summaryArticle={generated?.summary_article ?? null}
        articleOutlines={generated?.article_outlines ?? []}
        mode="test-scenario"
        initialContent={initialContent}
        onGenerate={async (index) => {
          if (initialContent[index]) {
            return initialContent[index];
          }
          return await testScenarioApi.generateScenarioArticle(scenario.scenarioId, index);
        }}
      />
    </Card>
  );
}

// ── Section 3: Evaluation ─────────────────────────────────────────────────────

function EvaluationCard({
  scenario,
  onEvaluated,
}: {
  scenario:    TestScenarioResponse;
  onEvaluated: () => void;
}) {
  if (scenario.evaluation) {
    return <EvaluationReadOnly evaluation={scenario.evaluation} />;
  }
  if (scenario.status === "Generated") {
    return <EvaluationForm scenarioId={scenario.scenarioId} onEvaluated={onEvaluated} />;
  }
  return (
    <Card title="تقييم المحتوى">
      <p
        className="text-ink-400 text-sm"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        المحتوى لم يُولَّد بعد.
      </p>
    </Card>
  );
}

// Read-only evaluation view
function EvaluationReadOnly({ evaluation: e }: { evaluation: ContentEvaluationResponse }) {
  return (
    <Card title="تقييم المحتوى">
      <div className="space-y-4">
        {/* Star ratings */}
        <div className="grid grid-cols-2 gap-3">
          {([
            ["الدقة الطبية",       e.accuracyRating],
            ["الوضوح والقراءة",    e.clarityRating],
            ["الصلة بالحالة",      e.relevanceRating],
            ["الشمولية",           e.completenessRating],
          ] as [string, number][]).map(([label, rating]) => (
            <div key={label}>
              <p
                className="text-xs text-ink-400 mb-0.5"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {label}
              </p>
              <StarDisplay rating={rating} />
            </div>
          ))}
        </div>

        {/* Boolean badges */}
        <div className="flex flex-wrap gap-2">
          <BoolBadge label="مناسب للمريض"       value={e.isAppropriate}         />
          <BoolBadge label="مراعٍ للسياق اليمني" value={e.isCulturallySensitive} />
          <BoolBadge label="جودة اللغة مقبولة"   value={e.isArabicQuality}       />
        </div>

        {/* Text feedback */}
        {e.whatWorked && (
          <TextBlock label="ما الذي نجح؟" value={e.whatWorked} />
        )}
        {e.needsImprovement && (
          <TextBlock label="ما يحتاج تحسيناً" value={e.needsImprovement} />
        )}
        {e.comments && (
          <TextBlock label="ملاحظات" value={e.comments} />
        )}

        <p
          className="text-xs text-ink-400"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          تم التقييم في: {formatRelativeTime(e.submittedAt)}
        </p>
      </div>
    </Card>
  );
}

// Interactive evaluation form
function EvaluationForm({
  scenarioId,
  onEvaluated,
}: {
  scenarioId:  string;
  onEvaluated: () => void;
}) {
  const [ratings, setRatings] = useState({
    accuracyRating:     0,
    clarityRating:      0,
    relevanceRating:    0,
    completenessRating: 0,
  });
  const [bools, setBools] = useState({
    isAppropriate:         true,
    isCulturallySensitive: true,
    isArabicQuality:       true,
  });
  const [texts, setTexts] = useState({
    whatWorked:       "",
    needsImprovement: "",
    comments:         "",
  });
  const [submitting, setSubmitting] = useState(false);
  const [error,      setError]      = useState<string | null>(null);

  const handleSubmit = async () => {
    const allSet = Object.values(ratings).every((r) => r > 0);
    if (!allSet) { setError("يرجى تقييم جميع المعايير (1-5 نجوم)"); return; }
    setError(null);
    setSubmitting(true);
    try {
      const req: SubmitEvaluationRequest = {
        ...ratings,
        ...bools,
        whatWorked:       texts.whatWorked       || undefined,
        needsImprovement: texts.needsImprovement || undefined,
        comments:         texts.comments         || undefined,
      };
      const res = await testScenarioApi.submitEvaluation(scenarioId, req);
      if (res.success) {
        onEvaluated();
      } else {
        setError(res.error ?? "حدث خطأ أثناء الإرسال");
      }
    } catch {
      setError("خطأ في الاتصال بالخادم");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="تقييم المحتوى">
      <div className="space-y-5">
        {/* Star ratings */}
        <div className="space-y-3">
          {([
            ["الدقة الطبية",      "accuracyRating"],
            ["الوضوح والقراءة",   "clarityRating"],
            ["الصلة بالحالة",     "relevanceRating"],
            ["الشمولية",          "completenessRating"],
          ] as [string, keyof typeof ratings][]).map(([label, key]) => (
            <div key={key} className="flex items-center gap-4">
              <span
                className="text-sm text-ink-700 min-w-[140px]"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {label}
              </span>
              <StarInput
                value={ratings[key]}
                onChange={(v) => setRatings((prev) => ({ ...prev, [key]: v }))}
              />
            </div>
          ))}
        </div>

        {/* Boolean toggles */}
        <div className="space-y-2.5">
          {([
            ["المحتوى مناسب للمريض؟",     "isAppropriate"],
            ["مراعٍ للسياق اليمني؟",      "isCulturallySensitive"],
            ["جودة اللغة العربية مقبولة؟", "isArabicQuality"],
          ] as [string, keyof typeof bools][]).map(([label, key]) => (
            <div key={key} className="flex items-center justify-between gap-4">
              <span
                className="text-sm text-ink-700"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {label}
              </span>
              <YesNoToggle
                value={bools[key]}
                onChange={(v) => setBools((prev) => ({ ...prev, [key]: v }))}
              />
            </div>
          ))}
        </div>

        {/* Text areas */}
        <TextareaField
          label="ما الذي نجح؟"
          value={texts.whatWorked}
          onChange={(v) => setTexts((p) => ({ ...p, whatWorked: v }))}
        />
        <TextareaField
          label="ما الذي يحتاج تحسيناً؟"
          value={texts.needsImprovement}
          onChange={(v) => setTexts((p) => ({ ...p, needsImprovement: v }))}
        />
        <TextareaField
          label="ملاحظات إضافية"
          value={texts.comments}
          onChange={(v) => setTexts((p) => ({ ...p, comments: v }))}
        />

        {error && (
          <div
            className="px-4 py-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            {error}
          </div>
        )}

        <button
          onClick={handleSubmit}
          disabled={submitting}
          className="w-full py-3 rounded-xl bg-navy-600 text-white font-semibold text-sm hover:bg-navy-700 transition disabled:opacity-60 flex items-center justify-center gap-2"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {submitting ? <><Spin /> جارٍ الإرسال...</> : "إرسال التقييم"}
        </button>
      </div>
    </Card>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function StarInput({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  const [hovered, setHovered] = useState(0);
  return (
    <div className="flex gap-1">
      {[1, 2, 3, 4, 5].map((n) => (
        <button
          key={n}
          type="button"
          onMouseEnter={() => setHovered(n)}
          onMouseLeave={() => setHovered(0)}
          onClick={() => onChange(n)}
          className="text-2xl leading-none transition-transform hover:scale-110 focus:outline-none"
        >
          <span className={(hovered || value) >= n ? "text-amber-400" : "text-ink-100"}>
            ★
          </span>
        </button>
      ))}
    </div>
  );
}

function StarDisplay({ rating }: { rating: number }) {
  return (
    <span className="font-mono text-amber-400 text-base tracking-wide">
      {"★".repeat(rating)}
      <span className="text-ink-100">{"★".repeat(5 - rating)}</span>
    </span>
  );
}

function YesNoToggle({ value, onChange }: { value: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex rounded-lg border border-ink-100 overflow-hidden text-sm">
      <button
        type="button"
        onClick={() => onChange(true)}
        className={`px-4 py-1.5 transition ${
          value ? "bg-green-500 text-white font-medium" : "bg-white text-ink-500 hover:bg-ink-50"
        }`}
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        نعم
      </button>
      <button
        type="button"
        onClick={() => onChange(false)}
        className={`px-4 py-1.5 border-r border-ink-100 transition ${
          !value ? "bg-red-500 text-white font-medium" : "bg-white text-ink-500 hover:bg-ink-50"
        }`}
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        لا
      </button>
    </div>
  );
}

function BoolBadge({ label, value }: { label: string; value: boolean }) {
  return (
    <span
      className={`inline-block px-3 py-1 rounded-full text-xs font-medium ${
        value
          ? "bg-green-50 text-green-700 border border-green-200"
          : "bg-red-50 text-red-700 border border-red-200"
      }`}
      style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
    >
      {value ? "✓" : "✗"} {label}
    </span>
  );
}

function TextBlock({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p
        className="text-xs font-medium text-ink-400 mb-1"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        {label}
      </p>
      <p
        className="text-sm text-ink-700 bg-ink-50 rounded-xl px-4 py-3 leading-relaxed"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        {value}
      </p>
    </div>
  );
}

function TextareaField({
  label, value, onChange,
}: {
  label: string; value: string; onChange: (v: string) => void;
}) {
  return (
    <div>
      <label
        className="block text-sm font-medium text-ink-700 mb-1"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        {label}
      </label>
      <textarea
        rows={2}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full px-4 py-3 rounded-xl border border-ink-100 bg-white text-sm focus:outline-none focus:ring-2 focus:ring-navy-400 transition resize-none"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      />
    </div>
  );
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-white rounded-2xl border border-ink-100 overflow-hidden">
      <div className="px-6 py-4 border-b border-ink-100">
        <h2
          className="font-semibold text-ink-900 text-sm"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {title}
        </h2>
      </div>
      <div className="px-6 py-5">{children}</div>
    </div>
  );
}

function Spin() {
  return (
    <span className="inline-block w-4 h-4 border-2 border-current/30 border-t-current rounded-full animate-spin flex-shrink-0" />
  );
}

function SkeletonCards() {
  return (
    <div className="space-y-4">
      {[1, 2, 3].map((n) => (
        <div key={n} className="bg-white rounded-2xl border border-ink-100 overflow-hidden animate-pulse">
          <div className="px-6 py-4 border-b border-ink-100">
            <div className="h-4 w-40 bg-ink-100 rounded" />
          </div>
          <div className="px-6 py-5 space-y-3">
            <div className="h-3 w-full bg-ink-100 rounded" />
            <div className="h-3 w-3/4 bg-ink-100 rounded" />
          </div>
        </div>
      ))}
    </div>
  );
}
