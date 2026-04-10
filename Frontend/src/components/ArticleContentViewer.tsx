"use client";
import { useState } from "react";
import type { ArticleOutline } from "@/types";

const RISK_CLASS: Record<string, string> = {
  LOW:      "bg-green-50  text-green-700  border border-green-200",
  MODERATE: "bg-blue-50   text-blue-700   border border-blue-200",
  HIGH:     "bg-orange-50 text-orange-700 border border-orange-200",
  CRITICAL: "bg-red-50    text-red-700    border border-red-200",
};

const RISK_LABEL: Record<string, string> = {
  LOW: "منخفض", MODERATE: "متوسط", HIGH: "مرتفع", CRITICAL: "حرج",
};

interface ArticleContentViewerProps {
  riskLevel:       string | null;
  summaryArticle:  string | null;
  articleOutlines: ArticleOutline[];
  mode:            "referral" | "test-scenario";
  onGenerate?:     (index: number) => Promise<void>;
}

export default function ArticleContentViewer({
  riskLevel,
  summaryArticle,
  articleOutlines,
  onGenerate,
}: ArticleContentViewerProps) {
  const [expanded,   setExpanded]   = useState(false);
  const [generating, setGenerating] = useState<number | null>(null);

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
            className="text-sm text-gray-700 leading-relaxed bg-gray-50 rounded-xl p-4 overflow-y-auto"
            style={{ maxHeight: expanded ? "none" : "400px" }}
          >
            {summaryArticle}
          </div>
          <button
            onClick={() => setExpanded(!expanded)}
            className="mt-1.5 text-brand-600 text-xs hover:underline"
          >
            {expanded ? "عرض أقل" : "عرض كامل"}
          </button>
        </div>
      )}

      {/* Article outlines */}
      {articleOutlines.length > 0 && (
        <div>
          <p className="text-xs font-medium text-gray-500 mb-2">
            مخطط المقالات ({articleOutlines.length})
          </p>
          <ol className="space-y-2">
            {articleOutlines.map((outline, i) => (
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
    </div>
  );
}
