"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { testScenarioApi, formatRelativeTime } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { TestScenarioResponse } from "@/types";

// ── Helpers ───────────────────────────────────────────────────────────────────

function parsePatientData(json: string): Record<string, string> {
  try { return JSON.parse(json); } catch { return {}; }
}

function Stars({ rating, max = 5 }: { rating: number; max?: number }) {
  return (
    <span className="font-mono text-amber-400 text-sm tracking-wide">
      {"★".repeat(rating)}{"☆".repeat(max - rating)}
    </span>
  );
}

const getStatusLabel = (status: string) => {
  switch (status) {
    case 'Created':   return 'جديد'
    case 'Generated': return 'تم التوليد'
    case 'Evaluated': return 'تم التقييم'
    default:          return status
  }
}

const getStatusStyle = (status: string): React.CSSProperties => {
  switch (status) {
    case 'Evaluated':
      return { background: '#E6F4EC', color: '#197540' }
    case 'Generated':
      return { background: '#EEF1F7', color: '#1E3A72' }
    default:
      return { background: '#F6F7FB', color: '#5A6478' }
  }
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function TestScenariosPage() {
  const router = useRouter();
  const { isLoggedIn, hydrated } = useAuthStore();
  const [scenarios, setScenarios] = useState<TestScenarioResponse[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [error,     setError]     = useState<string | null>(null);

  useEffect(() => {
    if (hydrated && !isLoggedIn) { router.push("/login"); }
  }, [hydrated, isLoggedIn, router]);

  const fetchScenarios = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await testScenarioApi.getTestScenarios();
      if (res.success && res.data) {
        setScenarios(res.data);
      } else {
        setError(res.error ?? "تعذر تحميل السيناريوهات");
      }
    } catch {
      setError("خطأ في الاتصال بالخادم");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (hydrated && isLoggedIn) fetchScenarios();
  }, [hydrated, isLoggedIn, fetchScenarios]);

  if (!hydrated) return null;

  return (
    <div className="min-h-screen flex flex-col bg-ink-50" dir="rtl">
      <NavBar />

      <main className="flex-1 max-w-5xl w-full mx-auto px-6 py-8">

        {/* Page header */}
        <div className="flex items-center justify-between mb-6">
          <h1
            className="text-xl font-bold text-ink-900"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            سيناريوهات الاختبار
          </h1>
          <Link
            href="/test-scenarios/new"
            className="px-4 py-2 rounded-xl bg-navy-600 text-white text-sm font-semibold hover:bg-navy-700 transition"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            + سيناريو جديد
          </Link>
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl border border-ink-100 overflow-hidden">
          <div className="px-6 py-4 border-b border-ink-100 flex items-center justify-between">
            <h2
              className="font-semibold text-ink-900 text-sm"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              قائمة السيناريوهات
            </h2>
            <button
              onClick={fetchScenarios}
              className="text-navy-600 text-sm hover:underline"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              تحديث
            </button>
          </div>

          {loading ? (
            <div
              className="py-16 text-center text-ink-400 text-sm"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              جاري التحميل...
            </div>
          ) : error ? (
            <div className="py-16 text-center">
              <p
                className="text-red-500 text-sm mb-3"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {error}
              </p>
              <button
                onClick={fetchScenarios}
                className="text-navy-600 text-sm hover:underline"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                إعادة المحاولة
              </button>
            </div>
          ) : scenarios.length === 0 ? (
            <div className="py-16 text-center">
              <p
                className="text-ink-400 text-sm mb-4"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                لا توجد سيناريوهات بعد. أنشئ سيناريو اختبار جديد.
              </p>
              <Link
                href="/test-scenarios/new"
                className="text-navy-600 text-sm font-medium hover:underline"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                + سيناريو جديد
              </Link>
            </div>
          ) : (
            <div className="divide-y divide-ink-100">
              {scenarios.map((s) => (
                <ScenarioCard key={s.scenarioId} scenario={s} />
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}

// ── Scenario card ─────────────────────────────────────────────────────────────

function ScenarioCard({ scenario: s }: { scenario: TestScenarioResponse }) {
  const patient = parsePatientData(s.patientDataJson);

  return (
    <div className="px-6 py-4 hover:bg-ink-50 transition">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          {/* Diagnosis */}
          <p
            className="font-medium text-ink-900 text-sm mb-1 truncate"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            {patient.primaryDiagnosis ?? "—"}
          </p>

          {/* Meta row */}
          <div className="flex items-center gap-2 flex-wrap">
            {patient.ageGroup && (
              <span
                className="text-xs text-ink-400"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {patient.ageGroup}
              </span>
            )}
            <span
              className="inline-block px-2 py-0.5 rounded-full text-xs font-medium"
              style={{ ...getStatusStyle(s.status), fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {getStatusLabel(s.status)}
            </span>
            <span
              className="text-xs text-ink-400"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {formatRelativeTime(s.createdAt)}
            </span>
          </div>

          {/* Star rating summary */}
          {s.evaluation && (
            <div className="mt-1.5 flex items-center gap-1.5">
              <span
                className="text-xs text-ink-500"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                دقة طبية:
              </span>
              <Stars rating={s.evaluation.accuracyRating} />
            </div>
          )}
        </div>

        {/* Action */}
        <Link
          href={`/test-scenarios/${s.scenarioId}`}
          className="shrink-0 text-xs font-semibold px-3 py-1.5 rounded-lg transition"
          style={{ background: '#EEF1F7', color: '#1E3A72', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          عرض التفاصيل
        </Link>
      </div>
    </div>
  );
}
