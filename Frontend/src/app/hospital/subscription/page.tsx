"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/store";
import NavBar from "@/components/NavBar";

export default function HospitalSubscriptionPage() {
  const router = useRouter();
  const { isLoggedIn, role } = useAuthStore();

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); return; }
    if (role !== "HospitalAdmin") { router.push("/dashboard"); return; }
  }, [isLoggedIn, role, router]);

  if (!isLoggedIn || role !== "HospitalAdmin") return null;

  return (
    <div className="min-h-screen flex flex-col">
      <NavBar />
      <main className="flex-1 max-w-3xl w-full mx-auto px-6 py-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">إدارة الاشتراك</h1>
        <p className="text-gray-500 text-sm">تفاصيل الخطة والحصص — قريباً</p>
      </main>
    </div>
  );
}
