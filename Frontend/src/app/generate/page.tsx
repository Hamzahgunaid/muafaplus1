"use client";
import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { contentApi } from "@/services/api";
import { useSessionPolling } from "@/hooks/useSessionPolling";
import type { PatientData, AgeGroup } from "@/types";
import RiskBadge from "@/components/RiskBadge";

const AGE_GROUPS: { value: AgeGroup; label: string }[] = [
  { value: "Infant",     label: "رضيع (0-1 سنة)"      },
  { value: "Toddler",    label: "طفل صغير (1-3 سنوات)" },
  { value: "Child",      label: "طفل (3-12 سنة)"       },
  { value: "Adolescent", label: "مراهق (12-18 سنة)"    },
  { value: "Adult",      label: "بالغ (18-65 سنة)"     },
  { value: "Elderly",    label: "مسن (65+ سنة)"        },
];

type AppStage =
  | { type: "idle" }
  | { type: "stage1" }
  | { type: "polling"; sessionId: string; riskLevel: string; articleCount: number }
  | { type: "complete"; sessionId: string; riskLevel: string; totalCost: number; articles: number }
  | { type: "error"; message: string };

export default function GeneratePage() {
  const router = useRouter();
  const { isLoggedIn, physician } = useAuthStore();
  const [appStage, setAppStage] = useState<AppStage>({ type: "idle" });

  const pollingId = appStage.type === "polling" ? appStage.sessionId : null;
  const poll = useSessionPolling(pollingId, true);

  useEffect(() => {
    if (!isLoggedIn) router.push("/login");
  }, [isLoggedIn, router]);

  useEffect(() => {
    if (appStage.type !== "polling") return;
    if (poll.status === "complete" && poll.data) {
      setAppStage({
        type: "complete",
        sessionId: appStage.sessionId,
        riskLevel: poll.data.riskLevel ?? "MODERATE",
        totalCost: poll.data.totalCost ?? 0,
        articles: poll.data.totalArticles ?? 0,
      });
    }
    if (poll.status === "failed")  setAppStage({ type: "error", message: poll.error ?? "فشل التوليد" });
    if (poll.status === "timeout") setAppStage({ type: "error", message: "انتهت مهلة الانتظار. راجع لوحة التحكم لاحقاً." });
  }, [poll.status, poll.data, poll.error, appStage]);

  const { register, handleSubmit, formState: { errors }, reset } = useForm<PatientData>({
    defaultValues: {
      ageGroup: "Adult", comorbidities: "لا يوجد",
      currentMedications: "لا يوجد", allergies: "لا حساسية معروفة", medicalRestrictions: "لا يوجد",
    },
  });

  const onSubmit = async (data: PatientData) => {
    if (!physician) return;
    setAppStage({ type: "stage1" });
    try {
      const res = await contentApi.generateComplete(physician.physicianId, data);
      if (res.success && res.data) {
        const r = res.data as any;
        setAppStage({ type: "polling", sessionId: r.sessionId, riskLevel: r.riskScore?.riskLevel ?? "MODERATE", articleCount: r.articleCount ?? 0 });
      } else {
        setAppStage({ type: "error", message: res.error ?? "فشل إنشاء الجلسة" });
      }
    } catch (e: any) {
      setAppStage({ type: "error", message: e?.message ?? "خطأ في الاتصال" });
    }
  };

  if (!isLoggedIn) return null;
  const busy = appStage.type === "stage1" || appStage.type === "polling";

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link href="/dashboard" className="text-gray-400 hover:text-gray-700 text-sm transition">← لوحة التحكم</Link>
          <span className="text-gray-200">/</span>
          <span className="text-sm font-medium text-gray-700">مريض جديد</span>
        </div>
        <div className="w-8 h-8 rounded-xl bg-brand-600 flex items-center justify-center text-white text-xs font-bold">م+</div>
      </header>

      <main className="max-w-2xl mx-auto px-6 py-10">
        {appStage.type === "stage1" && (
          <Banner color="blue"><Spin/><span>جارٍ تقييم المخاطر وإعداد الملخص...</span></Banner>
        )}
        {appStage.type === "polling" && (
          <Banner color="blue"><Spin/>
            <div>
              <p className="font-medium">جارٍ توليد المقالات التفصيلية في الخلفية...</p>
              <p className="text-xs mt-0.5 opacity-75">
                {poll.attempts > 0 && `فحص ${poll.attempts} · `}مستوى الخطر: {rAr(appStage.riskLevel)} · {appStage.articleCount} مقالات
              </p>
            </div>
          </Banner>
        )}
        {appStage.type === "complete" && (
          <Banner color="green">
            <span className="text-xl">✓</span>
            <div className="flex-1">
              <p className="font-medium mb-2">اكتمل التوليد بنجاح</p>
              <div className="flex flex-wrap gap-2 mb-3 text-sm">
                <RiskBadge level={appStage.riskLevel as any}/>
                <span className="text-gray-600">{appStage.articles} مقالات · ${appStage.totalCost.toFixed(4)}</span>
              </div>
              <div className="flex gap-3">
                <Link href={`/sessions/${appStage.sessionId}`}
                  className="px-4 py-2 rounded-xl bg-brand-600 text-white text-sm font-medium hover:bg-brand-800 transition">
                  عرض المقالات
                </Link>
                <button onClick={() => { setAppStage({ type: "idle" }); reset(); }}
                  className="px-4 py-2 rounded-xl border border-gray-200 text-gray-600 text-sm hover:bg-gray-50 transition">
                  مريض جديد
                </button>
              </div>
            </div>
          </Banner>
        )}
        {appStage.type === "error" && (
          <Banner color="red">
            <span>⚠</span>
            <div>
              <p className="font-medium">حدث خطأ</p>
              <p className="text-xs mt-0.5">{appStage.message}</p>
              <button onClick={() => setAppStage({ type: "idle" })} className="mt-2 text-xs underline">حاول مرة أخرى</button>
            </div>
          </Banner>
        )}

        <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
          <div className="px-6 py-5 border-b border-gray-50">
            <h1 className="text-lg font-semibold text-gray-900">بيانات المريض</h1>
            <p className="text-sm text-gray-400 mt-0.5">أدخل المعلومات الطبية لتوليد محتوى تعليمي مخصص</p>
          </div>
          <form onSubmit={handleSubmit(onSubmit)} className="px-6 py-6 space-y-6" noValidate>
            <F label="التشخيص الرئيسي" req err={errors.primaryDiagnosis?.message}>
              <input type="text" placeholder="مثال: داء السكري من النوع الثاني" className={i(!!errors.primaryDiagnosis)}
                {...register("primaryDiagnosis", { required: "مطلوب", minLength: { value: 3, message: "3 أحرف على الأقل" }, maxLength: { value: 200, message: "الحد الأقصى 200" } })} />
            </F>
            <F label="الفئة العمرية" req err={errors.ageGroup?.message}>
              <select className={i(!!errors.ageGroup)} {...register("ageGroup", { required: "مطلوب" })}>
                {AGE_GROUPS.map(g => <option key={g.value} value={g.value}>{g.label}</option>)}
              </select>
            </F>
            <F label="الأمراض المصاحبة" hint="افصل بفاصلة" err={errors.comorbidities?.message}>
              <textarea rows={2} placeholder="مثال: ارتفاع ضغط الدم، السمنة" className={i(!!errors.comorbidities)}
                {...register("comorbidities", { maxLength: { value: 500, message: "الحد الأقصى 500" } })} />
            </F>
            <F label="الأدوية الحالية" hint="اذكر الجرعة والتكرار" err={errors.currentMedications?.message}>
              <textarea rows={3} placeholder="مثال: ميتفورمين 500 ملغ مرتين يومياً" className={i(!!errors.currentMedications)}
                {...register("currentMedications", { maxLength: { value: 1000, message: "الحد الأقصى 1000" } })} />
            </F>
            <F label="الحساسية الدوائية" err={errors.allergies?.message}>
              <input type="text" placeholder="مثال: البنسلين" className={i(!!errors.allergies)}
                {...register("allergies", { maxLength: { value: 300, message: "الحد الأقصى 300" } })} />
            </F>
            <F label="القيود والملاحظات" hint="الحمل، أمراض الكلى، إلخ" err={errors.medicalRestrictions?.message}>
              <textarea rows={2} placeholder="مثال: حامل (28 أسبوعاً)" className={i(!!errors.medicalRestrictions)}
                {...register("medicalRestrictions", { maxLength: { value: 500, message: "الحد الأقصى 500" } })} />
            </F>
            <div className="px-4 py-3 rounded-xl bg-amber-50 border border-amber-100 text-amber-800 text-xs leading-relaxed">
              المرحلة الأولى ~10–15 ثانية ثم تُوَلَّد المقالات في الخلفية (60–120 ثانية). يمكنك مغادرة الصفحة ومراجعة النتائج لاحقاً من لوحة التحكم.
            </div>
            <button type="submit" disabled={busy}
              className="w-full py-3.5 rounded-xl bg-brand-600 text-white font-semibold text-sm hover:bg-brand-800 transition disabled:opacity-60 flex items-center justify-center gap-2">
              {busy ? <><Spin/> جارٍ التوليد...</> : "توليد المحتوى التعليمي"}
            </button>
          </form>
        </div>
      </main>
    </div>
  );
}

const i = (e: boolean) => `w-full px-4 py-3 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 transition bg-gray-50 ${e ? "border-red-300 bg-red-50" : "border-gray-200"}`;
const rAr = (l: string) => ({ LOW:"منخفض", MODERATE:"متوسط", HIGH:"مرتفع", CRITICAL:"حرج" }[l] ?? l);
const Spin = () => <span className="inline-block w-4 h-4 border-2 border-current/30 border-t-current rounded-full animate-spin flex-shrink-0" />;

function Banner({ color, children }: { color: "blue"|"green"|"red"; children: React.ReactNode }) {
  const c = color==="green"?"bg-brand-50 border-brand-200 text-brand-800":color==="red"?"bg-red-50 border-red-200 text-red-700":"bg-blue-50 border-blue-200 text-blue-800";
  return <div className={`mb-6 px-5 py-4 rounded-xl border flex items-start gap-3 text-sm ${c}`}>{children}</div>;
}

function F({ label, hint, req, err, children }: { label: string; hint?: string; req?: boolean; err?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {label}{req && <span className="text-red-500 mr-1">*</span>}
        {hint && <span className="text-gray-400 font-normal text-xs mr-2">{hint}</span>}
      </label>
      {children}
      {err && <p className="text-red-600 text-xs mt-1">{err}</p>}
    </div>
  );
}
