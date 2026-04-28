"use client";
import { useEffect, useState, useRef, useCallback } from "react";
import { useRouter, useParams } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/lib/store";
import { referralApi, chatApi, maskPhone, formatRelativeTime } from "@/services/api";
import NavBar from "@/components/NavBar";
import ArticleContentViewer from "@/components/ArticleContentViewer";
import type {
  ReferralResponse,
  ReferralEngagementResponse,
  ReferralArticleResponse,
  ChatThreadResponse,
  ArticleEngagementResponse,
} from "@/types";

// ── Constants ─────────────────────────────────────────────────────────────────

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

const STATUS_LABEL: Record<string, string> = {
  Created:          "تم الإنشاء",
  Stage1Delivered:  "تم التسليم",
  Stage2Requested:  "جارٍ التوليد",
  Stage2Complete:   "مكتمل",
  FeedbackSubmitted: "تم التقييم",
};

// ── Page ──────────────────────────────────────────────────────────────────────

export default function ReferralDetailPage() {
  const router        = useRouter();
  const params        = useParams();
  const referralId    = params.id as string;
  const { isLoggedIn } = useAuthStore();

  const [referral,          setReferral]          = useState<ReferralResponse | null>(null);
  const [engagement,        setEngagement]        = useState<ReferralEngagementResponse | null>(null);
  const [articles,          setArticles]          = useState<ReferralArticleResponse[]>([]);
  const [articleEngagement, setArticleEngagement] = useState<ArticleEngagementResponse[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [error,    setError]    = useState<string | null>(null);

  useEffect(() => {
    if (!isLoggedIn) { router.push("/login"); }
  }, [isLoggedIn, router]);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [refRes, engRes] = await Promise.all([
        referralApi.getReferral(referralId),
        referralApi.getEngagement(referralId),
      ]);
      if (refRes.success && refRes.data) {
        setReferral(refRes.data);
        if (refRes.data.sessionId) {
          const artRes = await referralApi.getArticles(referralId);
          if (artRes.success && artRes.data) setArticles(artRes.data);
        }
      } else {
        setError(refRes.error ?? "لم يتم العثور على الإحالة");
      }
      if (engRes.success && engRes.data) {
        setEngagement(engRes.data);
      }
      const fullEngRes = await referralApi.getFullEngagement(referralId);
      if (fullEngRes.success && fullEngRes.data) {
        setArticleEngagement(fullEngRes.data.articles ?? []);
      }
    } catch {
      setError("خطأ في الاتصال بالخادم");
    } finally {
      setLoading(false);
    }
  }, [referralId]);

  useEffect(() => {
    if (isLoggedIn && referralId) fetchData();
  }, [isLoggedIn, referralId, fetchData]);

  if (!isLoggedIn) return null;

  return (
    <div className="min-h-screen flex flex-col bg-ink-50" dir="rtl">
      <NavBar />

      <main className="flex-1 max-w-3xl w-full mx-auto px-6 py-8">

        {/* Breadcrumb */}
        <div className="flex items-center gap-3 mb-6">
          <Link
            href="/referrals"
            className="text-ink-400 hover:text-ink-700 text-sm transition"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            ← الإحالات
          </Link>
          <span className="text-ink-100">/</span>
          <span
            className="text-sm font-medium text-ink-700"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            تفاصيل الإحالة
          </span>
        </div>

        {/* Loading skeleton */}
        {loading && <SkeletonCards />}

        {/* Error */}
        {!loading && error && (
          <div className="bg-white rounded-2xl border border-ink-100 px-8 py-12 text-center">
            <p
              className="text-red-600 text-sm mb-4"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              {error}
            </p>
            <Link
              href="/referrals"
              className="text-navy-600 text-sm font-medium hover:underline"
              style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
            >
              ← العودة إلى الإحالات
            </Link>
          </div>
        )}

        {/* Content */}
        {!loading && !error && referral && (
          <div className="space-y-4">
            <InfoCard referral={referral} />
            <TimelineCard referral={referral} engagement={engagement} />
            <ArticleEngagementCard articles={articleEngagement} />
            {articles.length > 0 && (
              <Card title="المحتوى المولّد">
                <ArticleContentViewer
                  riskLevel={referral.riskLevel ?? null}
                  summaryArticle={
                    articles.find(a => a.articleType === "summary")?.content_ar ?? null
                  }
                  articleOutlines={[]}
                  referralArticles={articles}
                  mode="referral"
                />
              </Card>
            )}
            {referral.chatEnabled && (
              <ChatCard referralId={referralId} />
            )}
          </div>
        )}
      </main>
    </div>
  );
}

// ── Section 1: Referral Info ──────────────────────────────────────────────────

function InfoCard({ referral: r }: { referral: ReferralResponse }) {
  const deliveryTime = r.scheduledDeliveryAt
    ? new Date(r.scheduledDeliveryAt).toLocaleString("ar-YE", {
        weekday: "short", year: "numeric", month: "short",
        day: "numeric", hour: "2-digit", minute: "2-digit",
      })
    : null;

  return (
    <Card title="معلومات الإحالة">
      <div className="flex flex-wrap items-center gap-3 mb-4">
        {r.riskLevel && RISK_CLASS[r.riskLevel] && (
          <span
            className={`inline-block px-4 py-1.5 rounded-full text-sm font-semibold ${RISK_CLASS[r.riskLevel]}`}
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            مستوى الخطر: {RISK_LABEL[r.riskLevel] ?? r.riskLevel}
          </span>
        )}
        <span
          className="inline-block px-3 py-1 rounded-full text-xs font-medium bg-ink-100 text-ink-500"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {STATUS_LABEL[r.status] ?? r.status}
        </span>
      </div>

      <dl className="space-y-2 text-sm">
        <Row label="رقم الهاتف"    value={maskPhone(r.patientPhone)} mono />
        {r.patientName && <Row label="اسم المريض"  value={r.patientName} />}
        <Row label="رقم الإحالة"   value={r.referralId} mono />
        <Row label="تاريخ الإنشاء" value={formatRelativeTime(r.createdAt)} />
        {deliveryTime && (
          <Row label="موعد الإرسال" value={deliveryTime} />
        )}
        {r.deliveredAt && (
          <Row label="تم الإرسال" value={formatRelativeTime(r.deliveredAt)} />
        )}
      </dl>
    </Card>
  );
}

function Row({
  label, value, mono,
}: {
  label: string; value: string; mono?: boolean;
}) {
  return (
    <div className="flex items-start gap-2">
      <dt
        className="text-ink-400 min-w-[120px] shrink-0"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        {label}:
      </dt>
      <dd
        className={`text-ink-700 ${mono ? "font-mono" : ""}`}
        style={mono ? {} : { fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        {value}
      </dd>
    </div>
  );
}

// ── Section 2: Engagement Timeline ───────────────────────────────────────────

type TimelineStep = {
  label: string;
  time:  string | null | undefined;
};

function TimelineCard({
  referral,
  engagement,
}: {
  referral:   ReferralResponse;
  engagement: ReferralEngagementResponse | null;
}) {
  const steps: TimelineStep[] = [
    { label: "تم إنشاء الإحالة",       time: referral.createdAt },
    { label: "تم إرسال الرسالة",       time: engagement?.messageSentAt },
    { label: "فتح المريض التطبيق",     time: engagement?.appOpenedAt },
    { label: "قرأ الملخص الصحي",       time: engagement?.summaryViewedAt },
    { label: "طلب المقالات التفصيلية",  time: engagement?.stage2RequestedAt },
    { label: "أرسل التغذية الراجعة",   time: engagement?.feedbackSubmittedAt },
  ];

  return (
    <Card title="مسار التفاعل">
      <div className="space-y-0">
        {steps.map((step, i) => {
          const done = Boolean(step.time);
          const last = i === steps.length - 1;
          return (
            <div key={i} className="flex gap-4">
              {/* Circle + vertical line */}
              <div className="flex flex-col items-center">
                <div className={`w-5 h-5 rounded-full border-2 flex-shrink-0 flex items-center justify-center ${
                  done
                    ? "bg-navy-600 border-navy-600"
                    : "bg-white border-ink-100"
                }`}>
                  {done && (
                    <span className="text-white text-xs font-bold leading-none">✓</span>
                  )}
                </div>
                {!last && (
                  <div className={`w-0.5 flex-1 min-h-[24px] mt-0.5 ${done ? "bg-navy-200" : "bg-ink-100"}`} />
                )}
              </div>

              {/* Label + timestamp */}
              <div className={`pb-5 flex-1 ${last ? "pb-0" : ""}`}>
                <p
                  className={`text-sm font-medium ${done ? "text-ink-900" : "text-ink-400"}`}
                  style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                >
                  {step.label}
                </p>
                {done && step.time && (
                  <p
                    className="text-xs text-ink-400 mt-0.5"
                    style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                  >
                    {formatRelativeTime(step.time)}
                  </p>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </Card>
  );
}

// ── Section 3: Chat ───────────────────────────────────────────────────────────

type ChatState =
  | { type: "idle" }
  | { type: "loading" }
  | { type: "loaded"; thread: ChatThreadResponse }
  | { type: "error"; message: string };

function ChatCard({ referralId }: { referralId: string }) {
  const [chatState,    setChatState]    = useState<ChatState>({ type: "idle" });
  const [message,      setMessage]      = useState("");
  const [sending,      setSending]      = useState(false);
  const [chatDisabled, setChatDisabled] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);

  const loadThread = useCallback(async () => {
    setChatState({ type: "loading" });
    try {
      const res = await chatApi.getChatThread(referralId);
      if (res.success && res.data) {
        setChatState({ type: "loaded", thread: res.data });
      } else if (res.errorType === "Forbidden" || res.error?.toLowerCase().includes("403") || res.error?.toLowerCase().includes("forbidden")) {
        setChatDisabled(true);
        setChatState({ type: "idle" });
      } else {
        setChatState({ type: "error", message: res.error ?? "تعذر تحميل المحادثة" });
      }
    } catch {
      setChatState({ type: "error", message: "خطأ في الاتصال" });
    }
  }, [referralId]);

  if (chatDisabled) return null;

  const refreshThread = useCallback(async () => {
    try {
      const res = await chatApi.getChatThread(referralId);
      if (res.success && res.data) {
        setChatState({ type: "loaded", thread: res.data });
      }
    } catch {
      // silent refresh failure
    }
  }, [referralId]);

  // Scroll to bottom when messages change
  useEffect(() => {
    if (chatState.type === "loaded") {
      bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }
  }, [chatState]);

  const handleSend = async () => {
    const content = message.trim();
    if (!content || sending) return;
    setSending(true);
    setMessage("");
    try {
      await chatApi.sendChatMessage(referralId, content);
      await refreshThread();
    } finally {
      setSending(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const getExpiryInfo = (thread: ChatThreadResponse) => {
    const now      = Date.now();
    const expiry   = new Date(thread.expiresAt).getTime();
    const expired  = now > expiry;
    const daysLeft = Math.ceil((expiry - now) / (1000 * 60 * 60 * 24));
    return { expired, daysLeft };
  };

  const canSend = (thread: ChatThreadResponse) => {
    const { expired } = getExpiryInfo(thread);
    return !expired && thread.messageCount < 10 && thread.isEnabled;
  };

  return (
    <Card title="محادثة المريض">

      {/* State: idle */}
      {chatState.type === "idle" && (
        <button
          onClick={loadThread}
          className="px-4 py-2 rounded-xl bg-navy-600 text-white text-sm font-medium hover:bg-navy-700 transition"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          فتح المحادثة
        </button>
      )}

      {/* State: loading */}
      {chatState.type === "loading" && (
        <div
          className="flex items-center gap-2 text-ink-400 text-sm py-4"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          <Spin />
          <span>جاري تحميل المحادثة...</span>
        </div>
      )}

      {/* State: error */}
      {chatState.type === "error" && (
        <div className="space-y-3">
          <p
            className="text-red-600 text-sm"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            {chatState.message}
          </p>
          <button
            onClick={loadThread}
            className="text-navy-600 text-sm hover:underline"
            style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
          >
            إعادة المحاولة
          </button>
        </div>
      )}

      {/* State: loaded */}
      {chatState.type === "loaded" && (() => {
        const thread = chatState.thread;
        const { expired, daysLeft } = getExpiryInfo(thread);
        const active = canSend(thread);

        return (
          <div>
            {/* Disclaimer */}
            <div className="mb-4 p-3 rounded-xl bg-orange-50 border border-orange-100">
              <p
                className="text-orange-800 text-sm leading-relaxed"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {thread.disclaimerAr}
              </p>
              <p className="text-ink-400 text-xs mt-1">{thread.disclaimerEn}</p>
            </div>

            {/* Expired notice */}
            {expired && (
              <div
                className="mb-4 p-3 rounded-xl bg-ink-50 border border-ink-100 text-ink-500 text-sm"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                انتهت صلاحية المحادثة
              </div>
            )}

            {/* Max messages notice */}
            {!expired && thread.messageCount >= 10 && (
              <div
                className="mb-4 p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-700 text-sm"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                تم الوصول إلى الحد الأقصى من الرسائل (10)
              </div>
            )}

            {/* Expiry countdown */}
            {!expired && daysLeft > 0 && (
              <p
                className="text-xs text-ink-400 mb-3"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                تنتهي المحادثة في: {daysLeft} {daysLeft === 1 ? "يوم" : "أيام"}
              </p>
            )}

            {/* Message list */}
            <div className="space-y-3 max-h-80 overflow-y-auto mb-4 px-1">
              {thread.messages.length === 0 ? (
                <p
                  className="text-ink-400 text-sm text-center py-6"
                  style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                >
                  لا توجد رسائل بعد
                </p>
              ) : (
                thread.messages.map((msg) => {
                  const isPhysician = msg.senderRole === "Physician";
                  return (
                    <div
                      key={msg.messageId}
                      className={`flex ${isPhysician ? "justify-end" : "justify-start"}`}
                    >
                      <div
                        className={`max-w-[75%] px-4 py-2.5 rounded-2xl text-sm ${
                          isPhysician
                            ? "bg-navy-50 text-ink-900 rounded-tl-sm"
                            : "bg-ink-100 text-ink-700 rounded-tr-sm"
                        }`}
                      >
                        <p
                          className="leading-relaxed"
                          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                        >
                          {msg.content}
                        </p>
                        <p
                          className="text-xs opacity-60 mt-1"
                          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                        >
                          {isPhysician ? "الطبيب" : "المريض"} · {formatRelativeTime(msg.sentAt)}
                        </p>
                      </div>
                    </div>
                  );
                })
              )}
              <div ref={bottomRef} />
            </div>

            {/* Input */}
            {active && (
              <div className="flex items-end gap-2 border-t border-ink-100 pt-4">
                <textarea
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  onKeyDown={handleKeyDown}
                  rows={2}
                  placeholder="اكتب رسالتك..."
                  className="flex-1 px-4 py-2.5 rounded-xl border border-ink-100 bg-white text-sm resize-none focus:outline-none focus:ring-2 focus:ring-navy-400 transition"
                  style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                  disabled={sending}
                />
                <button
                  onClick={handleSend}
                  disabled={sending || !message.trim()}
                  className="px-4 py-2.5 rounded-xl bg-navy-600 text-white text-sm font-medium hover:bg-navy-700 transition disabled:opacity-50 flex items-center gap-1.5"
                  style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
                >
                  {sending ? <Spin /> : null}
                  إرسال
                </button>
              </div>
            )}
          </div>
        );
      })()}
    </Card>
  );
}

// ── Shared primitives ─────────────────────────────────────────────────────────

function Card({
  title,
  children,
}: {
  title:    string;
  children: React.ReactNode;
}) {
  return (
    <div className="bg-white rounded-2xl border border-ink-100 overflow-hidden">
      <div className="px-6 py-4 border-b border-ink-100">
        <h2
          className="font-semibold text-ink-900 text-sm"
          style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
        >
          {title}
        </h2>
      </div>
      <div className="px-6 py-5">{children}</div>
    </div>
  );
}

function Spin() {
  return (
    <span className="inline-block w-4 h-4 border-2 border-current/30 border-t-current rounded-full animate-spin flex-shrink-0" />
  );
}

function ArticleEngagementCard({ articles }: { articles: ArticleEngagementResponse[] }) {
  if (!articles || articles.length === 0) return null;

  const depthLabel = (a: ArticleEngagementResponse) => {
    if (a.completedAt) return { label: "مكتمل", pct: 100, color: "bg-green-500" };
    if (a.depth75At)   return { label: "75%",   pct: 75,  color: "bg-blue-500" };
    if (a.depth50At)   return { label: "50%",   pct: 50,  color: "bg-yellow-500" };
    if (a.depth25At)   return { label: "25%",   pct: 25,  color: "bg-orange-400" };
    if (a.openedAt)    return { label: "فُتح",  pct: 5,   color: "bg-ink-400" };
    return               { label: "لم يُقرأ",   pct: 0,   color: "bg-ink-100" };
  };

  return (
    <div className="bg-white rounded-2xl border border-ink-100 p-6">
      <h2
        className="text-right text-sm font-semibold text-ink-900 mb-4"
        style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
      >
        تفاعل المريض مع المقالات
      </h2>
      <div className="space-y-3">
        {articles.map((a, i) => {
          const { label, pct, color } = depthLabel(a);
          return (
            <div key={a.articleId ?? i} className="flex items-center gap-3">
              <span
                className="text-xs font-medium text-ink-500 w-12 text-left"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {label}
              </span>
              <div className="flex-1 bg-ink-100 rounded-full h-2">
                <div
                  className={`${color} h-2 rounded-full transition-all`}
                  style={{ width: `${pct}%` }}
                />
              </div>
              <span
                className="text-sm text-ink-700 text-right flex-1 truncate"
                style={{ fontFamily: "IBM Plex Sans Arabic, system-ui" }}
              >
                {a.articleTitle ?? `مقال ${i + 1}`}
              </span>
              {a.reaction === "Like"    && <span className="text-green-500">👍</span>}
              {a.reaction === "Dislike" && <span className="text-red-500">👎</span>}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function SkeletonCards() {
  return (
    <div className="space-y-4">
      {[1, 2, 3].map((n) => (
        <div key={n} className="bg-white rounded-2xl border border-ink-100 overflow-hidden animate-pulse">
          <div className="px-6 py-4 border-b border-ink-100">
            <div className="h-4 w-32 bg-ink-100 rounded" />
          </div>
          <div className="px-6 py-5 space-y-3">
            <div className="h-3 w-full bg-ink-100 rounded" />
            <div className="h-3 w-4/5 bg-ink-100 rounded" />
            <div className="h-3 w-3/5 bg-ink-100 rounded" />
          </div>
        </div>
      ))}
    </div>
  );
}
