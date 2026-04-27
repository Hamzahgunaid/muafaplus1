"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { physicianApi, referralApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { SessionSummary, RiskLevel, ReferralResponse } from "@/types";

const RISK_LABELS: Record<RiskLevel, string> = {
  LOW: "منخفض", MODERATE: "متوسط", HIGH: "مرتفع", CRITICAL: "حرج",
};

const STATUS_LABELS: Record<string, string> = {
  pending:           "قيد الانتظار",
  in_progress:       "جارٍ التوليد",
  complete:          "مكتمل",
  failed:            "فشل",
  Created:           "تم الإنشاء",
  Stage1Complete:    "مكتمل المرحلة 1",
  Stage1Delivered:   "تم الإرسال",
  Stage2Requested:   "جارٍ التوليد",
  Stage2Complete:    "مكتمل",
  FeedbackSubmitted: "تم التقييم",
};

const RISK_BADGE_STYLE: Record<string, { background: string; color: string }> = {
  LOW:      { background: "#E6F4EC", color: "#197540" },
  MODERATE: { background: "#FDF3E1", color: "#B8771F" },
  HIGH:     { background: "#FDECE2", color: "#D85A30" },
  CRITICAL: { background: "#FBE5E5", color: "#D64545" },
};

export default function DashboardPage() {
  const router = useRouter();
  const { isLoggedIn, physician, fullName, role } = useAuthStore();
  const [sessions,  setSessions]  = useState<SessionSummary[]>([]);
  const [referrals, setReferrals] = useState<ReferralResponse[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [page,      setPage]      = useState(1);

  const isAdmin             = role === "SuperAdmin" || role === "HospitalAdmin";
  const isPhysicianOrAssist = role === "Physician"  || role === "Assistant";

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); }
  }, [isLoggedIn, router]);

  const fetchReferrals = useCallback(async () => {
    if (!isAdmin) return;
    setLoading(true);
    try {
      const res = await referralApi.getReferrals();
      if (res.success && res.data) setReferrals(res.data);
    } finally {
      setLoading(false);
    }
  }, [isAdmin]);

  const fetchSessions = useCallback(async () => {
    if (!isPhysicianOrAssist || !physician) return;
    setLoading(true);
    try {
      const res = await physicianApi.getSessions(physician.physicianId, page);
      if (res.success && res.data) setSessions(res.data);
    } finally {
      setLoading(false);
    }
  }, [isPhysicianOrAssist, physician, page]);

  useEffect(() => {
    if (isLoggedIn) {
      if (isAdmin) fetchReferrals();
      else fetchSessions();
    }
  }, [isLoggedIn, isAdmin, fetchReferrals, fetchSessions]);

  if (!isLoggedIn) return null;

  const displayName = fullName ?? physician?.fullName ?? "الطبيب";

  // ── Admin dashboard ───────────────────────────────────────────────────────
  if (isAdmin) {
    const completed = referrals.filter(
      (r) => r.status === "Stage1Complete" || r.status === "Stage2Complete"
    ).length;

    return (
      <div className="min-h-screen" style={{ backgroundColor: "#F6F7FB", fontFamily: "IBM Plex Sans Arabic, system-ui" }} dir="rtl">
        <NavBar />
        <div className="max-w-7xl mx-auto px-6 py-8">

          {/* Header */}
          <div className="flex items-center justify-between mb-8">
            <div>
              <div
                className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-semibold mb-2"
                style={{ background: "#E6F4EC", color: "#197540" }}
              >
                <div className="w-1.5 h-1.5 rounded-full bg-green-500" />
                متصل بالنظام
              </div>
              <h1 className="text-2xl font-bold" style={{ color: "#0E1726" }}>
                مرحباً، {displayName} 👋
              </h1>
              <p className="text-sm mt-1" style={{ color: "#5A6478" }}>
                لوحة تحكم المدير — منصة معافى+
              </p>
            </div>
            <Link
              href="/referrals/new"
              className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-semibold text-white"
              style={{ background: "#1E3A72" }}
            >
              + إحالة جديدة
            </Link>
          </div>

          {/* Stat cards */}
          <div className="grid grid-cols-3 gap-4 mb-8">
            {[
              { label: "إجمالي الإحالات", value: referrals.length, icon: "📋", color: "#1E3A72", bg: "#EEF1F7" },
              { label: "المكتملة",         value: completed,        icon: "✅", color: "#197540", bg: "#E6F4EC" },
              { label: "قيد المعالجة",     value: referrals.length - completed, icon: "⏳", color: "#B8771F", bg: "#FDF3E1" },
            ].map((stat, i) => (
              <div key={i} className="rounded-2xl p-5" style={{ background: "white", border: "1px solid #EEF0F5" }}>
                <div className="flex items-center justify-between mb-3">
                  <div className="w-10 h-10 rounded-xl flex items-center justify-center text-lg" style={{ background: stat.bg }}>
                    {stat.icon}
                  </div>
                  <span className="text-xs font-semibold" style={{ color: "#8A93A6" }}>{stat.label}</span>
                </div>
                <div className="text-3xl font-bold" style={{ color: stat.color }}>{stat.value}</div>
              </div>
            ))}
          </div>

          {/* Referrals table */}
          <div className="rounded-2xl overflow-hidden" style={{ background: "white", border: "1px solid #EEF0F5" }}>
            <div className="flex items-center justify-between px-6 py-4" style={{ borderBottom: "1px solid #EEF0F5" }}>
              <h2 className="font-bold text-base" style={{ color: "#0E1726" }}>الإحالات</h2>
              <button
                onClick={fetchReferrals}
                className="text-xs px-3 py-1.5 rounded-lg"
                style={{ background: "#F6F7FB", color: "#5A6478", border: "1px solid #EEF0F5" }}
              >
                تحديث
              </button>
            </div>

            {loading ? (
              <div className="flex items-center justify-center py-16">
                <div className="w-8 h-8 rounded-full border-2 animate-spin" style={{ borderColor: "#1E3A72", borderTopColor: "transparent" }} />
              </div>
            ) : referrals.length === 0 ? (
              <div className="py-16 text-center">
                <p className="text-sm mb-4" style={{ color: "#8A93A6" }}>لا توجد إحالات بعد</p>
                <Link href="/referrals/new" className="text-sm font-medium" style={{ color: "#1E3A72" }}>
                  إنشاء أول إحالة
                </Link>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr style={{ background: "#F6F7FB" }}>
                      {["رمز الإحالة", "مستوى الخطر", "الحالة", "التاريخ", ""].map((h, i) => (
                        <th key={i} className="px-6 py-3 text-right text-xs font-semibold" style={{ color: "#8A93A6" }}>{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {referrals.map((r) => (
                      <tr key={r.referralId} className="hover:bg-gray-50 transition" style={{ borderTop: "1px solid #EEF0F5" }}>
                        <td className="px-6 py-3 font-mono text-xs" style={{ color: "#5A6478" }}>{r.referralCode}</td>
                        <td className="px-6 py-3">
                          {r.riskLevel && RISK_BADGE_STYLE[r.riskLevel] ? (
                            <span className="px-2.5 py-1 rounded-full text-xs font-semibold" style={RISK_BADGE_STYLE[r.riskLevel]}>
                              {RISK_LABELS[r.riskLevel as RiskLevel] ?? r.riskLevel}
                            </span>
                          ) : "—"}
                        </td>
                        <td className="px-6 py-3">
                          <span className="text-xs" style={{ color: "#5A6478" }}>
                            {STATUS_LABELS[r.status] ?? r.status}
                          </span>
                        </td>
                        <td className="px-6 py-3 text-xs" style={{ color: "#8A93A6" }}>
                          {new Date(r.createdAt).toLocaleDateString("ar-YE")}
                        </td>
                        <td className="px-6 py-3">
                          <Link
                            href={`/referrals/${r.referralId}`}
                            className="text-xs px-3 py-1.5 rounded-lg font-medium"
                            style={{ background: "#EEF1F7", color: "#1E3A72" }}
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

            {referrals.length >= 20 && (
              <div className="px-6 py-4 flex justify-between" style={{ borderTop: "1px solid #EEF0F5" }}>
                <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}
                  className="text-sm disabled:opacity-40 transition" style={{ color: "#1E3A72" }}>
                  السابق
                </button>
                <span className="text-xs" style={{ color: "#8A93A6" }}>صفحة {page}</span>
                <button onClick={() => setPage((p) => p + 1)}
                  className="text-sm transition" style={{ color: "#1E3A72" }}>
                  التالي
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  // ── Physician / Assistant dashboard ───────────────────────────────────────
  const stats = {
    total:     sessions.length,
    completed: sessions.filter((s) => s.status === "complete" || s.status === "Completed").length,
    articles:  sessions.reduce((sum, s) => sum + (s.totalArticles ?? 0), 0),
    cost:      sessions.reduce((sum, s) => sum + (s.totalCost ?? 0), 0),
  };

  return (
    <div className="min-h-screen" style={{ backgroundColor: "#F6F7FB", fontFamily: "IBM Plex Sans Arabic, system-ui" }} dir="rtl">
      <NavBar />

      <div className="max-w-7xl mx-auto px-6 py-8">

        {/* Header row */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <div
              className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-semibold mb-2"
              style={{ background: "#E6F4EC", color: "#197540" }}
            >
              <div className="w-1.5 h-1.5 rounded-full bg-green-500" />
              متصل بالنظام
            </div>
            <h1 className="text-2xl font-bold" style={{ color: "#0E1726" }}>
              مرحباً، {displayName} 👋
            </h1>
            <p className="text-sm mt-1" style={{ color: "#5A6478" }}>
              هذا ملخص نشاطك الطبي على منصة معافى+
            </p>
          </div>
          <button
            onClick={() => router.push("/referrals/new")}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-semibold text-white transition-all"
            style={{ background: "#1E3A72" }}
          >
            + إحالة جديدة
          </button>
        </div>

        {/* 4-stat grid */}
        <div className="grid grid-cols-4 gap-4 mb-8">
          {[
            { label: "إجمالي الجلسات",   value: stats.total,                  icon: "📋", color: "#1E3A72", bg: "#EEF1F7" },
            { label: "الجلسات المكتملة", value: stats.completed,              icon: "✅", color: "#197540", bg: "#E6F4EC" },
            { label: "المقالات المولّدة", value: stats.articles,              icon: "📄", color: "#E87A2F", bg: "#FDF3E1" },
            { label: "التكلفة الإجمالية", value: `$${stats.cost.toFixed(3)}`, icon: "💰", color: "#1E3A72", bg: "#EEF1F7" },
          ].map((stat, i) => (
            <div key={i} className="rounded-2xl p-5" style={{ background: "white", border: "1px solid #EEF0F5" }}>
              <div className="flex items-center justify-between mb-3">
                <div className="w-10 h-10 rounded-xl flex items-center justify-center text-lg" style={{ background: stat.bg }}>
                  {stat.icon}
                </div>
                <span className="text-xs font-semibold" style={{ color: "#8A93A6" }}>{stat.label}</span>
              </div>
              <div className="text-3xl font-bold" style={{ color: stat.color }}>{stat.value}</div>
            </div>
          ))}
        </div>

        {/* Main content grid */}
        <div className="grid gap-6" style={{ gridTemplateColumns: "1fr 340px" }}>

          {/* Sessions table */}
          <div className="rounded-2xl overflow-hidden" style={{ background: "white", border: "1px solid #EEF0F5" }}>
            <div className="flex items-center justify-between px-6 py-4" style={{ borderBottom: "1px solid #EEF0F5" }}>
              <h2 className="font-bold text-base" style={{ color: "#0E1726" }}>سجل الجلسات</h2>
              <button
                onClick={fetchSessions}
                className="text-xs px-3 py-1.5 rounded-lg"
                style={{ background: "#F6F7FB", color: "#5A6478", border: "1px solid #EEF0F5" }}
              >
                تحديث
              </button>
            </div>

            {loading ? (
              <div className="flex items-center justify-center py-16">
                <div className="w-8 h-8 rounded-full border-2 animate-spin"
                  style={{ borderColor: "#1E3A72", borderTopColor: "transparent" }} />
              </div>
            ) : sessions.length === 0 ? (
              <div className="py-16 text-center">
                <p className="text-sm mb-4" style={{ color: "#8A93A6" }}>لا توجد جلسات بعد</p>
                <Link href="/referrals/new" className="text-sm font-medium" style={{ color: "#1E3A72" }}>
                  إنشاء أول إحالة
                </Link>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr style={{ background: "#F6F7FB" }}>
                      {["رقم المريض", "مستوى الخطر", "المقالات", "الحالة", "التكلفة", "التاريخ", ""].map((h, i) => (
                        <th key={i} className="px-4 py-3 text-right text-xs font-semibold" style={{ color: "#8A93A6" }}>{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {sessions.map((s, i) => (
                      <tr key={s.sessionId ?? i} className="transition-colors hover:bg-gray-50"
                        style={{ borderTop: "1px solid #EEF0F5" }}>
                        <td className="px-4 py-3 text-sm font-mono" style={{ color: "#0E1726" }}>
                          {(s.patientId ?? "").slice(0, 8)}…
                        </td>
                        <td className="px-4 py-3">
                          {s.riskLevel && RISK_BADGE_STYLE[s.riskLevel] ? (
                            <span className="px-2.5 py-1 rounded-full text-xs font-semibold" style={RISK_BADGE_STYLE[s.riskLevel]}>
                              {RISK_LABELS[s.riskLevel]}
                            </span>
                          ) : "—"}
                        </td>
                        <td className="px-4 py-3 text-sm text-center" style={{ color: "#0E1726" }}>
                          {s.totalArticles ?? 0}
                        </td>
                        <td className="px-4 py-3">
                          <span className="px-2.5 py-1 rounded-full text-xs font-semibold"
                            style={{
                              background: (s.status === "complete" || s.status === "Completed") ? "#E6F4EC" : "#EEF1F7",
                              color:      (s.status === "complete" || s.status === "Completed") ? "#197540"  : "#1E3A72",
                            }}>
                            {STATUS_LABELS[s.status] ?? s.status}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-sm" style={{ color: "#0E1726" }}>
                          {s.totalCost ? `$${s.totalCost.toFixed(3)}` : "—"}
                        </td>
                        <td className="px-4 py-3 text-sm" style={{ color: "#5A6478" }}>
                          {s.startedAt ? new Date(s.startedAt).toLocaleDateString("ar-YE") : "—"}
                        </td>
                        <td className="px-4 py-3">
                          <button
                            onClick={() => router.push(`/sessions/${s.sessionId}`)}
                            className="text-xs px-3 py-1.5 rounded-lg font-medium transition-colors"
                            style={{ background: "#EEF1F7", color: "#1E3A72" }}
                          >
                            عرض
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {sessions.length >= 20 && (
              <div className="px-6 py-4 flex justify-between" style={{ borderTop: "1px solid #EEF0F5" }}>
                <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}
                  className="text-sm disabled:opacity-40" style={{ color: "#1E3A72" }}>
                  السابق
                </button>
                <span className="text-xs" style={{ color: "#8A93A6" }}>صفحة {page}</span>
                <button onClick={() => setPage((p) => p + 1)} className="text-sm" style={{ color: "#1E3A72" }}>
                  التالي
                </button>
              </div>
            )}
          </div>

          {/* Right column */}
          <div className="flex flex-col gap-4">

            {/* AI Activity card */}
            <div className="rounded-2xl p-6 text-white" style={{ background: "linear-gradient(135deg, #1E3A72, #11254A)" }}>
              <div className="flex items-center gap-2 mb-4">
                <div className="w-2 h-2 rounded-full bg-orange-400 animate-pulse" />
                <span className="text-xs font-semibold" style={{ color: "rgba(255,255,255,0.7)" }}>
                  نشاط الذكاء الاصطناعي
                </span>
              </div>
              <svg viewBox="0 0 200 40" className="w-full h-8 mb-4">
                <polyline
                  points="0,20 25,20 35,5 42,35 50,10 58,20 85,20 95,5 102,35 110,10 118,20 145,20 155,5 162,35 170,10 178,20 200,20"
                  fill="none" stroke="#50B2E6" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"
                />
              </svg>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <div className="text-2xl font-bold">{stats.articles}</div>
                  <div className="text-xs" style={{ color: "rgba(255,255,255,0.55)" }}>مقال مولَّد</div>
                </div>
                <div>
                  <div className="text-2xl font-bold">${stats.cost.toFixed(2)}</div>
                  <div className="text-xs" style={{ color: "rgba(255,255,255,0.55)" }}>تكلفة الذكاء</div>
                </div>
              </div>
            </div>

            {/* Quick actions */}
            <div className="rounded-2xl p-5" style={{ background: "white", border: "1px solid #EEF0F5" }}>
              <h3 className="font-bold text-sm mb-4" style={{ color: "#0E1726" }}>إجراءات سريعة</h3>
              <div className="flex flex-col gap-2">
                {[
                  { label: "+ إحالة جديدة",           href: "/referrals/new",       primary: true  },
                  { label: "سيناريو اختبار جديد",      href: "/test-scenarios/new",  primary: false },
                  { label: "عرض كل الإحالات",          href: "/referrals",           primary: false },
                ].map((action, i) => (
                  <button key={i}
                    onClick={() => router.push(action.href)}
                    className="w-full py-2.5 px-4 rounded-xl text-sm font-semibold text-right transition-all"
                    style={{
                      background: action.primary ? "#1E3A72" : "#F6F7FB",
                      color:      action.primary ? "white"   : "#1E3A72",
                      border:     action.primary ? "none"    : "1px solid #EEF0F5",
                      fontFamily: "IBM Plex Sans Arabic, system-ui",
                    }}
                  >
                    {action.label}
                  </button>
                ))}
              </div>
            </div>

          </div>
        </div>
      </div>
    </div>
  );
}
