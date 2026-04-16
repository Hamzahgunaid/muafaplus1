"use client";
import { useState } from "react";
import ReactMarkdown from "react-markdown";
import type { ArticleOutline, ReferralArticleResponse } from "@/types";

const RISK_CLASS: Record<string, string> = {
  LOW:      "bg-green-50  text-green-700  border border-green-200",
  MODERATE: "bg-blue-50   text-blue-700   border border-blue-200",
  HIGH:     "bg-orange-50 text-orange-700 border border-orange-200",
  CRITICAL: "bg-red-50    text-red-700    border border-red-200",
};

const RISK_LABEL: Record<string, string> = {
  LOW: "منخفض", MODERATE: "متوسط", HIGH: "مرتفع", CRITICAL: "حرج",
};

function extractTitle(content: string): string | null {
  const lines = content.split("\n");
  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith("# ")) {
      return trimmed.replace(/^#+\s*/, "").trim();
    }
  }
  return null;
}

interface ArticleContentViewerProps {
  riskLevel:          string | null;
  summaryArticle:     string | null;
  articleOutlines?:   ArticleOutline[];
  referralArticles?:  ReferralArticleResponse[];
  mode:               "referral" | "test-scenario";
  onGenerate?:        (index: number) => Promise<void>;
}

export default function ArticleContentViewer({
  riskLevel,
  summaryArticle,
  articleOutlines,
  referralArticles,
  mode,
  onGenerate,
}: ArticleContentViewerProps) {
  const [expanded,      setExpanded]      = useState(false);
  const [generating,    setGenerating]    = useState<number | null>(null);
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null);

  const handleGenerate = async (index: number) => {
    if (!onGenerate) return;
    setGenerating(index);
    try {
      await onGenerate(index);
    } finally {
      setGenerating(null);
    }
  };

  return (
    <div className="space-y-4">
      {/* Risk badge */}
      {riskLevel && RISK_CLASS[riskLevel] && (
        <span className={`inline-block px-3 py-1 rounded-full text-sm font-medium ${RISK_CLASS[riskLevel]}`}>
          مستوى الخطر: {RISK_LABEL[riskLevel] ?? riskLevel}
        </span>
      )}

      {/* Summary article */}
      {summaryArticle && (
        <div>
          <p className="text-xs font-medium text-gray-500 mb-2">الملخص الصحي</p>
          <div
            className="text-sm text-gray-700 leading-relaxed bg-gray-50 rounded-xl p-4 overflow-y-auto prose prose-sm max-w-none rtl"
            style={{ maxHeight: expanded ? "none" : "400px" }}
          >
            <ReactMarkdown>{summaryArticle}</ReactMarkdown>
          </div>
          <button
            onClick={() => setExpanded(!expanded)}
            className="mt-1.5 text-brand-600 text-xs hover:underline"
          >
            {expanded ? "عرض أقل" : "عرض كامل"}
          </button>
        </div>
      )}

      {/* Test-scenario mode — article outline list with generate buttons */}
      {(articleOutlines?.length ?? 0) > 0 && (
        <div>
          <p className="text-xs font-medium text-gray-500 mb-2">
            مخطط المقالات ({articleOutlines!.length})
          </p>
          <ol className="space-y-2">
            {articleOutlines!.map((outline, i) => (
              <li key={i} className="flex items-center justify-between gap-3 text-sm">
                <span className="text-gray-700">
                  {i + 1}. {outline.TitleAr || outline.TitleEn || `مقالة ${i + 1}`}
                </span>
                {onGenerate && (
                  <button
                    onClick={() => handleGenerate(i)}
                    disabled={generating !== null}
                    className="shrink-0 px-3 py-1 rounded-lg text-xs font-medium bg-brand-50 text-brand-700 hover:bg-brand-100 transition disabled:opacity-50"
                  >
                    {generating === i ? "جارٍ التوليد..." : "توليد"}
                  </button>
                )}
              </li>
            ))}
          </ol>
        </div>
      )}

      {/* Referral mode — flat article list from GET /referrals/{id}/articles */}
      {mode === "referral" && referralArticles && referralArticles.length > 0 && (
        <div className="border border-gray-100 rounded-xl overflow-hidden">
          <div className="px-5 py-3 bg-gray-50 border-b border-gray-100">
            <span className="font-medium text-sm text-gray-800">المقالات التفصيلية</span>
            <span className="text-xs text-gray-400 mr-2">
              ({referralArticles.filter(a => a.articleType === "detailed").length} مقالات)
            </span>
          </div>
          <div className="divide-y divide-gray-50">
            {referralArticles
              .filter(a => a.articleType === "detailed")
              .map((article, index) => {
                const isExpanded = expandedIndex === index;
                const title      = extractTitle(article.content_ar);
                return (
                  <div key={article.articleId} className="px-5 py-3">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex-1">
                        <p className="text-sm font-medium text-gray-800">
                          {title ?? `مقال ${index + 1}`}
                        </p>
                        <p className="text-xs text-gray-400 mt-0.5">
                          {article.wordCount} كلمة
                        </p>
                      </div>
                      <button
                        onClick={() => setExpandedIndex(isExpanded ? null : index)}
                        className="text-xs text-brand-600 hover:underline px-3 py-1 rounded-lg border border-brand-200 hover:bg-brand-50 transition shrink-0"
                      >
                        {isExpanded ? "إخفاء ▲" : "عرض →"}
                      </button>
                    </div>
                    {isExpanded && (
                      <div className="mt-3 pt-3 border-t border-gray-100 text-sm text-gray-700 leading-relaxed prose prose-sm max-w-none rtl">
                        <ReactMarkdown>{article.content_ar}</ReactMarkdown>
                      </div>
                    )}
                  </div>
                );
              })}
          </div>
        </div>
      )}
    </div>
  );
}
