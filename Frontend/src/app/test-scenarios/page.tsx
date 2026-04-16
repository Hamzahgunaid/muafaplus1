"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { testScenarioApi, formatRelativeTime } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { TestScenarioResponse } from "@/types";

// ── Constants ─────────────────────────────────────────────────────────────────

const STATUS_BADGE: Record<string, { label: string; cls: string }> = {
  Created:   { label: "جديد",        cls: "bg-gray-100  text-gray-600"  },
  Generated: { label: "تم التوليد",  cls: "bg-blue-50   text-blue-700"  },
  Evaluated: { label: "تم التقييم",  cls: "bg-green-50  text-green-700" },
};

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

// ── Page ──────────────────────────────────────────────────────────────────────

export default function TestScenariosPage() {
  const router = useRouter();
  const { isLoggedIn } = useAuthStore();
  const [scenarios, setScenarios] = useState<TestScenarioResponse[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [error,     setError]     = useState<string | null>(null);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); }
  }, [isLoggedIn, router]);

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
    if (isLoggedIn) fetchScenarios();
  }, [isLoggedIn, fetchScenarios]);

  if (!isLoggedIn) return null;

  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />

      <main className="flex-1 max-w-5xl w-full mx-auto px-6 py-8">

        {/* Page header */}
        <div className="flex items-center justify-between mb-6">
          <h1 className="text-xl font-bold text-gray-900">سيناريوهات الاختبار</h1>
          <Link
            href="/test-scenarios/new"
            className="px-4 py-2 rounded-xl bg-brand-600 text-white text-sm font-medium hover:bg-brand-800 transition"
          >
            + سيناريو جديد
          </Link>
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-50 flex items-center justify-between">
            <h2 className="font-semibold text-gray-800">قائمة السيناريوهات</h2>
            <button onClick={fetchScenarios} className="text-brand-600 text-sm hover:underline">
              تحديث
            </button>
          </div>

          {loading ? (
            <div className="py-16 text-center text-gray-400 text-sm">جاري التحميل...</div>
          ) : error ? (
            <div className="py-16 text-center">
              <p className="text-red-500 text-sm mb-3">{error}</p>
              <button onClick={fetchScenarios} className="text-brand-600 text-sm hover:underline">
                إعادة المحاولة
              </button>
            </div>
          ) : scenarios.length === 0 ? (
            <div className="py-16 text-center">
              <p className="text-gray-400 text-sm mb-4">
                لا توجد سيناريوهات بعد. أنشئ سيناريو اختبار جديد.
              </p>
              <Link href="/test-scenarios/new" className="text-brand-600 text-sm font-medium hover:underline">
                + سيناريو جديد
              </Link>
            </div>
          ) : (
            <div className="divide-y divide-gray-50">
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
  const patient  = parsePatientData(s.patientDataJson);
  const badge    = STATUS_BADGE[s.status] ?? STATUS_BADGE["Created"];

  return (
    <div className="px-6 py-4 hover:bg-gray-50 transition">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          {/* Diagnosis */}
          <p className="font-medium text-gray-900 text-sm mb-1 truncate">
            {patient.primaryDiagnosis ?? "—"}
          </p>

          {/* Meta row */}
          <div className="flex items-center gap-2 flex-wrap">
            {patient.ageGroup && (
              <span className="text-xs text-gray-400">{patient.ageGroup}</span>
            )}
            <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${badge.cls}`}>
              {badge.label}
            </span>
            <span className="text-xs text-gray-400">{formatRelativeTime(s.createdAt)}</span>
          </div>

          {/* Star rating summary */}
          {s.evaluation && (
            <div className="mt-1.5 flex items-center gap-1.5">
              <span className="text-xs text-gray-500">دقة طبية:</span>
              <Stars rating={s.evaluation.accuracyRating} />
            </div>
          )}
        </div>

        {/* Action */}
        <Link
          href={`/test-scenarios/${s.scenarioId}`}
          className="shrink-0 text-brand-600 text-xs font-medium hover:underline"
        >
          عرض التفاصيل
        </Link>
      </div>
    </div>
  );
}
