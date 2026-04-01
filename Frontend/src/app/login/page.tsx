"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { useAuthStore } from "@/lib/store";
import { authApi } from "@/services/api";
import type { LoginRequest } from "@/types";

export default function LoginPage() {
  const router  = useRouter();
  const login   = useAuthStore((s) => s.login);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginRequest>();

  const onSubmit = async (data: LoginRequest) => {
    setError(null);
    setLoading(true);
    try {
      const res = await authApi.login(data);
      if (res.success && res.data) {
        login(res.data);
        router.push("/dashboard");
      } else {
        setError(res.error ?? "فشل تسجيل الدخول");
      }
    } catch {
      setError("خطأ في الاتصال بالخادم. يرجى المحاولة مرة أخرى.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-brand-50 to-white px-4">
      <div className="w-full max-w-md">

        {/* Logo / Brand */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-brand-600 mb-4">
            <span className="text-white text-2xl font-bold">م+</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">معافى+</h1>
          <p className="text-gray-500 text-sm mt-1">لوحة تحكم الطبيب</p>
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">
          <h2 className="text-lg font-semibold text-gray-800 mb-6">تسجيل الدخول</h2>

          {error && (
            <div className="mb-4 px-4 py-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                البريد الإلكتروني
              </label>
              <input
                type="email"
                autoComplete="email"
                className={`w-full px-4 py-3 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 transition
                  ${errors.email ? "border-red-400 bg-red-50" : "border-gray-200 bg-gray-50"}`}
                placeholder="doctor@hospital.ye"
                {...register("email", {
                  required: "البريد الإلكتروني مطلوب",
                  pattern:  { value: /\S+@\S+\.\S+/, message: "صيغة البريد غير صحيحة" },
                })}
              />
              {errors.email && (
                <p className="text-red-600 text-xs mt-1">{errors.email.message}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                كلمة المرور
              </label>
              <input
                type="password"
                autoComplete="current-password"
                className={`w-full px-4 py-3 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 transition
                  ${errors.password ? "border-red-400 bg-red-50" : "border-gray-200 bg-gray-50"}`}
                placeholder="••••••••"
                {...register("password", {
                  required: "كلمة المرور مطلوبة",
                  minLength: { value: 8, message: "8 أحرف على الأقل" },
                })}
              />
              {errors.password && (
                <p className="text-red-600 text-xs mt-1">{errors.password.message}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 rounded-xl bg-brand-600 text-white font-semibold text-sm
                         hover:bg-brand-800 active:scale-[0.99] transition disabled:opacity-60"
            >
              {loading ? "جاري تسجيل الدخول..." : "دخول"}
            </button>
          </form>
        </div>

        <p className="text-center text-xs text-gray-400 mt-6">
          منصة معافى+ · نظام توليد المحتوى التعليمي الطبي
        </p>
      </div>
    </div>
  );
}
