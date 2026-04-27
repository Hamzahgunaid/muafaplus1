"use client";
import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import ReactMarkdown from "react-markdown";
import { useAuthStore } from "@/lib/store";
import { sessionApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { SessionDetail, ArticleRecord, RiskLevel } from "@/types";

const RISK_LABELS: Record<RiskLevel, string> = {
  LOW:      "منخفض",
  MODERATE: "متوسط",
  HIGH:     "مرتفع",
  CRITICAL: "حرج",
};

const RISK_STYLE: Record<RiskLevel, React.CSSProperties> = {
  LOW:      { background: '#E6F4EC', color: '#197540', border: '1px solid #C1E3CD' },
  MODERATE: { background: '#FFF8E6', color: '#BA7517', border: '1px solid #F5DFA0' },
  HIGH:     { background: '#FFF0E6', color: '#D85A30', border: '1px solid #F5C6A0' },
  CRITICAL: { background: '#FBE5E5', color: '#D64545', border: '1px solid #F5B8B8' },
};

const COV: Record<string, string> = {
  understanding_condition: "فهم الحالة",
  medication_management:   "إدارة الأدوية",
  safety_preparedness:     "الأمان",
  daily_living:            "الحياة اليومية",
  nutrition_guide:         "التغذية",
  family_support:          "دعم الأسرة",
  emotional_wellness:      "الصحة النفسية",
  allergy_precautions:     "الحساسية",
  pregnancy_specific:      "الحمل",
  child_specific:          "الأطفال",
};

export default function SessionPage() {
  const router = useRouter();
  const { id: sessionId } = useParams() as { id: string };
  const { isLoggedIn } = useAuthStore();

  const [session,   setSession]   = useState<SessionDetail | null>(null);
  const [loading,   setLoading]   = useState(true);
  const [err,       setErr]       = useState<string | null>(null);
  const [tab,       setTab]       = useState(0);
  const [exporting, setExporting] = useState<"pdf" | "docx" | null>(null);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    (async () => {
      try {
        const res = await sessionApi.getById(sessionId);
        if (res.success && res.data) setSession(res.data);
        else setErr(res.error ?? "تعذّر التحميل");
      } catch { setErr("خطأ في الاتصال"); }
      finally { setLoading(false); }
    })();
  }, [isLoggedIn, sessionId, router]);

  async function doExport(format: "pdf" | "docx") {
    setExporting(format);
    try {
      const token = typeof window !== "undefined" ? localStorage.getItem("muafa_token") : null;
      const url = sessionApi.getExportUrl(sessionId, format);
      const r = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
      if (!r.ok) throw new Error();
      const blob = await r.blob();
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = `muafaplus_${sessionId.slice(0, 8)}.${format}`;
      a.click();
    } catch { alert("فشل التحميل"); }
    finally { setExporting(null); }
  }

  if (!isLoggedIn) return null;

  return (
    <div className="min-h-screen flex flex-col bg-ink-50" dir="rtl">
      <NavBar />

      <main className="flex-1 max-w-5xl mx-auto px-6 py-8">

        {/* Breadcrumb */}
        <div className="flex items-center gap-3 mb-6">
          <Link
            href="/dashboard"
            className="text-ink-400 hover:text-ink-700 text-sm transition"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            ← لوحة التحكم
          </Link>
          <span className="text-ink-100">/</span>
          <span
            className="text-sm font-medium text-ink-700"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            عرض الجلسة
          </span>
        </div>

        {loading && (
          <div
            className="py-24 text-center text-ink-400 text-sm"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            جاري التحميل...
          </div>
        )}

        {err && !loading && (
          <div className="py-16 text-center">
            <p
              className="text-red-600 text-sm mb-3"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {err}
            </p>
            <Link
              href="/dashboard"
              className="text-navy-600 text-sm hover:underline"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              العودة
            </Link>
          </div>
        )}

        {session && !loading && (
          <>
            {/* Header card */}
            <div className="bg-white rounded-2xl border border-ink-100 p-6 mb-4">
              <div className="flex flex-wrap items-start justify-between gap-4 mb-4">
                <div>
                  <h1
                    className="text-lg font-semibold text-ink-900 mb-1"
                    style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                  >
                    نتائج التوليد
                  </h1>
                  <p className="text-xs text-ink-400 font-mono">{sessionId}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  {session.riskLevel && (
                    <span
                      className="px-3 py-1 rounded-full text-xs font-medium"
                      style={{
                        ...RISK_STYLE[session.riskLevel as RiskLevel],
                        fontFamily: "IBM Plex Sans Arabic, system-ui",
                      }}
                    >
                      {RISK_LABELS[session.riskLevel as RiskLevel]}
                    </span>
                  )}
                  <span
                    className="px-3 py-1 rounded-full text-xs"
                    style={{ background: '#F6F7FB', color: '#5A6478', border: '1px solid #EEF0F5', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                  >
                    {session.articles.length} مقالات
                  </span>
                  {session.totalCost && (
                    <span
                      className="px-3 py-1 rounded-full text-xs font-mono"
                      style={{ background: '#F6F7FB', color: '#5A6478', border: '1px solid #EEF0F5' }}
                    >
                      ${session.totalCost.toFixed(4)}
                    </span>
                  )}
                </div>
              </div>

              {session.status === "complete" && (
                <div className="flex gap-3 pt-4 border-t border-ink-100">
                  <button
                    onClick={() => doExport("pdf")}
                    disabled={!!exporting}
                    className="px-4 py-2 rounded-xl border border-ink-100 text-ink-700 text-sm hover:bg-ink-50 transition disabled:opacity-50 flex items-center gap-2"
                    style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                  >
                    {exporting === "pdf" ? <><Spin /> جاري...</> : "تحميل PDF"}
                  </button>
                  <button
                    onClick={() => doExport("docx")}
                    disabled={!!exporting}
                    className="px-4 py-2 rounded-xl border border-ink-100 text-ink-700 text-sm hover:bg-ink-50 transition disabled:opacity-50 flex items-center gap-2"
                    style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                  >
                    {exporting === "docx" ? <><Spin /> جاري...</> : "تحميل Word"}
                  </button>
                  <span
                    className="self-center text-xs text-ink-400"
                    style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                  >
                    RTL عربي · مُنسَّق
                  </span>
                </div>
              )}
            </div>

            {/* Articles tabs */}
            <div className="bg-white rounded-2xl border border-ink-100 overflow-hidden">
              <div className="flex overflow-x-auto border-b border-ink-100 px-2 pt-2 gap-1">
                {session.articles.map((a, idx) => (
                  <button
                    key={a.articleId}
                    onClick={() => setTab(idx)}
                    className="flex-shrink-0 px-4 py-2.5 rounded-t-xl text-xs font-medium whitespace-nowrap transition"
                    style={{
                      color:        tab === idx ? '#1E3A72' : '#8A93A6',
                      borderBottom: tab === idx ? '2px solid #1E3A72' : '2px solid transparent',
                      background:   tab === idx ? '#EEF1F7' : 'transparent',
                      fontFamily:   "IBM Plex Sans Arabic, system-ui",
                    }}
                  >
                    <span className="block max-w-[160px] truncate">
                      {a.articleType === "summary" ? "المقال التلخيصي" : covLabel(a.coverageCodes)}
                    </span>
                    <span
                      className="inline-block mt-0.5 px-1.5 py-0.5 rounded text-[10px]"
                      style={
                        a.articleType === "summary"
                          ? { background: '#EEF1F7', color: '#1E3A72' }
                          : { background: '#F6F7FB', color: '#8A93A6' }
                      }
                    >
                      {a.articleType === "summary" ? "ملخص" : "تفصيلي"}
                    </span>
                  </button>
                ))}
              </div>

              <div className="p-6">
                {session.articles[tab]
                  ? <ArtView art={session.articles[tab]} />
                  : (
                    <div
                      className="py-16 text-center text-ink-400 text-sm"
                      style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                    >
                      لا يوجد محتوى
                    </div>
                  )
                }
              </div>
            </div>

            {/* Cost row */}
            {session.totalCost && (
              <div className="mt-4 bg-white rounded-2xl border border-ink-100 px-6 py-4 flex justify-between text-sm">
                <span
                  className="text-ink-500"
                  style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                >
                  إجمالي التكلفة
                </span>
                <span className="font-mono font-semibold text-ink-900">
                  ${session.totalCost.toFixed(4)}
                </span>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

function ArtView({ art }: { art: ArticleRecord }) {
  const codes = art.coverageCodes.split(",").map(c => c.trim()).filter(Boolean);
  return (
    <div>
      {codes.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-5">
          {codes.map(c => (
            <span
              key={c}
              className="px-2.5 py-1 rounded-lg text-xs"
              style={{ background: '#EEF1F7', color: '#1E3A72', border: '1px solid #D4DCEB', fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {COV[c] ?? c}
            </span>
          ))}
          <span
            className="px-2.5 py-1 rounded-lg text-xs"
            style={{ background: '#F6F7FB', color: '#8A93A6', border: '1px solid #EEF0F5' }}
          >
            {art.wordCount.toLocaleString()} كلمة
          </span>
        </div>
      )}
      <div className="article-content max-w-3xl">
        <ReactMarkdown>{art.content}</ReactMarkdown>
      </div>
    </div>
  );
}

const Spin = () => (
  <span className="inline-block w-3.5 h-3.5 border-2 border-current/30 border-t-current rounded-full animate-spin" />
);
const covLabel = (codes: string) => {
  const c = codes.split(",")[0].trim();
  return COV[c] ?? c;
};
