"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/store";
import { tenantApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { TenantResponse } from "@/types";

export default function HospitalDashboard() {
  const router = useRouter();
  const { isLoggedIn, role, tenantId } = useAuthStore();
  const [tenant, setTenant] = useState<TenantResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    if (role !== "HospitalAdmin") { router.push("/dashboard"); return; }
  }, [isLoggedIn, role, router]);

  useEffect(() => {
    if (role !== "HospitalAdmin" || !tenantId) return;
    tenantApi.getTenant(tenantId)
      .then((res) => { if (res.success && res.data) setTenant(res.data); })
      .finally(() => setLoading(false));
  }, [role, tenantId]);

  if (!isLoggedIn || role !== "HospitalAdmin") return null;

  const sub = tenant?.activeSubscription;
  const usagePct = sub ? Math.min(100, Math.round((sub.casesUsed / sub.casesAllocated) * 100)) : 0;
  const usageColor =
    usagePct >= 90 ? "bg-red-500" :
    usagePct >= 70 ? "bg-amber-500" :
    "bg-brand-600";

  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />
      <main className="flex-1 max-w-4xl w-full mx-auto px-6 py-8">

        <div className="mb-8">
          {loading ? (
            <div className="h-7 w-48 bg-gray-100 rounded animate-pulse" />
          ) : (
            <>
              <h1 className="text-2xl font-bold text-gray-900">
                {tenant?.nameAr ?? tenant?.name ?? "لوحة تحكم المستشفى"}
              </h1>
              <p className="text-gray-500 text-sm mt-1">مدير المؤسسة</p>
            </>
          )}
        </div>

        {/* Subscription quota card */}
        <div className="bg-white rounded-2xl border border-gray-100 p-6 mb-6">
          <h2 className="font-semibold text-gray-800 mb-4">الاشتراك والحصة</h2>

          {loading ? (
            <div className="space-y-3">
              <div className="h-4 w-full bg-gray-100 rounded animate-pulse" />
              <div className="h-4 w-2/3 bg-gray-100 rounded animate-pulse" />
            </div>
          ) : sub ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between text-sm">
                <span className="text-gray-600">الخطة</span>
                <span className="font-medium text-gray-900">{sub.planType}</span>
              </div>

              <div>
                <div className="flex items-center justify-between text-sm mb-2">
                  <span className="text-gray-600">الحالات المستخدمة</span>
                  <span className="font-medium text-gray-900">
                    {sub.casesUsed} / {sub.casesAllocated}
                    <span className="text-gray-400 font-normal mr-1">({usagePct}%)</span>
                  </span>
                </div>
                <div className="w-full bg-gray-100 rounded-full h-3">
                  <div
                    className={`h-3 rounded-full transition-all ${usageColor}`}
                    style={{ width: `${usagePct}%` }}
                  />
                </div>
                {usagePct >= 90 && (
                  <p className="text-red-600 text-xs mt-1">
                    تنبيه: اقتربت من الحد الأقصى للحالات.
                  </p>
                )}
              </div>

              <div className="flex items-center justify-between text-sm pt-2 border-t border-gray-50">
                <span className="text-gray-600">تاريخ انتهاء الدورة</span>
                <span className="font-medium text-gray-900">
                  {new Date(sub.billingCycleEnd).toLocaleDateString("ar-YE")}
                </span>
              </div>
            </div>
          ) : (
            <p className="text-gray-400 text-sm">لا يوجد اشتراك نشط.</p>
          )}
        </div>

        {/* Quick links */}
        <div className="grid grid-cols-2 gap-4">
          <QuickLink href="/referrals" label="عرض الإحالات" desc="تتبّع حالات المرضى" />
          <QuickLink href="/hospital/subscription" label="إدارة الاشتراك" desc="تفاصيل الخطة والحصص" />
        </div>
      </main>
    </div>
  );
}

function QuickLink({ href, label, desc }: { href: string; label: string; desc: string }) {
  return (
    <a
      href={href}
      className="bg-white rounded-2xl border border-gray-100 px-6 py-5 hover:border-brand-200 hover:shadow-sm transition block"
    >
      <p className="font-semibold text-gray-800 text-sm">{label}</p>
      <p className="text-gray-400 text-xs mt-1">{desc}</p>
    </a>
  );
}
