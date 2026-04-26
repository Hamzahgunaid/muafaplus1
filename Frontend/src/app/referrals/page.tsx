"use client";
import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { referralApi, maskPhone, formatRelativeTime } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { ReferralResponse } from "@/types";

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

export default function ReferralsPage() {
  const router  = useRouter();
  const { isLoggedIn } = useAuthStore();
  const [referrals, setReferrals] = useState<ReferralResponse[]>([]);
  const [loading, setLoading]     = useState(true);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
  }, [isLoggedIn, router]);

  const fetchReferrals = useCallback(async () => {
    setLoading(true);
    try {
      const res = await referralApi.getReferrals();
      if (res.success && res.data) setReferrals(res.data);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { if (isLoggedIn) fetchReferrals(); }, [isLoggedIn, fetchReferrals]);

  if (!isLoggedIn) return null;

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
            الإحالات
          </h1>
          <Link
            href="/referrals/new"
            className="px-4 py-2 rounded-xl bg-navy-600 text-white text-sm font-semibold hover:bg-navy-700 transition"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            + إحالة جديدة
          </Link>
        </div>

        {/* Card list */}
        <div className="bg-white rounded-2xl border border-ink-100 overflow-hidden">
          <div className="px-6 py-4 border-b border-ink-100 flex items-center justify-between">
            <h2
              className="font-semibold text-ink-900 text-sm"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              قائمة الإحالات
            </h2>
            <button
              onClick={fetchReferrals}
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
          ) : referrals.length === 0 ? (
            <div className="py-16 text-center">
              <p
                className="text-ink-400 text-sm mb-4"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                لا توجد إحالات بعد
              </p>
              <Link
                href="/referrals/new"
                className="text-navy-600 text-sm font-medium hover:underline"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                إنشاء أول إحالة
              </Link>
            </div>
          ) : (
            <div className="divide-y divide-ink-100">
              {referrals.map((r) => (
                <ReferralCard key={r.referralId} referral={r} />
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}

function ReferralCard({ referral: r }: { referral: ReferralResponse }) {
  const sentDot   = r.status !== "Created";
  const openedDot = r.status === "Stage2Complete" || r.status === "Stage1Complete";
  const viewedDot = r.status === "Stage2Complete";

  return (
    <div className="px-6 py-4 hover:bg-ink-50 transition">
      <div className="flex items-center justify-between gap-4">

        {/* Phone + meta */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3 flex-wrap">
            <span
              className="font-medium text-ink-900 text-sm font-mono"
            >
              {maskPhone(r.patientPhone)}
            </span>

            {r.riskLevel && RISK_CLASS[r.riskLevel] && (
              <span
                className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${RISK_CLASS[r.riskLevel]}`}
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {RISK_LABEL[r.riskLevel] ?? r.riskLevel}
              </span>
            )}

            <span
              className="inline-block px-2 py-0.5 rounded-full text-xs font-medium bg-ink-100 text-ink-500"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {r.status}
            </span>
          </div>

          <div className="flex items-center gap-3 mt-1.5">
            <EngagementDots sent={sentDot} opened={openedDot} viewed={viewedDot} />
            <span
              className="text-xs text-ink-400"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {formatRelativeTime(r.createdAt)}
            </span>
          </div>
        </div>

        {/* Action */}
        <Link
          href={`/referrals/${r.referralId}`}
          className="shrink-0 text-navy-600 text-xs font-semibold hover:underline"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          عرض التفاصيل
        </Link>
      </div>
    </div>
  );
}

function EngagementDots({
  sent,
  opened,
  viewed,
}: {
  sent:   boolean;
  opened: boolean;
  viewed: boolean;
}) {
  return (
    <div className="flex items-center gap-1.5">
      <Dot active={sent}   title="أُرسلت الرسالة" />
      <Dot active={opened} title="فتح التطبيق" />
      <Dot active={viewed} title="قرأ الملخص" />
    </div>
  );
}

function Dot({ active, title }: { active: boolean; title: string }) {
  return (
    <span
      title={title}
      className={`w-2 h-2 rounded-full ${active ? "bg-navy-600" : "bg-ink-100"}`}
    />
  );
}
