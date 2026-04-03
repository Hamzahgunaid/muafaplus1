"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { authApi } from "@/services/api";
import type { ChangePasswordRequest } from "@/types";

export default function ChangePasswordPage() {
  const router = useRouter();
  const [error, setError]     = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<ChangePasswordRequest>();

  const onSubmit = async (data: ChangePasswordRequest) => {
    setError(null);
    setLoading(true);
    try {
      const res = await authApi.changePassword(data);
      if (res.success) {
        router.push("/dashboard");
      } else {
        setError(res.error ?? "فشل تغيير كلمة المرور");
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
          <p className="text-gray-500 text-sm mt-1">تغيير كلمة المرور</p>
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">
          <h2 className="text-lg font-semibold text-gray-800 mb-2">تغيير كلمة المرور</h2>
          <p className="text-sm text-gray-500 mb-6">
            يجب عليك تغيير كلمة المرور قبل المتابعة.
          </p>

          {error && (
            <div className="mb-4 px-4 py-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                كلمة المرور الحالية
              </label>
              <input
                type="password"
                autoComplete="current-password"
                className={`w-full px-4 py-3 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 transition
                  ${errors.currentPassword ? "border-red-400 bg-red-50" : "border-gray-200 bg-gray-50"}`}
                placeholder="••••••••"
                {...register("currentPassword", {
                  required:  "كلمة المرور الحالية مطلوبة",
                  minLength: { value: 8, message: "8 أحرف على الأقل" },
                })}
              />
              {errors.currentPassword && (
                <p className="text-red-600 text-xs mt-1">{errors.currentPassword.message}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                كلمة المرور الجديدة
              </label>
              <input
                type="password"
                autoComplete="new-password"
                className={`w-full px-4 py-3 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 transition
                  ${errors.newPassword ? "border-red-400 bg-red-50" : "border-gray-200 bg-gray-50"}`}
                placeholder="••••••••"
                {...register("newPassword", {
                  required:  "كلمة المرور الجديدة مطلوبة",
                  minLength: { value: 8, message: "8 أحرف على الأقل" },
                })}
              />
              {errors.newPassword && (
                <p className="text-red-600 text-xs mt-1">{errors.newPassword.message}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 rounded-xl bg-brand-600 text-white font-semibold text-sm
                         hover:bg-brand-800 active:scale-[0.99] transition disabled:opacity-60"
            >
              {loading ? "جاري الحفظ..." : "حفظ كلمة المرور"}
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
