"use client";
import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import ReactMarkdown from "react-markdown";
import { useAuthStore } from "@/lib/store";
import { sessionApi } from "@/services/api";
import type { SessionDetail, ArticleRecord, RiskLevel } from "@/types";

const RISK_LABELS: Record<RiskLevel,string> = { LOW:"منخفض", MODERATE:"متوسط", HIGH:"مرتفع", CRITICAL:"حرج" };
const RISK_CLASS:  Record<RiskLevel,string> = { LOW:"risk-low", MODERATE:"risk-moderate", HIGH:"risk-high", CRITICAL:"risk-critical" };
const COV: Record<string,string> = {
  understanding_condition:"فهم الحالة", medication_management:"إدارة الأدوية",
  safety_preparedness:"الأمان", daily_living:"الحياة اليومية", nutrition_guide:"التغذية",
  family_support:"دعم الأسرة", emotional_wellness:"الصحة النفسية",
  allergy_precautions:"الحساسية", pregnancy_specific:"الحمل", child_specific:"الأطفال",
};

export default function SessionPage() {
  const router = useRouter();
  const { id: sessionId } = useParams() as { id: string };
  const { isLoggedIn } = useAuthStore();

  const [session, setSession] = useState<SessionDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string|null>(null);
  const [tab, setTab] = useState(0);
  const [exporting, setExporting] = useState<"pdf"|"docx"|null>(null);

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

  async function doExport(format: "pdf"|"docx") {
    setExporting(format);
    try {
      const token = typeof window !== "undefined" ? localStorage.getItem("muafa_token") : null;
      const url = sessionApi.getExportUrl(sessionId, format);
      const r = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
      if (!r.ok) throw new Error();
      const blob = await r.blob();
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = `muafaplus_${sessionId.slice(0,8)}.${format}`;
      a.click();
    } catch { alert("فشل التحميل"); }
    finally { setExporting(null); }
  }

  if (!isLoggedIn) return null;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link href="/dashboard" className="text-gray-400 hover:text-gray-700 text-sm">← لوحة التحكم</Link>
          <span className="text-gray-200">/</span>
          <span className="text-sm font-medium text-gray-700">عرض الجلسة</span>
        </div>
        <div className="w-8 h-8 rounded-xl bg-brand-600 flex items-center justify-center text-white text-xs font-bold">م+</div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-8">
        {loading && <div className="py-24 text-center text-gray-400 text-sm">جاري التحميل...</div>}
        {err && !loading && (
          <div className="py-16 text-center">
            <p className="text-red-600 text-sm mb-3">{err}</p>
            <Link href="/dashboard" className="text-brand-600 text-sm hover:underline">العودة</Link>
          </div>
        )}

        {session && !loading && (
          <>
            {/* Header card */}
            <div className="bg-white rounded-2xl border border-gray-100 p-6 mb-6">
              <div className="flex flex-wrap items-start justify-between gap-4 mb-4">
                <div>
                  <h1 className="text-lg font-semibold text-gray-900 mb-1">نتائج التوليد</h1>
                  <p className="text-xs text-gray-400 font-mono">{sessionId}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  {session.riskLevel && (
                    <span className={`px-3 py-1 rounded-full border text-xs font-medium ${RISK_CLASS[session.riskLevel as RiskLevel]}`}>
                      {RISK_LABELS[session.riskLevel as RiskLevel]}
                    </span>
                  )}
                  <span className="px-3 py-1 rounded-full bg-gray-50 border border-gray-200 text-gray-600 text-xs">
                    {session.articles.length} مقالات
                  </span>
                  {session.totalCost && (
                    <span className="px-3 py-1 rounded-full bg-gray-50 border border-gray-200 text-gray-600 text-xs font-mono">
                      ${session.totalCost.toFixed(4)}
                    </span>
                  )}
                </div>
              </div>

              {session.status === "complete" && (
                <div className="flex gap-3 pt-4 border-t border-gray-50">
                  <button onClick={() => doExport("pdf")} disabled={!!exporting}
                    className="px-4 py-2 rounded-xl border border-gray-200 text-gray-700 text-sm hover:bg-gray-50 transition disabled:opacity-50 flex items-center gap-2">
                    {exporting==="pdf" ? <><Spin/> جاري...</> : "تحميل PDF"}
                  </button>
                  <button onClick={() => doExport("docx")} disabled={!!exporting}
                    className="px-4 py-2 rounded-xl border border-gray-200 text-gray-700 text-sm hover:bg-gray-50 transition disabled:opacity-50 flex items-center gap-2">
                    {exporting==="docx" ? <><Spin/> جاري...</> : "تحميل Word"}
                  </button>
                  <span className="self-center text-xs text-gray-400">RTL عربي · مُنسَّق</span>
                </div>
              )}
            </div>

            {/* Articles */}
            <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
              <div className="flex overflow-x-auto border-b border-gray-100 px-2 pt-2 gap-1">
                {session.articles.map((a, idx) => (
                  <button key={a.articleId} onClick={() => setTab(idx)}
                    className={`flex-shrink-0 px-4 py-2.5 rounded-t-xl text-xs font-medium whitespace-nowrap transition
                      ${tab===idx ? "bg-brand-50 text-brand-800 border-b-2 border-brand-600" : "text-gray-500 hover:text-gray-800 hover:bg-gray-50"}`}>
                    <span className="block max-w-[160px] truncate">
                      {a.articleType==="summary" ? "المقال التلخيصي" : covLabel(a.coverageCodes)}
                    </span>
                    <span className={`inline-block mt-0.5 px-1.5 py-0.5 rounded text-[10px]
                      ${a.articleType==="summary" ? "bg-brand-100 text-brand-700" : "bg-gray-100 text-gray-400"}`}>
                      {a.articleType==="summary" ? "ملخص" : "تفصيلي"}
                    </span>
                  </button>
                ))}
              </div>

              <div className="p-6">
                {session.articles[tab]
                  ? <ArtView art={session.articles[tab]} />
                  : <div className="py-16 text-center text-gray-400 text-sm">لا يوجد محتوى</div>}
              </div>
            </div>

            {/* Cost row */}
            {session.totalCost && (
              <div className="mt-5 bg-white rounded-2xl border border-gray-100 px-6 py-4 flex justify-between text-sm">
                <span className="text-gray-500">إجمالي التكلفة</span>
                <span className="font-mono font-semibold text-gray-900">${session.totalCost.toFixed(4)}</span>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

function ArtView({ art }: { art: ArticleRecord }) {
  const codes = art.coverageCodes.split(",").map(c=>c.trim()).filter(Boolean);
  return (
    <div>
      {codes.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-5">
          {codes.map(c => (
            <span key={c} className="px-2.5 py-1 rounded-lg bg-brand-50 text-brand-700 border border-brand-100 text-xs">
              {COV[c]??c}
            </span>
          ))}
          <span className="px-2.5 py-1 rounded-lg bg-gray-50 text-gray-400 border border-gray-200 text-xs">
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

const Spin = () => <span className="inline-block w-3.5 h-3.5 border-2 border-current/30 border-t-current rounded-full animate-spin"/>;
const covLabel = (codes: string) => { const c = codes.split(",")[0].trim(); return COV[c]??c; };
