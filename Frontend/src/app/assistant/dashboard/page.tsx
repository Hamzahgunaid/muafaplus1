"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { referralApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { ReferralResponse } from "@/types";

export default function AssistantDashboard() {
  const router = useRouter();
  const { isLoggedIn, role, fullName } = useAuthStore();
  const [referrals, setReferrals] = useState<ReferralResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    if (role !== "Assistant") { router.push("/dashboard"); return; }
  }, [isLoggedIn, role, router]);

  useEffect(() => {
    if (role !== "Assistant") return;
    referralApi.getReferrals()
      .then((res) => { if (res.success && res.data) setReferrals(res.data); })
      .finally(() => setLoading(false));
  }, [role]);

  if (!isLoggedIn || role !== "Assistant") return null;

  const total     = referrals.length;
  const pending   = referrals.filter(r => r.status === "Created" || r.status === "Stage1Complete").length;
  const delivered = referrals.filter(r => r.deliveredAt).length;

  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />
      <main className="flex-1 max-w-3xl w-full mx-auto px-6 py-8">

        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">
            أهلاً{fullName ? `، ${fullName}` : ""}
          </h1>
          <p className="text-gray-500 text-sm mt-1">لوحة تحكم المساعد</p>
        </div>

        {/* Primary CTA */}
        <Link
          href="/referrals/new"
          className="flex items-center justify-center gap-3 w-full py-5 rounded-2xl bg-brand-600 text-white font-bold text-lg hover:bg-brand-800 active:scale-[0.99] transition mb-8 shadow-sm"
        >
          <span className="text-2xl">+</span>
          إنشاء إحالة جديدة
        </Link>

        {/* Stats */}
        <div className="grid grid-cols-3 gap-4 mb-8">
          <StatCard label="إجمالي الإحالات" value={loading ? "…" : total.toString()} />
          <StatCard label="قيد الانتظار"    value={loading ? "…" : pending.toString()} />
          <StatCard label="تم التسليم"       value={loading ? "…" : delivered.toString()} />
        </div>

        {/* Recent referrals */}
        {!loading && referrals.length > 0 && (
          <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-50 flex items-center justify-between">
              <h2 className="font-semibold text-gray-800 text-sm">آخر الإحالات</h2>
              <Link href="/referrals" className="text-brand-600 text-xs hover:underline">
                عرض الكل
              </Link>
            </div>
            <ul className="divide-y divide-gray-50">
              {referrals.slice(0, 5).map((r) => (
                <li key={r.referralId} className="px-6 py-4 flex items-center justify-between">
                  <div>
                    <span className="text-sm font-medium text-gray-800">{r.referralCode}</span>
                    <span className="block text-xs text-gray-400 mt-0.5">
                      {new Date(r.createdAt).toLocaleDateString("ar-YE")}
                    </span>
                  </div>
                  <Link
                    href={`/referrals/${r.referralId}`}
                    className="text-brand-600 text-xs hover:underline"
                  >
                    عرض
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        )}
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
