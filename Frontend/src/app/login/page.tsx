"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { useAuthStore } from "@/lib/store";
import { authApi } from "@/services/api";
import type { LoginRequest } from "@/types";

const DEMO_ACCOUNTS = [
  { label: "طبيب", email: "ahmed.sana@hospital.ye",  pass: "MuafaPlus2025!" },
  { label: "مساعد", email: "fatima.hakim@clinic.ye", pass: "MuafaPlus2025!" },
];

export default function LoginPage() {
  const router  = useRouter();
  const login   = useAuthStore((s) => s.login);
  const [error, setError]     = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, setValue, formState: { errors } } = useForm<LoginRequest>();

  const onSubmit = async (data: LoginRequest) => {
    setError(null);
    setLoading(true);
    try {
      const res = await authApi.login(data);
      if (res.success && res.data) {
        login(res.data);
        router.push(res.data.mustResetOnNextLogin ? "/change-password" : "/dashboard");
      } else {
        setError(res.error ?? "بيانات الدخول غير صحيحة");
      }
    } catch {
      setError("تعذّر الاتصال بالخادم");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex" dir="rtl">

      {/* ── Left Hero Column ── */}
      <div
        className="hidden lg:flex lg:w-1/2 flex-col justify-between p-12 relative overflow-hidden"
        style={{ background: "linear-gradient(135deg, #11254A 0%, #1E3A72 60%, #17305F 100%)" }}
      >
        {/* Orange burst decoration */}
        <div className="absolute top-8 left-8 opacity-20">
          <svg width="120" height="120" viewBox="0 0 120 120">
            {[0,30,60,90,120,150,180,210,240,270,300,330].map((angle, i) => (
              <line key={i}
                x1="60" y1="60"
                x2={60 + 50 * Math.cos(angle * Math.PI / 180)}
                y2={60 + 50 * Math.sin(angle * Math.PI / 180)}
                stroke="#E87A2F" strokeWidth="2.5" strokeLinecap="round" />
            ))}
          </svg>
        </div>

        {/* Logo */}
        <div className="flex items-center gap-3">
          <div className="bg-white rounded-2xl p-3 shadow-lg">
            <span className="font-bold text-xl" style={{ color: "#1E3A72", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
              معافى+
            </span>
          </div>
        </div>

        {/* Hero content */}
        <div className="flex-1 flex flex-col justify-center">
          {/* AI pill */}
          <div
            className="inline-flex items-center gap-2 px-4 py-2 rounded-full mb-8 w-fit"
            style={{ background: "rgba(255,255,255,0.1)", border: "1px solid rgba(255,255,255,0.2)" }}
          >
            <div className="w-2 h-2 rounded-full bg-green-400 animate-pulse" />
            <span className="text-white text-sm font-medium" style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
              مدعوم بالذكاء الاصطناعي
            </span>
          </div>

          <h1 className="text-4xl font-bold text-white leading-tight mb-4"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
            تعليم طبي مخصص
            <br />
            <span style={{ color: "#9ECDF5" }}>لكل مريض بعد التشخيص</span>
          </h1>

          <p className="text-lg mb-10"
            style={{ color: "rgba(255,255,255,0.65)", fontFamily: "IBM Plex Sans Arabic, system-ui", lineHeight: "1.8" }}>
            منصة معافى+ تمكّن الأطباء من تقديم محتوى صحي مخصص لمرضاهم فور التشخيص عبر واتساب وتطبيق الجوال.
          </p>

          {/* Stats card */}
          <div className="rounded-2xl p-6"
            style={{ background: "rgba(255,255,255,0.08)", border: "1px solid rgba(255,255,255,0.12)" }}>
            {/* ECG line */}
            <div className="mb-4 opacity-60">
              <svg viewBox="0 0 300 40" className="w-full h-8">
                <polyline
                  points="0,20 40,20 55,5 65,35 75,10 85,20 130,20 145,5 155,35 165,10 175,20 220,20 235,5 245,35 255,10 265,20 300,20"
                  fill="none" stroke="#50B2E6" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </div>
            <div className="grid grid-cols-3 gap-4">
              {[
                { value: "٢٤٠+", label: "مقال طبي" },
                { value: "٩٨٪",  label: "رضا المرضى" },
                { value: "٤٨ث",  label: "وقت التوليد" },
              ].map((stat, i) => (
                <div key={i} className="text-center">
                  <div className="text-2xl font-bold text-white mb-1"
                    style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
                    {stat.value}
                  </div>
                  <div className="text-xs" style={{ color: "rgba(255,255,255,0.5)", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
                    {stat.label}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="text-sm" style={{ color: "rgba(255,255,255,0.35)", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
          معافى+ · أفية وايز للحلول الرقمية · اليمن
        </div>
      </div>

      {/* ── Right Form Column ── */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-8" style={{ backgroundColor: "#F6F7FB" }}>
        <div className="w-full max-w-md">

          {/* Mobile logo */}
          <div className="flex justify-center mb-8 lg:hidden">
            <div className="rounded-2xl p-4" style={{ backgroundColor: "#1E3A72" }}>
              <span className="text-white font-bold text-2xl" style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
                معافى+
              </span>
            </div>
          </div>

          <h2 className="text-2xl font-bold mb-2" style={{ color: "#0E1726", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
            تسجيل الدخول
          </h2>
          <p className="text-sm mb-8" style={{ color: "#5A6478", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
            أدخل بياناتك للوصول إلى لوحة التحكم
          </p>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>

            {/* Email */}
            <div>
              <label className="block text-sm font-semibold mb-2"
                style={{ color: "#2D3748", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
                البريد الإلكتروني
              </label>
              <input
                type="email"
                autoComplete="email"
                placeholder="doctor@hospital.ye"
                className="w-full px-4 py-3 rounded-xl text-sm outline-none transition-all"
                style={{
                  background: "white",
                  border: "1.5px solid #EEF0F5",
                  color: "#0E1726",
                  fontFamily: "IBM Plex Sans Arabic, system-ui",
                  direction: "ltr",
                  textAlign: "right",
                }}
                onFocus={e  => (e.target.style.borderColor = "#1E3A72")}
                onBlur={e   => (e.target.style.borderColor = "#EEF0F5")}
                {...register("email", {
                  required: "البريد الإلكتروني مطلوب",
                  pattern:  { value: /\S+@\S+\.\S+/, message: "صيغة البريد غير صحيحة" },
                })}
              />
              {errors.email && (
                <p className="text-red-600 text-xs mt-1">{errors.email.message}</p>
              )}
            </div>

            {/* Password */}
            <div>
              <div className="flex justify-between items-center mb-2">
                <label className="text-sm font-semibold" style={{ color: "#2D3748", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
                  كلمة المرور
                </label>
              </div>
              <input
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                className="w-full px-4 py-3 rounded-xl text-sm outline-none transition-all"
                style={{
                  background: "white",
                  border: "1.5px solid #EEF0F5",
                  color: "#0E1726",
                  direction: "ltr",
                }}
                onFocus={e  => (e.target.style.borderColor = "#1E3A72")}
                onBlur={e   => (e.target.style.borderColor = "#EEF0F5")}
                {...register("password", {
                  required:  "كلمة المرور مطلوبة",
                  minLength: { value: 8, message: "8 أحرف على الأقل" },
                })}
              />
              {errors.password && (
                <p className="text-red-600 text-xs mt-1">{errors.password.message}</p>
              )}
            </div>

            {/* Error */}
            {error && (
              <div className="px-4 py-3 rounded-xl text-sm text-center"
                style={{ background: "#FBE5E5", color: "#D64545", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
                {error}
              </div>
            )}

            {/* Submit */}
            <button
              type="submit"
              disabled={loading}
              className="w-full py-3.5 rounded-xl font-bold text-white text-sm transition-all"
              style={{
                background: loading ? "#4E6AA3" : "#1E3A72",
                fontFamily: "IBM Plex Sans Arabic, system-ui",
                boxShadow: "0 4px 16px rgba(30,58,114,0.25)",
              }}
            >
              {loading ? "جارٍ تسجيل الدخول..." : "دخول"}
            </button>

          </form>

          {/* Demo accounts */}
          <div className="mt-6 p-4 rounded-xl" style={{ background: "#EEF1F7", border: "1px solid #D4DCEB" }}>
            <p className="text-xs font-semibold mb-3 text-center"
              style={{ color: "#2D3748", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
              حسابات تجريبية
            </p>
            <div className="space-y-2">
              {DEMO_ACCOUNTS.map((acc, i) => (
                <button key={i}
                  type="button"
                  onClick={() => { setValue("email", acc.email); setValue("password", acc.pass); }}
                  className="w-full text-right px-3 py-2 rounded-lg text-xs transition-all hover:bg-white"
                  style={{ color: "#1E3A72", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
                  <span className="font-semibold">{acc.label}:</span> {acc.email}
                </button>
              ))}
            </div>
          </div>

          <p className="text-center text-xs mt-6" style={{ color: "#8A93A6", fontFamily: "IBM Plex Sans Arabic, system-ui" }}>
            معافى+ · أفية وايز للحلول الرقمية
          </p>

        </div>
      </div>
    </div>
  );
}
