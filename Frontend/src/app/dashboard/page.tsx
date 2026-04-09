"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { physicianApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { SessionSummary, RiskLevel } from "@/types";

const RISK_LABELS: Record<RiskLevel, string> = {
  LOW:      "منخفض",
  MODERATE: "متوسط",
  HIGH:     "مرتفع",
  CRITICAL: "حرج",
};

const RISK_CLASS: Record<RiskLevel, string> = {
  LOW:      "risk-low",
  MODERATE: "risk-moderate",
  HIGH:     "risk-high",
  CRITICAL: "risk-critical",
};

const STATUS_LABELS: Record<string, string> = {
  pending:     "قيد الانتظار",
  in_progress: "جارٍ التوليد",
  complete:    "مكتمل",
  failed:      "فشل",
};

export default function DashboardPage() {
  const router    = useRouter();
  const { isLoggedIn, physician, role } = useAuthStore();
  const [sessions, setSessions]   = useState<SessionSummary[]>([]);
  const [loading, setLoading]     = useState(true);
  const [page, setPage]           = useState(1);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    if (role === "SuperAdmin" || role === "HospitalAdmin") {
      router.push("/admin"); return;
    }
  }, [isLoggedIn, role, router]);

  const fetchSessions = useCallback(async () => {
    if (!physician) return;
    setLoading(true);
    try {
      const res = await physicianApi.getSessions(physician.physicianId, page);
      if (res.success && res.data) setSessions(res.data);
    } finally {
      setLoading(false);
    }
  }, [physician, page]);

  useEffect(() => { fetchSessions(); }, [fetchSessions]);

  if (!isLoggedIn || !physician) return null;
  if (role === "SuperAdmin" || role === "HospitalAdmin") return null;

  return (
    <div className="min-h-screen flex flex-col">

      {/* ── Top bar ─────────────────────────────────────────────────────────── */}
      <NavBar />

      {/* ── Main ────────────────────────────────────────────────────────────── */}
      <main className="flex-1 max-w-5xl w-full mx-auto px-6 py-8">

        {/* Stats row */}
        <div className="grid grid-cols-3 gap-4 mb-8">
          <StatCard label="إجمالي الجلسات" value={physician.totalSessions.toString()} />
          <StatCard label="المكتملة"        value={sessions.filter(s => s.status === "complete").length.toString()} />
          <StatCard
            label="التكلفة الإجمالية"
            value={`$${sessions.reduce((acc, s) => acc + (s.totalCost ?? 0), 0).toFixed(3)}`}
          />
        </div>

        {/* Sessions table */}
        <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-50 flex items-center justify-between">
            <h2 className="font-semibold text-gray-800">سجل الجلسات</h2>
            <button onClick={fetchSessions} className="text-brand-600 text-sm hover:underline">
              تحديث
            </button>
          </div>

          {loading ? (
            <div className="py-16 text-center text-gray-400 text-sm">جاري التحميل...</div>
          ) : sessions.length === 0 ? (
            <div className="py-16 text-center">
              <p className="text-gray-400 text-sm mb-4">لا توجد جلسات بعد</p>
              <Link href="/generate" className="text-brand-600 text-sm font-medium hover:underline">
                إنشاء أول جلسة
              </Link>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 text-gray-500 text-xs">
                    <th className="px-6 py-3 text-right font-medium">رقم المريض</th>
                    <th className="px-6 py-3 text-right font-medium">مستوى الخطر</th>
                    <th className="px-6 py-3 text-right font-medium">المقالات</th>
                    <th className="px-6 py-3 text-right font-medium">الحالة</th>
                    <th className="px-6 py-3 text-right font-medium">التكلفة</th>
                    <th className="px-6 py-3 text-right font-medium">التاريخ</th>
                    <th className="px-6 py-3 text-right font-medium"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {sessions.map((s) => (
                    <tr key={s.sessionId} className="hover:bg-gray-50 transition">
                      <td className="px-6 py-4 text-gray-600 font-mono text-xs">{s.patientId.slice(0, 8)}…</td>
                      <td className="px-6 py-4">
                        {s.riskLevel ? (
                          <span className={`inline-block px-2 py-0.5 rounded-full text-xs border font-medium ${RISK_CLASS[s.riskLevel]}`}>
                            {RISK_LABELS[s.riskLevel]}
                          </span>
                        ) : "—"}
                      </td>
                      <td className="px-6 py-4 text-gray-600">{s.totalArticles ?? "—"}</td>
                      <td className="px-6 py-4">
                        <span className={`text-xs font-medium ${s.status === "complete" ? "text-brand-600" : s.status === "failed" ? "text-red-600" : "text-amber-600"}`}>
                          {STATUS_LABELS[s.status] ?? s.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-gray-600">{s.totalCost ? `$${s.totalCost.toFixed(3)}` : "—"}</td>
                      <td className="px-6 py-4 text-gray-400 text-xs">
                        {new Date(s.startedAt).toLocaleDateString("ar-YE")}
                      </td>
                      <td className="px-6 py-4">
                        <Link
                          href={`/sessions/${s.sessionId}`}
                          className="text-brand-600 text-xs hover:underline"
                        >
                          عرض
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {sessions.length >= 20 && (
            <div className="px-6 py-4 border-t border-gray-50 flex justify-between">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="text-sm text-gray-500 disabled:opacity-40 hover:text-brand-600 transition"
              >
                السابق
              </button>
              <span className="text-xs text-gray-400">صفحة {page}</span>
              <button
                onClick={() => setPage((p) => p + 1)}
                className="text-sm text-gray-500 hover:text-brand-600 transition"
              >
                التالي
              </button>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white rounded-2xl border border-gray-100 px-6 py-5">
      <p className="text-xs text-gray-400 mb-1">{label}</p>
      <p className="text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}
