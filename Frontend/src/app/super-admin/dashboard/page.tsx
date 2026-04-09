"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/store";
import { tenantApi } from "@/services/api";
import NavBar from "@/components/NavBar";
import type { TenantResponse } from "@/types";

export default function SuperAdminDashboard() {
  const router = useRouter();
  const { isLoggedIn, role } = useAuthStore();
  const [tenants, setTenants] = useState<TenantResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    if (role !== "SuperAdmin") { router.push("/dashboard"); return; }
  }, [isLoggedIn, role, router]);

  useEffect(() => {
    if (role !== "SuperAdmin") return;
    tenantApi.getTenants()
      .then((res) => { if (res.success && res.data) setTenants(res.data); })
      .finally(() => setLoading(false));
  }, [role]);

  if (!isLoggedIn || role !== "SuperAdmin") return null;

  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />
      <main className="flex-1 max-w-6xl w-full mx-auto px-6 py-8">

        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">لوحة تحكم المشرف العام</h1>
          <p className="text-gray-500 text-sm mt-1">منصة معافى+ · إدارة المستأجرين</p>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-3 gap-4 mb-8">
          <StatCard label="إجمالي المؤسسات" value={tenants.length.toString()} />
          <StatCard label="المؤسسات النشطة"  value={tenants.filter(t => t.isActive).length.toString()} />
          <StatCard label="غير النشطة"        value={tenants.filter(t => !t.isActive).length.toString()} />
        </div>

        {/* Tenants table */}
        <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-50">
            <h2 className="font-semibold text-gray-800">قائمة المستأجرين</h2>
          </div>

          {loading ? (
            <div className="py-16 text-center text-gray-400 text-sm">جاري التحميل...</div>
          ) : tenants.length === 0 ? (
            <div className="py-16 text-center text-gray-400 text-sm">لا توجد مؤسسات مسجّلة</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 text-gray-500 text-xs">
                    <th className="px-6 py-3 text-right font-medium">المؤسسة</th>
                    <th className="px-6 py-3 text-right font-medium">المعرّف</th>
                    <th className="px-6 py-3 text-right font-medium">الدولة</th>
                    <th className="px-6 py-3 text-right font-medium">الخطة</th>
                    <th className="px-6 py-3 text-right font-medium">الحالة</th>
                    <th className="px-6 py-3 text-right font-medium">تاريخ الإنشاء</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {tenants.map((t) => (
                    <tr key={t.tenantId} className="hover:bg-gray-50 transition">
                      <td className="px-6 py-4">
                        <span className="font-medium text-gray-800">{t.nameAr ?? t.name}</span>
                        {t.nameAr && <span className="block text-xs text-gray-400">{t.name}</span>}
                      </td>
                      <td className="px-6 py-4 text-gray-500 font-mono text-xs">{t.slug}</td>
                      <td className="px-6 py-4 text-gray-500">{t.country ?? "—"}</td>
                      <td className="px-6 py-4 text-gray-500">
                        {t.activeSubscription?.planType ?? "—"}
                      </td>
                      <td className="px-6 py-4">
                        <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
                          t.isActive
                            ? "bg-green-50 text-green-700 border border-green-200"
                            : "bg-gray-100 text-gray-500 border border-gray-200"
                        }`}>
                          {t.isActive ? "نشط" : "معطّل"}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-gray-400 text-xs">
                        {new Date(t.createdAt).toLocaleDateString("ar-YE")}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
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
