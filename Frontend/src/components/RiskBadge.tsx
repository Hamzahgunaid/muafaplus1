import type { RiskLevel } from "@/types";

const LABELS: Record<RiskLevel, string> = {
  LOW:      "منخفض",
  MODERATE: "متوسط",
  HIGH:     "مرتفع",
  CRITICAL: "حرج",
};

const CLASSES: Record<RiskLevel, string> = {
  LOW:      "risk-low",
  MODERATE: "risk-moderate",
  HIGH:     "risk-high",
  CRITICAL: "risk-critical",
};

export default function RiskBadge({ level }: { level: RiskLevel }) {
  return (
    <span className={`inline-block px-2.5 py-0.5 rounded-full border text-xs font-medium ${CLASSES[level]}`}>
      مستوى الخطر: {LABELS[level]}
    </span>
  );
}
